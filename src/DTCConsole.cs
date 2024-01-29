using CYQ.Data;
using System;
using System.Diagnostics;

namespace Taurus.Plugin.DistributedTransaction
{
    /// <summary>
    /// 控制台输出
    /// </summary>
    internal class DTCConsole
    {
        public static void WriteLine(string msg)
        {
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }
        public static void WriteDebugLine(string msg)
        {
            if (AppConfig.IsDebugMode || (DTCConfig.Server.IsEnable && DTCConfig.Server.IsPrintTraceLog) || (DTCConfig.Client.IsEnable && DTCConfig.Client.IsPrintTraceLog))
            {
                WriteLine(msg);
            }
        }
    }
}
