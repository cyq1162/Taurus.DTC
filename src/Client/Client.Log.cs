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
            internal static partial class Log
            {
                public static void Print(string msg)
                {
                    if(DTCConfig.Client.IsPrintTraceLog)
                    {
                        CYQ.Data.Log.Write(msg, "DTC.Client.TraceLog");
                    }
                }
                public static void Error(Exception err)
                {
                    CYQ.Data.Log.Write(err, "DTC.Client.Error");
                }
            }
        }
    }
}
