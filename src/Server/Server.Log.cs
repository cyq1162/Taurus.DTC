using CYQ.Data;
using System;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTC
    {
        /// <summary>
        /// 分布式事务 提供端
        /// </summary>
        public static partial class Server
        {
            internal static partial class Log
            {
                public static void Print(string msg)
                {
                    if(DTCConfig.Server.IsPrintTraceLog)
                    {
                        CYQ.Data.Log.Write(msg, "DTC.Server.TraceLog");
                    }
                }
                public static void Error(Exception err)
                {
                    CYQ.Data.Log.Write(err, "DTC.Server.Error");
                }
            }
        }
    }
}
