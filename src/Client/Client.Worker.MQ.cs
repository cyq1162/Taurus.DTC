using CYQ.Data;
using CYQ.Data.Table;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTC
    {
        public static partial class Client
        {
            internal static partial class Worker
            {
                internal static partial class MQPublisher
                {
                    /// <summary>
                    /// 待处理的工作队列
                    /// </summary>
                    static ConcurrentQueue<MQMsg> _dtcQueue = new ConcurrentQueue<MQMsg>();

                    static bool threadIsWorking = false;
                    static object lockObj = new object();
                    public static void Add(MQMsg msg)
                    {
                        _dtcQueue.Enqueue(msg);
                        InitQueueListen(null);//允许延时监听
                        if (threadIsWorking) { return; }
                        lock (lockObj)
                        {
                            if (!threadIsWorking)
                            {
                                threadIsWorking = true;
                                ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), null);
                            }
                        }
                    }

                    private static void DoWork(object p)
                    {
                        try
                        {
                            int empty = 0;
                            while (true)
                            {
                                while (!_dtcQueue.IsEmpty)
                                {
                                    empty = 0;
                                    List<MQMsg> mQMsgs = new List<MQMsg>();
                                    while (!_dtcQueue.IsEmpty && mQMsgs.Count < 500)
                                    {
                                        MQMsg msg;
                                        if (_dtcQueue.TryDequeue(out msg))
                                        {
                                            mQMsgs.Add(msg);
                                        }
                                    }
                                    if (mQMsgs.Count > 0)
                                    {
                                        if (MQ.Client.PublishBatch(mQMsgs))
                                        {
                                            string printMsg = "Client.MQ.Publish : " + mQMsgs.Count + " items.";
                                            Log.Print(printMsg);
                                            DTCConsole.WriteDebugLine(printMsg);
                                        }
                                        mQMsgs.Clear();
                                    }

                                    Thread.Sleep(1);
                                }
                                empty++;
                                Thread.Sleep(1000);
                                if (empty > 100)
                                {
                                    //超过10分钟没日志产生
                                    threadIsWorking = false;
                                    break;//结束线程。
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            threadIsWorking = false;
                            //数据库异常，不处理。
                            Log.Error(err);
                        }
                    }
                    private static bool hasInitListen = false;
                    public static void InitQueueListen(object p)
                    {
                        if (hasInitListen) { return; }
                        var mq = MQ.Client;
                        if (mq.MQType != MQType.Empty)
                        {
                            hasInitListen = true;
                            string printMsg = "--------------------------------------------------" + Environment.NewLine;
                            if (mq.MQType == MQType.Rabbit)
                            {

                                //对默认对列绑定交换机。
                                bool isOK = MQ.Client.Listen(DTCConfig.Client.MQ.Rabbit.DefaultQueue, Client.OnReceived, DTCConfig.Client.MQ.Rabbit.DefaultExChange, false);
                                printMsg+="DTC.Client." + mq.MQType + ".Listen : " + DTCConfig.Client.MQ.Rabbit.DefaultQueue + " - ExChange : " + DTCConfig.Client.MQ.Rabbit.DefaultExChange + (isOK ? " - OK." : " - Fail.")+Environment.NewLine;

                                isOK = MQ.Client.Listen(DTCConfig.Client.MQ.Rabbit.ConfirmQueue, Client.OnReceived, DTCConfig.Client.MQ.Rabbit.ConfirmExChange, false);
                                printMsg += "DTC.Client." + mq.MQType + ".Listen : " + DTCConfig.Client.MQ.Rabbit.ConfirmQueue + " - ExChange : " + DTCConfig.Client.MQ.Rabbit.ConfirmExChange + (isOK ? " - OK." : " - Fail.") + Environment.NewLine;

                            }
                            else if (mq.MQType == MQType.Kafka)
                            {
                                bool isOK = MQ.Client.Listen(DTCConfig.Client.MQ.Kafka.DefaultTopic, Client.OnReceived, DTCConfig.Client.MQ.Kafka.DefaultGroup, false);
                                printMsg += "DTC.Client." + mq.MQType + ".Listen : " + DTCConfig.Client.MQ.Kafka.DefaultTopic + " -  Group : " + DTCConfig.Client.MQ.Kafka.DefaultGroup + (isOK ? " - OK." : " - Fail.") + Environment.NewLine;

                                isOK = MQ.Client.Listen(DTCConfig.Client.MQ.Kafka.ConfirmTopic, Client.OnReceived, DTCConfig.Client.MQ.Kafka.ConfirmGroup, false);
                                printMsg += "DTC.Client." + mq.MQType + ".Listen : " + DTCConfig.Client.MQ.Kafka.ConfirmTopic + " -  Group : " + DTCConfig.Client.MQ.Kafka.ConfirmGroup + (isOK ? " - OK." : " - Fail.") + Environment.NewLine;
                            }
                            printMsg += "--------------------------------------------------" + Environment.NewLine;
                            DTCConsole.WriteLine(printMsg);
                        }

                    }
                }
            }

        }
    }
}
