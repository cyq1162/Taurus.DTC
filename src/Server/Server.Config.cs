using CYQ.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTCConfig
    {
        /// <summary>
        /// 服务端【接口提供端】配置项
        /// </summary>
        public static class Server
        {
            /// <summary>
            /// 配置是否启用 服务端【接口提供端】
            /// 如 DTC.Server.IsEnable ：true， 默认值：true
            /// </summary>
            public static bool IsEnable
            {
                get
                {
                    return AppConfig.GetAppBool("DTC.Server.IsEnable", true);
                }
                set
                {
                    AppConfig.SetApp("DTC.Server.IsEnable", value.ToString());
                }
            }

            /// <summary>
            /// 配置是否 打印追踪日志，用于调试
            /// 如 DTC.Client.IsPrintTraceLog ：true， 默认值：true
            /// </summary>
            public static bool IsPrintTraceLog
            {
                get
                {
                    return AppConfig.GetAppBool("DTC.Server.IsPrintTraceLog", false);
                }
                set
                {
                    AppConfig.SetApp("DTC.Server.IsPrintTraceLog", value.ToString());
                }
            }

            /// <summary>
            /// DTC 类记录本地消息数据库 - 数据库链接配置
            /// 配置项：DTCServerConn：server=.;database=x;uid=s;pwd=p;
            /// </summary>
            public static string Conn
            {
                get
                {
                    return AppConfig.GetConn("DTC.Server.Conn");
                }
                set
                {
                    AppConfig.SetConn("DTC.Server.Conn", value);
                }
            }

            /// <summary>
            /// DTC 类记录本地消息数据库 - 表名
            /// 配置项：DTC.Server.TableName ：DTC_Server
            /// </summary>
            public static string TableName
            {
                get
                {
                    return AppConfig.GetApp("DTC.Server.TableName", "DTC_Server");
                }
                set
                {
                    AppConfig.SetApp("DTC.Server.TableName", value);
                }
            }
            /// <summary>
            /// RabbitMQ 链接配置
            /// 配置项：DTC.Server.Rabbit=127.0.0.1;guest;guest;/
            /// </summary>
            public static string Rabbit
            {
                get
                {
                    return AppConfig.GetApp("DTC.Server.Rabbit");
                }
                set
                {
                    AppConfig.SetApp("DTC.Server.Rabbit", value);
                }
            }
            /// <summary>
            /// Kafka 链接配置
            /// 配置项：DTC.Server.Kafka=127.0.0.1:9092
            /// </summary>
            public static string Kafka
            {
                get
                {
                    return AppConfig.GetApp("DTC.Server.Kafka");
                }
                set
                {
                    AppConfig.SetApp("DTC.Server.Kafka", value);
                }
            }
            /// <summary>
            /// MQ相关配置项
            /// </summary>
            public static class MQ
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
                            return "DTC_Server_Default";
                        }
                    }

                    /// <summary>
                    /// DTC 默认交换机名称，绑定所有Confirm队列
                    /// </summary>
                    internal static string ConfirmExChange
                    {
                        get
                        {
                            return "DTC_Server_Confirm";
                        }
                    }
                    /// <summary>
                    /// 首次队列往这发，比较急。
                    /// </summary>
                    internal static string DefaultQueue
                    {
                        get
                        {
                            return "DTC_Server_Default_" + ProjectName;
                        }
                    }

                    /// <summary>
                    /// 确认删除队列往这发。
                    /// </summary>
                    internal static string ConfirmQueue
                    {
                        get
                        {
                            return "DTC_Server_Confirm_" + ProjectName;
                        }
                    }
                }
                public static class Kafka
                {
                    internal static string DefaultTopic
                    {
                        get
                        {
                            return "DTC_Server_Default";
                        }
                    }

                   
                    internal static string ConfirmTopic
                    {
                        get
                        {
                            return "DTC_Server_Confirm";
                        }
                    }
                  
                    internal static string DefaultGroup
                    {
                        get
                        {
                            return "DTC_Server_Default_" + ProjectName;
                        }
                    }

                  
                    internal static string ConfirmGroup
                    {
                        get
                        {
                            return "DTC_Server_Confirm_" + ProjectName;
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
                /// 数据已处理完成，但未收后客户端确认时：向客户端发起重新确认以便删除数据的间隔时间：单位（秒），默认10分钟。
                /// 配置项：DTC.Server.RetryIntervalSecond ：0
                /// </summary>
                public static int RetryIntervalSecond
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTC.Server.RetryIntervalSecond", 10 * 60);
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Server.RetryIntervalSecond", ((int)value).ToString());
                    }
                }

                /// <summary>
                /// 数据确认完成后：0 删除（默认）、1 转移到历史表
                /// 配置项：DTC.Server.ConfirmClearMode ：0
                /// </summary>
                public static ClearMode ConfirmClearMode
                {
                    get
                    {
                        return (ClearMode)AppConfig.GetAppInt("DTC.Server.ConfirmClearMode", (int)ClearMode.Delete);
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Server.ConfirmClearMode", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 数据超时后（即未处理或处理完未收到客户端确认时）保留时间：（单位秒），默认7天。
                /// 配置项：DTC.Server.TimeoutKeepSecond ：7 * 24 * 3600
                /// </summary>
                public static int TimeoutKeepSecond
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTC.Server.TimeoutKeepSecond", 7 * 24 * 3600);//7 * 24 * 3600
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Server.TimeoutKeepSecond", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 数据超时后：0 删除、1 转移到历史表（默认）
                /// 配置项：DTC.Server.TimeoutClearMode ：1
                /// </summary>
                public static ClearMode TimeoutClearMode
                {
                    get
                    {
                        return (ClearMode)AppConfig.GetAppInt("DTC.Server.TimeoutClearMode", (int)ClearMode.MoveToHistoryTable);
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Server.TimeoutClearMode", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 向客户端发起重新确认以便删除数据的最大重试次数，默认7次。
                /// 配置项：DTC.Server.MaxRetries ：7
                /// </summary>
                public static int MaxRetries
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTC.Server.MaxRetries", 7);
                    }
                    set
                    {
                        AppConfig.SetApp("DTC.Server.MaxRetries", ((int)value).ToString());
                    }
                }
            }

        }
    }
}
