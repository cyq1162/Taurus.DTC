using CYQ.Data;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Taurus.Plugin.DistributedLock;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTC
    {
        public static partial class Client
        {
            internal static partial class Worker
            {
                /// <summary>
                /// 1、扫描数据库
                /// 2、发送到MQ
                /// 3、程序运行时启动、服务调用时也检测启动。  
                /// </summary>
                internal class DBScanner
                {
                    static DBScanner()
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(MQPublisher.InitQueueListen), null);
                    }
                    static bool threadIsWorking = false;
                    const string lockKey = "DTC.Client.Lock:Worker.ScanDB";
                    static object lockObj = new object();
                    public static void Start()
                    {
                        if (MQ.Client.MQType == MQType.Empty || threadIsWorking)
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

                    private static void DoWork(object p)
                    {
                        while (true)
                        {
                            try
                            {
                                bool isLockOK = false;
                                if (empty % 30 == 0)
                                {
                                    #region 处理数据库
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(DTCConfig.Client.Conn))
                                        {
                                            isLockOK = DLock.Instance.Lock(lockKey, 1);
                                            if (isLockOK)
                                            {
                                                ScanDB_DeleteConfirm();
                                                ScanDB_DeleteTimeout();

                                                if (empty % 120 == 0)
                                                {
                                                    ScanDB_Retry();//数据库仅允许一个在扫描
                                                }
                                            }

                                        }
                                    }
                                    finally
                                    {
                                        if (isLockOK) { DLock.Instance.UnLock(lockKey); }
                                    }
                                    #endregion

                                    if (empty > 0)
                                    {
                                        try
                                        {
                                            isLockOK = DLock.Local.Lock(lockKey, 1);
                                            if (isLockOK)
                                            {
                                                ScanIO_Retry();//硬盘每个进程都需要扫描，但延时处理。
                                            }
                                        }
                                        finally
                                        {
                                            if (isLockOK) { DLock.Local.UnLock(lockKey); }
                                        }

                                    }
                                }

                                Thread.Sleep(1000);
                                empty++;
                                if (empty > 5 * 60)  //扫描5分钟都没东西可以扫
                                {
                                    threadIsWorking = false;
                                    break;//结束线程。
                                }
                            }
                            catch (Exception err)
                            {
                                Log.Error(err);
                                break;
                            }

                        }
                    }

                    private static void ScanDB_Retry()
                    {
                        if (!DBTool.Exists(DTCConfig.Client.TableName, "U", DTCConfig.Client.Conn))
                        {
                            return;
                        }
                        MQType mQType = MQ.Client.MQType;
                        if(mQType == MQType.Empty) { return; }
                        int maxRetries = DTCConfig.Client.Worker.MaxRetries;
                        int retryInterval = Math.Max(60, DTCConfig.Client.Worker.RetryIntervalSecond);//最短1分钟

                        using (MAction action = new MAction(DTCConfig.Client.TableName, DTCConfig.Client.Conn))
                        {
                            action.IsUseAutoCache = false;

                            #region 扫描数据库、发送到MQ队列
                            string where = "ConfirmState = 0 and Retries<" + maxRetries + " and EditTime<'" + DateTime.Now.AddSeconds(-retryInterval).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                            MDataTable dtSend = action.Select(1000, where);
                            while (dtSend != null && dtSend.Rows.Count > 0)
                            {
                                empty = -1;

                                bool isUpdateOK = false;
                                List<MQMsg> msgList = dtSend.ToList<MQMsg>();
                               
                                foreach (var item in msgList)
                                {
                                    if (mQType == MQType.Rabbit)
                                    {
                                        item.ExChange = DTCConfig.Server.MQ.Rabbit.DefaultExChange;
                                        item.CallBackName = DTCConfig.Client.MQ.Rabbit.ConfirmQueue;
                                    }
                                    else if (mQType == MQType.Kafka)
                                    {
                                        item.QueueName = DTCConfig.Server.MQ.Kafka.DefaultTopic;
                                        item.CallBackName = DTCConfig.Client.MQ.Kafka.ConfirmTopic;
                                    }
                                }
                                if (MQ.Client.PublishBatch(msgList))
                                {
                                    Log.Print("ScanDB.MQ.Publish.ToRetryExChange :" + msgList.Count + " items.");
                                    DTCConsole.WriteDebugLine("Client.ScanDB.MQ.Publish.ToRetryExChange :" + msgList.Count + " items.");
                                    foreach (var row in dtSend.Rows)
                                    {
                                        row.Set("Retries", row.Get<int>("Retries") + 1, 2);
                                        row.Set("EditTime", DateTime.Now, 2);
                                    }
                                    isUpdateOK = dtSend.AcceptChanges(AcceptOp.Update, DTCConfig.Client.Conn, "ID");
                                }

                                if (isUpdateOK)
                                {
                                    dtSend = action.Select(1000, where);
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
                        if (!DBTool.Exists(DTCConfig.Client.TableName, "U", DTCConfig.Client.Conn))
                        {
                            return;
                        }
                        using (MAction action = new MAction(DTCConfig.Client.TableName, DTCConfig.Client.Conn))
                        {
                            action.IsUseAutoCache = false;
                            string whereConfirm = "ConfirmState=1";
                            if (DTCConfig.Client.Worker.ConfirmClearMode == ClearMode.Delete)
                            {
                                action.Delete(whereConfirm);//不讲道理直接清
                            }
                            else
                            {
                                #region 已确认的数据：清空数据、或转移到历史表
                                MDataTable dt = action.Select(10000, whereConfirm + " order by id asc");
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    dt.TableName = DTCConfig.Client.TableName + "_History";
                                    if (dt.AcceptChanges(AcceptOp.Auto | AcceptOp.InsertWithID, DTCConfig.Client.Conn))//仅插入
                                    {
                                        dt.TableName = DTCConfig.Client.TableName;
                                        dt.AcceptChanges(AcceptOp.Delete, DTCConfig.Client.Conn, "ID");
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                    private static void ScanDB_DeleteTimeout()
                    {
                        if (!DBTool.Exists(DTCConfig.Client.TableName, "U", DTCConfig.Client.Conn))
                        {
                            return;
                        }
                        int noConfirmSecond = DTCConfig.Client.Worker.TimeoutKeepSecond;
                        using (MAction action = new MAction(DTCConfig.Client.TableName, DTCConfig.Client.Conn))
                        {
                            action.IsUseAutoCache = false;
                            string whereTimeout = "ConfirmState=0 and CreateTime<'" + DateTime.Now.AddSeconds(-noConfirmSecond).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                            if (DTCConfig.Client.Worker.TimeoutClearMode == ClearMode.Delete)
                            {
                                action.Delete(whereTimeout);
                            }
                            else
                            {
                                #region 已超时的数据：删除或转移到超时表

                                MDataTable dt = action.Select(10000, whereTimeout + " order by id asc");
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    dt.TableName = DTCConfig.Client.TableName + "_History";
                                    if (dt.AcceptChanges(AcceptOp.Auto | AcceptOp.InsertWithID, DTCConfig.Client.Conn))
                                    {
                                        dt.TableName = DTCConfig.Client.TableName;
                                        dt.AcceptChanges(AcceptOp.Delete, DTCConfig.Client.Conn, "ID");
                                    }
                                }


                                #endregion
                            }
                        }
                    }
                    private static void ScanIO_Retry()
                    {
                        MQType mQType = MQ.Client.MQType;
                        if (mQType == MQType.Empty) { return; }
                        List<Table> tables = Worker.IO.GetScanTable();
                        if (tables != null && tables.Count > 0)
                        {
                            int maxRetries = DTCConfig.Client.Worker.MaxRetries;
                            int retryInterval = Math.Max(60, DTCConfig.Client.Worker.RetryIntervalSecond);//最短1分钟
                            int timeout = DTCConfig.Client.Worker.TimeoutKeepSecond;

                            DateTime retryDate = DateTime.Now.AddSeconds(-retryInterval);
                            DateTime timeoutDate = DateTime.Now.AddSeconds(-timeout);

                            List<MQMsg> msgList = new List<MQMsg>();
                            //消息重发
                            foreach (var table in tables)
                            {
                                if (table.CreateTime.HasValue && table.CreateTime.Value < timeoutDate)
                                {
                                    //删除过期数据。
                                    IO.Delete(table.TraceID, table.ExeType);
                                    continue;
                                }
                                if (!table.Retries.HasValue) { table.Retries = 0; }
                                if (table.Retries >= maxRetries)
                                {
                                    continue;
                                }
                                if (table.EditTime.HasValue && table.EditTime.Value > retryDate)
                                {
                                    continue;//在一个扫描间隔时间内的不触发重试
                                }
                                
                                table.Retries += 1;
                                table.EditTime = DateTime.Now;
                                IO.Write(table);
                                MQMsg msg = table.ToMQMsg();
                                if (mQType == MQType.Rabbit)
                                {
                                    msg.ExChange = DTCConfig.Server.MQ.Rabbit.DefaultExChange;
                                    msg.CallBackName = DTCConfig.Client.MQ.Rabbit.ConfirmQueue;
                                }
                                else if (mQType == MQType.Kafka)
                                {
                                    msg.QueueName = DTCConfig.Server.MQ.Kafka.DefaultTopic;
                                    msg.CallBackName = DTCConfig.Client.MQ.Kafka.ConfirmTopic;
                                }
                                msgList.Add(msg);

                            }

                            //批量发送
                            if (msgList.Count > 0 && MQ.Client.PublishBatch(msgList))
                            {
                                string printMsg = "Client.ScanIO.MQ.Publish.Retry :" + msgList.Count + " items.";
                                Log.Print(printMsg);
                                DTCConsole.WriteDebugLine(printMsg);
                            }
                        }
                    }
                }
            }
        }
    }
}
