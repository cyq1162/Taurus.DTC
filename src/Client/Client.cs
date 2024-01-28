using CYQ.Data.Json;
using CYQ.Data.Tool;
using System;
using System.Reflection;
using System.Web;
using Taurus.Plugin.DistributedLock;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTC
    {
        /// <summary>
        /// 分布式事务 调用端
        /// </summary>
        public static partial class Client
        {

            /*
             * 客户端逻辑说明
             * 1、发送消息：DTC.Client.SendAsync(....)
             * 2、异步表任务：生成表、创建数据    =》注意：失败重试、再失败直接写MQ、再失败记录文本数据
             * 3、异步MQ任务：读表、写MQ、发送数据。
             * 4、异步MQ任务：等待接收数据，更新表状态数据。
             */

            #region 发起 PublishTask

            /// <summary>
            /// 提交事务确认
            /// </summary>
            /// <param name="requestNum">需要确认的数量</param>
            public static bool CommitAsync(int requestNum)
            {
                return ExeAsync(ExeType.Commit, null, null, null, requestNum);
            }

            /// <summary>
            /// 提交事务确认
            /// </summary>
            /// <param name="requestNum">需要确认的数量</param>
            /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTCClientCallBack 特性标注</param>
            public static bool CommitAsync(int requestNum, string callBackKey)
            {
                return ExeAsync(ExeType.Commit, null, null, callBackKey, requestNum);
            }

            /// <summary>
            /// 提交事务确认
            /// </summary>
            /// <param name="requestNum">需要确认的数量</param>
            /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTCClientCallBack 特性标注</param>
            /// <param name="content">传递的信息</param>
            public static bool CommitAsync(int requestNum, string callBackKey, string content)
            {
                return ExeAsync(ExeType.Commit, content, null, callBackKey, requestNum);
            }
            /// <summary>
            /// 提交事务回滚
            /// </summary>
            /// <param name="requestNum">需要回滚的数量</param>
            public static bool RollBackAsync(int requestNum)
            {
                return ExeAsync(ExeType.RollBack, null, null, null, requestNum);
            }
            /// <summary>
            /// 提交事务回滚
            /// </summary>
            /// <param name="requestNum">需要回滚的数量</param>
            /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTCClientCallBack 特性标注</param>
            public static bool RollBackAsync(int requestNum, string callBackKey)
            {
                return ExeAsync(ExeType.RollBack, null, null, callBackKey, requestNum);
            }

            /// <summary>
            /// 提交事务回滚
            /// </summary>
            /// <param name="requestNum">需要回滚的数量</param>
            /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTCClientCallBack 特性标注</param>
            /// <param name="content">传递的信息</param>
            public static bool RollBackAsync(int requestNum, string callBackKey, string content)
            {
                return ExeAsync(ExeType.RollBack, content, null, callBackKey, requestNum);
            }

            private static bool ExeAsync(ExeType exeType, string content, string taskKey, string callBackKey, int requestNum)
            {
                if (System.Web.HttpContext.Current == null)
                {
                    throw new Exception("HttpContext.Current is null.");
                }
                Table table = new Table();
                table.TraceID = HttpContext.Current.GetTraceID();
                table.ExeType = exeType.ToString();
                table.Content = content;
                table.TaskKey = taskKey;
                table.CallBackKey = callBackKey;
                table.RequestNum = Math.Max(1, requestNum);
                table.CreateTime = DateTime.Now;
                table.EditTime = DateTime.Now;
                table.Retries = 0;

                if (!Worker.Save(table))
                {
                    return false;
                }
                MQMsg msg = table.ToMQMsg();
                switch (MQ.Client.MQType)
                {
                    case MQType.Rabbit:
                        msg.ExChange = DTCConfig.Server.MQ.Rabbit.DefaultExChange;
                        msg.CallBackName = DTCConfig.Client.MQ.Rabbit.DefaultQueue;
                        break;
                    case MQType.Kafka:
                        msg.QueueName = DTCConfig.Server.MQ.Kafka.DefaultTopic;
                        msg.CallBackName = DTCConfig.Client.MQ.Kafka.DefaultTopic;
                        break;
                    default:
                        return true;
                }

                Worker.Add(msg);
                return true;
            }

            #endregion

            #region 接收 Subscribe

            /// <summary>
            /// 消息有回调，说明对方任务已完成
            /// </summary>
            internal static void OnReceived(MQMsg msg)
            {
                Log.Print("MQ.OnReceived : " + msg.ToJson());
                var localLock = DLock.Local;
                string key = "DTC.Client." + msg.MsgID;
                bool isLockOK = false;
                try
                {
                    isLockOK = localLock.Lock(key, 1);
                    if (isLockOK)
                    {
                        string printMsg = "-------------------Client " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "------------------------------" + Environment.NewLine;
                        bool isFirstAck = !msg.IsFirstAck.HasValue || msg.IsFirstAck.Value;
                        printMsg += "Client.MQ.OnReceived : " + msg.TraceID + "[" + msg.MsgID + "] - " + msg.ExeType + (isFirstAck ? " - FirstAck" : "") + " - NextTo :" + msg.QueueName + Environment.NewLine;
                        OnCommitOrRollBack(msg, ref printMsg);
                        printMsg += "-------------------------------------------------------------------------------" + Environment.NewLine;
                        DTCConsole.WriteDebugLine(printMsg);
                    }
                    else
                    {

                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
                finally
                {
                    if (isLockOK)
                    {
                        localLock.UnLock(key);
                    }
                }
            }

            private static void OnCommitOrRollBack(MQMsg msg, ref string printMsg)
            {
                if (!(!msg.IsFirstAck.HasValue || msg.IsFirstAck.Value) || (msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value))
                {
                    CommitOrRollBackConfirm(msg);
                    return;
                }

                if (!string.IsNullOrEmpty(msg.CallBackKey))
                {
                    MethodInfo method = MethodCollector.GetClientMethod(msg.CallBackKey);
                    if (method != null)
                    {
                        try
                        {
                            DTCCallBackPara para = new DTCCallBackPara(msg);
                            object obj = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType);
                            object invokeResult = method.Invoke(obj, new object[] { para });
                            if (invokeResult is bool && !(bool)invokeResult) { return; }
                            Log.Print("Execute." + msg.ExeType + ".CallBack.Method : " + method.Name);
                            printMsg += "Client.Execute." + msg.ExeType + ".CallBack.Method : " + method.Name + Environment.NewLine;
                        }
                        catch (Exception err)
                        {
                            Log.Error(err);
                            return;
                        }
                    }
                }
                CommitOrRollBackConfirm(msg);
            }

            private static void CommitOrRollBackConfirm(MQMsg msg)
            {
                bool isUpdateOK = false;
                using (Table table = new Table())
                {
                    #region 事务确认和回滚
                    //等待这边确认执行类型
                    bool isConfirm = false;
                    if (table.Fill("TraceID='" + msg.TraceID + "' and ExeType='" + msg.ExeType + "'"))
                    {
                        if (string.IsNullOrEmpty(table.Content))
                        {
                            table.Content = msg.MsgID;
                            if (!table.RequestNum.HasValue || table.RequestNum.Value <= 1)
                            {
                                table.ConfirmState = 1;
                                isConfirm = true;
                            }
                            isUpdateOK = table.Update();
                            if (isUpdateOK)
                            {
                                //DTCLog.WriteDebugLine("Client.OnCommitOrRollBack 更新状态。");
                            }
                        }
                        else if (!table.Content.Contains(msg.MsgID))
                        {
                            table.Content += "," + msg.MsgID;
                            if (!table.RequestNum.HasValue || table.RequestNum.Value <= table.Content.Split(',').Length)
                            {
                                table.ConfirmState = 1;
                                isConfirm = true;
                            }
                            isUpdateOK = table.Update();
                            if (isUpdateOK)
                            {
                                //DTCLog.WriteDebugLine("Client.OnCommitOrRollBack 更新状态。");
                            }
                        }
                    }
                    if (!isConfirm)
                    {
                        string json = Worker.IO.Read(msg.TraceID, msg.ExeType);
                        if (!string.IsNullOrEmpty(json))
                        {
                            var tb = JsonHelper.ToEntity<Table>(json);
                            if (tb != null)
                            {
                                if (string.IsNullOrEmpty(tb.Content))
                                {
                                    tb.Content = msg.MsgID;
                                    if (!tb.RequestNum.HasValue || tb.RequestNum.Value <= 1)
                                    {
                                        isConfirm = true;
                                    }
                                }
                                else if (!tb.Content.Contains(msg.MsgID))
                                {
                                    tb.Content += "," + msg.MsgID;
                                    if (!tb.RequestNum.HasValue || tb.RequestNum.Value <= tb.Content.Split(',').Length)
                                    {
                                        isConfirm = true;
                                    }
                                }
                                if (!isConfirm)
                                {
                                    //重新写回缓存里
                                    isUpdateOK = Worker.IO.Write(tb);
                                }
                            }
                        }
                    }
                    #endregion
                    if (isConfirm)
                    {
                        if (Worker.IO.Delete(msg.TraceID, msg.ExeType))
                        {
                            //DTCLog.WriteDebugLine("Client.OnCommitOrRollBack 删除缓存。");
                        }
                    }

                    //这边已经删除数据，告诉对方，也可以删除数据了。
                    if (isUpdateOK || isConfirm || (msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value))
                    {
                        msg.SetDeleteAsk();
                        Worker.MQPublisher.Add(msg);
                        //DTCLog.WriteDebugLine("Client.OnDoTask.IsDeleteAck，让服务端确认及删除掉缓存。");
                    }
                }
            }

            #endregion

        }
    }
}
