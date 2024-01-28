using CYQ.Data;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Taurus.Plugin.DistributedLock;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTC
    {
        public static partial class Server
        {
            internal static partial class Worker
            {
                /// <summary>
                /// 1、扫描数据库
                /// 2、发送到MQ
                /// 3、程序运行时启动、服务调用时也检测启动。  
                /// </summary>
                internal static class DBScanner
                {
                    static DBScanner()
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(MQPublisher.InitQueueListen), null);
                    }

                    static bool threadIsWorking = false;
                    const string lockKey = "DTC.Server.Lock:Worker.ScanDB";
                    static readonly object lockObj = new object();
                    public static void Start()
                    {
                        if (MQ.Server.MQType == MQType.Empty || threadIsWorking)
                        {
                            empty = 1;//保持任务不退出。
                            return;
                        }
                        lock (lockObj)
                        {
                            if (!threadIsWorking)
                            {
                                threadIsWorking = true;
                                empty = 0;
                                ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), null);
                            }
                        }
                    }
                    static int empty = 0;
                    static DateTime scanTime = DateTime.Now;
                    private static void DoWork(object p)
                    {
                        while (true)
                        {
                            try
                            {
                                bool isLockOK = false;
                                try
                                {
                                    isLockOK = DLock.Instance.Lock(lockKey, 1);
                                    if (isLockOK)
                                    {
                                        if (empty % 30 == 0)
                                        {
                                            ScanIO_DeleteEmptyDirectory();

                                            if (!string.IsNullOrEmpty(DTCConfig.Server.Conn))
                                            {
                                                ScanDB_DeleteConfirm();
                                                if (empty % 60 == 0)
                                                {
                                                    ScanDB_DeleteTimeout();
                                                    if (empty % 120 == 0)
                                                    {
                                                        ScanDB_RetryForConfirm();//2分钟重试1次
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    if (isLockOK)
                                    {
                                        DLock.Instance.UnLock(lockKey);
                                    }
                                }


                                Thread.Sleep(1000);
                                empty++;
                                if (empty > 4 * 60)  //扫描10次都没东西可以扫
                                {
                                    ScanIO_DeleteTimeout();
                                    threadIsWorking = false;
                                    break;//结束线程。
                                }
                            }
                            catch (Exception err)
                            {
                                threadIsWorking = false;
                                Log.Error(err);
                                break;
                            }
                        }
                    }



                    /// <summary>
                    /// 向客户端，发起重新确认，以便处理删除。
                    /// </summary>
                    private static void ScanDB_RetryForConfirm()
                    {
                        if (string.IsNullOrEmpty(DTCConfig.Server.Conn) || !DBTool.Exists(DTCConfig.Server.TableName, "U", DTCConfig.Server.Conn))
                        {
                            return;
                        }
                        MQType mQType = MQ.Client.MQType;
                        if (mQType == MQType.Empty) { return; }
                        int maxRetries = Math.Max(1, DTCConfig.Server.Worker.MaxRetries);
                        int scanInterval = Math.Max(60, DTCConfig.Server.Worker.RetryIntervalSecond);//最短1分钟

                        using (MAction action = new MAction(DTCConfig.Server.TableName, DTCConfig.Server.Conn))
                        {
                            action.IsUseAutoCache = false;

                            #region 扫描数据库、发送到MQ队列
                            string whereConfirm = "ConfirmState=1 and Retries<" + maxRetries + " and EditTime<'" + DateTime.Now.AddSeconds(-scanInterval).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                            MDataTable dtSend = action.Select(1000, whereConfirm);
                            while (dtSend != null && dtSend.Rows.Count > 0)
                            {
                                empty = -1;

                                bool isUpdateOK = false;

                                List<MQMsg> msgList = dtSend.ToList<MQMsg>();
                                foreach (MQMsg msg in msgList)
                                {
                                    msg.SetDeleteAsk();
                                    if (mQType == MQType.Rabbit)
                                    {
                                        msg.ExChange = DTCConfig.Client.MQ.Rabbit.ConfirmExChange;
                                        msg.CallBackName = DTCConfig.Server.MQ.Rabbit.ConfirmQueue;
                                    }
                                    else if (mQType == MQType.Kafka)
                                    {
                                        msg.QueueName = DTCConfig.Client.MQ.Kafka.ConfirmTopic;
                                        msg.CallBackName = DTCConfig.Server.MQ.Kafka.ConfirmTopic;
                                    }
                                }
                                if (MQ.Client.PublishBatch(msgList))
                                {
                                    Log.Print("ScanDB.MQ.Publish :" + msgList.Count + " items.");
                                    DTCConsole.WriteDebugLine("Server.ScanDB.MQ.Publish :" + msgList.Count + " items.");
                                    foreach (var row in dtSend.Rows)
                                    {
                                        row.Set("Retries", row.Get<int>("Retries") + 1, 2);
                                        row.Set("EditTime", DateTime.Now, 2);
                                    }
                                    isUpdateOK = dtSend.AcceptChanges(AcceptOp.Update, DTCConfig.Server.Conn, "ID");
                                }

                                if (isUpdateOK)
                                {
                                    dtSend = action.Select(1000, whereConfirm);
                                }
                                else
                                {
                                    break;
                                }
                                Thread.Sleep(1);
                            }


                            #endregion

                            #region 清空数据、或转移到历史表




                            #endregion
                        }
                    }



                    private static void ScanDB_DeleteConfirm()
                    {
                        if (string.IsNullOrEmpty(DTCConfig.Server.Conn) || !DBTool.Exists(DTCConfig.Server.TableName, "U", DTCConfig.Server.Conn))
                        {
                            return;
                        }
                        using (MAction action = new MAction(DTCConfig.Server.TableName, DTCConfig.Server.Conn))
                        {
                            action.IsUseAutoCache = false;
                            string whereDelete = "ConfirmState=2";

                            if (DTCConfig.Server.Worker.ConfirmClearMode == ClearMode.Delete)
                            {
                                action.Delete(whereDelete);//不讲道理直接清
                            }
                            else
                            {
                                #region 已确认的数据：清空数据、或转移到历史表
                                MDataTable dt = action.Select(1000, whereDelete + " order by id asc");
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    dt.TableName = DTCConfig.Server.TableName + "_History";
                                    if (dt.AcceptChanges(AcceptOp.Auto | AcceptOp.InsertWithID, DTCConfig.Server.Conn))//仅插入
                                    {
                                        dt.TableName = DTCConfig.Server.TableName;
                                        dt.AcceptChanges(AcceptOp.Delete, DTCConfig.Server.Conn, "ID");
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                    private static void ScanDB_DeleteTimeout()
                    {
                        if (string.IsNullOrEmpty(DTCConfig.Server.Conn) || !DBTool.Exists(DTCConfig.Server.TableName, "U", DTCConfig.Server.Conn))
                        {
                            return;
                        }
                        using (MAction action = new MAction(DTCConfig.Server.TableName, DTCConfig.Server.Conn))
                        {
                            action.IsUseAutoCache = false;
                            int noConfirmSecond = DTCConfig.Server.Worker.TimeoutKeepSecond;
                            string whereTimeout = "ConfirmState<2 and CreateTime<'" + DateTime.Now.AddSeconds(-noConfirmSecond).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                            if (DTCConfig.Server.Worker.TimeoutClearMode == ClearMode.Delete)
                            {
                                action.Delete(whereTimeout);
                            }
                            else
                            {
                                #region 已超时的数据：删除或转移到超时表

                                MDataTable dt = action.Select(1000, whereTimeout + " order by id asc");
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    dt.TableName = DTCConfig.Server.TableName + "_History";
                                    if (dt.AcceptChanges(AcceptOp.Auto | AcceptOp.InsertWithID, DTCConfig.Server.Conn))
                                    {
                                        dt.TableName = DTCConfig.Server.TableName;
                                        dt.AcceptChanges(AcceptOp.Delete, DTCConfig.Server.Conn, "ID");
                                    }
                                }


                                #endregion
                            }
                        }
                    }

                    private static void ScanIO_DeleteTimeout()
                    {
                        IO.DeleteTimeoutTable();
                    }
                    private static void ScanIO_DeleteEmptyDirectory()
                    {
                        IO.DeleteEmptyDirectory();
                    }
                }
            }
        }
    }
}
