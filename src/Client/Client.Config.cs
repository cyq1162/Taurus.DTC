using CYQ.Data;
namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTCConfig
    {
        /// <summary>
        /// 客户端【调用端】配置项
        /// </summary>
        public static class Client
        {
            /// <summary>
            /// 配置是否启用 客户端【调用端】
            /// 如 DTC.Client.IsEnable ：true， 默认值：true
            /// </summary>
            public static bool IsEnable
            {
                get
                {
                    return AppConfig.GetAppBool("DTC.Client.IsEnable", true);
                }
                set
                {
                    AppConfig.SetApp("DTC.Client.IsEnable", value.ToString());
                }
            }

            /// <summary>
            /// 配置是否 打印追踪日志，用于调试
            /// 如 DTC.Client.IsPrintTraceLog ：true， 默认值：false
            /// </summary>
            public static bool IsPrintTraceLog
            {
                get
                {
                    return AppConfig.GetAppBool("DTC.Client.IsPrintTraceLog", false);
                }
                set
                {
                    AppConfig.SetApp("DTC.Client.IsPrintTraceLog", value.ToString());
                }
            }
            /// <summary>
            /// DTC 类记录本地消息数据库 - 数据库链接配置
            /// 配置项：DTCClientConn：server=.;database=x;uid=s;pwd=p;
            /// </summary>
            public static string Conn
            {
                get
                {
                    return AppConfig.GetConn("DTC.Client.Conn");
                }
                set
                {
                    AppConfig.SetConn("DTC.Client.Conn", value);
                }
            }
            /// <summary>
            /// DTC 类记录本地消息数据库 - 表名
            /// 配置项：DTC.Client.TableName ：DTC_Client
            /// </summary>
            public static string TableName
            {
                get
                {
                    return AppConfig.GetApp("DTC.Client.TableName", "DTC_Client");
                }
                set
                {
                    AppConfig.SetApp("DTC.Client.TableName", value);
                }
            }

            /// <summary>
            /// RabbitMQ 链接配置
            /// 配置项：DTC.Client.Rabbit=127.0.0.1;guest;guest;/
            /// </summary>
            public static string Rabbit
            {
                get
                {
                    return AppConfig.GetApp("DTC.Client.Rabbit");
                }
                set
                {
                    AppConfig.SetApp("DTC.Client.Rabbit", value);
                }
            }
            /// <summary>
            /// Kafka 链接配置
            /// 配置项：DTC.Client.Kafka=127.0.0.1:9092
            /// </summary>
            public static string Kafka
            {
                get
                {
                    return AppConfig.GetApp("DTC.Client.Kafka");
                }
                set
                {
                    AppConfig.SetApp("DTC.Client.Kafka", value);
                }
            }

            /// <summary>
            /// RabbitMQ相关配置项
            /// </summary>
            internal static class MQ
            {
               
                public static class Rabbit
                {
                    /// <summary>
                    /// DTC 默认交换机名称，绑定所Default队列
                    /// </summary>
                    internal static string DefaultExChange
                    {
                        get
                        {
                            return "DTC_Client_Default";
                        }
                    }

                    /// <summary>
                    /// DTC 默认交换机名称，绑定所有Confirm队列
                    /// </summary>
                    internal static string ConfirmExChange
                    {
                        get
                        {
                            return "DTC_Client_Confirm";
                        }
                    }
                    /// <summary>
                    /// 首次队列往这发，比较急。
                    /// </summary>
                    internal static string DefaultQueue
                    {
                        get
                        {
                            return "DTC_Client_Default_" + ProjectName;
                        }
                    }

                    /// <summary>
                    /// 确认删除队列往这发。
                    /// </summary>
                    internal static string ConfirmQueue
                    {
                        get
                        {
                            return "DTC_Client_Confirm_" + ProjectName;
                        }
                    }
                }

                public static class Kafka
                {
                    internal static string DefaultTopic
                    {
                        get
                        {
                            return "DTC_Client_Default";
                        }
                    }

                    internal static string ConfirmTopic
                    {
                        get
                        {
                            return "DTC_Client_Confirm";
                        }
                    }

                    internal static string DefaultGroup
                    {
                        get
                        {
                            return "DTC_Client_Default_" + ProjectName;
                        }
                    }

                    internal static string ConfirmGroup
                    {
                        get
                        {
                            return "DTC_Client_Confirm_" + ProjectName;
                        }
                    }
                }

               
            }

            /// <summary>
            /// 工作线程处理模式
            /// </summary>
            public static class Worker
            {
                /// <summary>
                /// 事务发送后，未收到服务端响应时，间隔多久重新发起任务：单位（秒），默认3分钟
                /// 配置项：DTC.Client.RetryIntervalSecond ：300
                /// </summary>
                public static int RetryIntervalSecond
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTC.Client.RetryIntervalSecond", 3 * 60);
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Client.RetryIntervalSecond", ((int)value).ToString());
                    }
                }

                /// <summary>
                /// 事务完成，收到确认后：0 删除（默认）、1 转移到历史表
                /// 配置项：DTC.Client.ConfirmClearMode ：0
                /// </summary>
                public static ClearMode ConfirmClearMode
                {
                    get
                    {
                        return (ClearMode)AppConfig.GetAppInt("DTC.Client.ConfirmClearMode", (int)ClearMode.Delete);
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Client.ConfirmClearMode", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 事务发起后，未收到确认时，数据保留时间：（单位秒），默认7天
                /// 配置项：DTC.Client.TimeoutKeepSecond ：7 * 24 * 3600
                /// </summary>
                public static int TimeoutKeepSecond
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTC.Client.TimeoutKeepSecond", 3 * 24 * 3600);//7 * 24 * 3600
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Client.TimeoutKeepSecond", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 事务发起后，未收到确认，超时后数据：0 删除、1 转移到历史表（默认）
                /// 配置项：DTC.Client.TimeoutClearMode ：1
                /// </summary>
                public static ClearMode TimeoutClearMode
                {
                    get
                    {
                        return (ClearMode)AppConfig.GetAppInt("DTC.Client.TimeoutClearMode", (int)ClearMode.MoveToHistoryTable);
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Client.TimeoutClearMode", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 任务发起后，未收到确认时，最大重试次数。
                /// 配置项：DTC.Client.MaxRetries ：50
                /// </summary>
                public static int MaxRetries
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTC.Client.MaxRetries", 50);
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Client.MaxRetries", ((int)value).ToString());
                    }
                }
            }
        }
    }
}
