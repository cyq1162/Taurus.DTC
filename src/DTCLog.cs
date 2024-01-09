using System;
using System.Diagnostics;

namespace Taurus.Plugin.DistributedTransaction
{
    internal class DTCLog
    {
        public static void WriteDebugLine(string msg)
        {
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }
    }
}
