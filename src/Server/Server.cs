using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using CYQ.Data;
using System.Web;
using Taurus.Plugin.DistributedLock;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTC
    {
        /// <summary>
        /// 分布式事务 提供端
        /// </summary>
        public static partial class Server
        {
            #region Subscribe

            /// <summary>
            /// 保存信息，以便订阅回调函数处处理。
            /// </summary>
            /// <param name="content">需要传递到订阅回调处理的内容</param>
            /// <param name="subKey">指定订阅key</param>
            /// <returns></returns>
            public static bool Subscribe(string content, string subKey)
            {
                if (System.Web.HttpContext.Current == null)
                {
                    throw new Exception("HttpContext.Current is null.");
                }
                Table table = new Table();
                table.CallBackKey = subKey;
                table.Content = content;
                table.Retries = 0;
                table.CreateTime = DateTime.Now;
                table.EditTime = DateTime.Now;

                table.TraceID = HttpContext.Current.GetTraceID();
                return Worker.Add(table);
            }
            #endregion



            internal static void OnReceived(MQMsg msg)
            {
                MQType mqType = MQ.Server.MQType;
                if (mqType == MQType.Rabbit)
                {
                    msg.CallBackName = DTCConfig.Server.MQ.Rabbit.ConfirmQueue;
                }
                else if (mqType == MQType.Kafka)
                {
                    msg.CallBackName = DTCConfig.Server.MQ.Kafka.ConfirmTopic;
                }

                //这里不能加锁：同一个TraceID，调用了同一台电脑，不同的项目接口。
                try
                {
                    string printMsg = "-------------------Server " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + " - " + msg.ExeType + " --------------------" + Environment.NewLine;

                    printMsg += "Server.MQ.OnReceived : " + msg.TraceID + Environment.NewLine;
                    OnCommitOrRollBack(msg, ref printMsg);
                    if (msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value)
                    {
                        //打印分隔线，以便查看
                        printMsg += "-------------------Server " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + " - END -----------------------" + Environment.NewLine;
                    }
                    else
                    {
                        //打印分隔线，以便查看
                        printMsg += "-------------------Server " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + " ------------------------------" + Environment.NewLine;
                    }
                    DTCConsole.WriteDebugLine(printMsg);
                    Log.Print(printMsg);
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
                finally
                {

                }
            }


            #region 事务提交或回滚

            private static void OnCommitOrRollBack(MQMsg msg, ref string printMsg)
            {
                if (msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value)
                {
                    //可以删除数据
                    using (Table table = new Table())
                    {
                        table.ConfirmState = 2;
                        table.EditTime = DateTime.Now;
                        if (table.Update(msg.MsgID))
                        {
                            //DTCLog.WriteDebugLine("Server.OnCommitOrRollBack 收到MQ Ack：IsFirstAck=true ，更新表。");
                        }
                    }
                    if (Worker.IO.Delete(msg.TraceID, msg.MsgID, msg.ExeType))
                    {
                        //DTCLog.WriteDebugLine("Server.OnCommitOrRollBack 收到MQ Ack：IsFirstAck=true ，删除缓存。");
                    }
                    return;
                }

                List<Table> tables = GetTableList(msg);
                if (tables == null || tables.Count == 0)
                {
                    printMsg += "No items for process, return directly." + Environment.NewLine;
                    msg.IsDeleteAck = true;
                    return;
                }

                foreach (Table item in tables)
                {
                    msg.MsgID = item.MsgID;
                    if (item.ConfirmState.HasValue && item.ConfirmState.Value > 0)
                    {
                        printMsg += (msg.MsgID + " Processed, return directly." + Environment.NewLine);
                        msg.IsFirstAck = false;
                        Worker.MQPublisher.Add(msg);
                        printMsg += "NextTo :" + msg.QueueName + Environment.NewLine;
                        continue;
                    }
                    string returnContent = null;
                    try
                    {
                        MethodInfo method = MethodCollector.GetServerMethod(item.CallBackKey);
                        if (method == null)
                        {
                            printMsg += ("No callback method for execute, return directly." + Environment.NewLine);
                            continue;
                        }
                        DTCSubscribePara para = new DTCSubscribePara(msg);
                        object obj = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType);
                        object result = method.Invoke(obj, new object[] { para });
                        if (result is bool && !(bool)result) { continue; }
                        returnContent = para.CallBackContent;
                        printMsg += "Server.Execute." + msg.ExeType + ".Subscribe.Method : " + method.Name + Environment.NewLine;
                        printMsg += "NextTo :" + msg.QueueName + Environment.NewLine;
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        return;
                    }
                    msg.IsFirstAck = true;
                    msg.Content = returnContent;
                    Worker.MQPublisher.Add(msg.Clone());
                    
                    item.TaskKey = msg.TaskKey;
                    item.ExeType = msg.ExeType;
                    item.ConfirmState = 1;
                    item.EditTime = DateTime.Now;
                    if (item.Update(item.MsgID))
                    {
                        item.Dispose();
                        //DTCLog.WriteDebugLine("Server.OnCommitOrRollBack 更新数据表。");
                    }
                    else if (Worker.IO.Write(item))//缓存1份。
                    {
                        //DTCLog.WriteDebugLine("Server.OnCommitOrRollBack 更新数据缓存。");
                    }


                }
            }
            private static List<Table> GetTableList(MQMsg msg)
            {
                List<Table> tableList = null;
                using (Table table = new Table())
                {
                    tableList = table.Select<Table>("TraceID='" + msg.TraceID + "'");
                }
                if (tableList != null && tableList.Count > 0)
                {
                    return tableList;
                }
                return Worker.IO.GetListByTraceID(msg.TraceID, msg.ExeType);
            }
            #endregion
        }
    }
}
