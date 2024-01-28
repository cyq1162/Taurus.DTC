using CYQ.Data;
using System;
//using Taurus.Plugin.Admin;


namespace Taurus.Plugin.DistributedTransaction
{
    /// <summary>
    /// DTC 分布式事务协调器，Distributed Transaction Coordinator
    /// DTC 分布式任务协调器，Distributed Task Coordinator
    /// </summary>
    public partial class DTC
    {
        private static string _Version;
        /// <summary>
        /// 获取当前 Taurus 版本号
        /// </summary>
        internal static string Version
        {
            get
            {
                if (_Version == null)
                {
                    _Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
                return _Version;
            }
        }
        public static partial class Client
        {
            /// <summary>
            /// 启动定时描述，并监听默认队列。
            /// </summary>
            public static void Start()
            {
                if (DTCConfig.Client.IsEnable)
                {
                    //DTCAdminController.Init();
                    DTCConsole.WriteDebugLine("--------------------------------------------------");
                    DTCConsole.WriteDebugLine("DTC.Client.Start = true , Version = "+ Version);
                    DTCConsole.WriteDebugLine("--------------------------------------------------");
                    DTC.Client.Worker.DBScanner.Start();
                }
            }
        }
        public static partial class Server
        { /// <summary>
          /// 启动定时描述，并监听默认队列。
          /// </summary>
            public static void Start()
            {
                if (DTCConfig.Server.IsEnable)
                {
                    //DTCAdminController.Init();
                    DTCConsole.WriteDebugLine("--------------------------------------------------");
                    DTCConsole.WriteDebugLine("DTC.Server.Start = true , Version = "+ Version);
                    DTCConsole.WriteDebugLine("--------------------------------------------------");
                    DTC.Server.Worker.DBScanner.Start();
                }
            }
        }

        /// <summary>
        /// 同时启动客户端和服务端定时扫描程序。
        /// </summary>
        public static void Start()
        {
            Client.Start();
            Server.Start();
        }
    }

}
