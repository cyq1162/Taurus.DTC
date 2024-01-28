using System;
using CYQ.Data.Tool;
using CYQ.Data.Table;
using Taurus.Mvc;
using CYQ.Data;
using Taurus.Plugin.MicroService;
using Taurus.Plugin.Limit;
using Taurus.Plugin.Doc;
using System.Configuration;
using System.IO;
using Taurus.Mvc.Reflect;
using Newtonsoft.Json.Linq;
using Taurus.Plugin.DistributedTransaction;
using Taurus.Mvc.Attr;

namespace Taurus.Plugin.Admin
{
    /// <summary>
    /// 后台配置管理
    /// </summary>
    public class DTCAdminController : Taurus.Mvc.Controller
    {
        public override bool BeforeInvoke()
        {
            return AdminAPI.Auth.IsOnline(this.Context, true);
        }
        private static bool hasInit = false;
        /// <summary>
        /// 初始化
        /// </summary>
        internal static void Init()
        {
            if (hasInit) { return; }
            hasInit = true;

            if (DTCConfig.Client.IsEnable)
            {
                string path = AdminConfig.Path + "/config_dtc_client";
                RouteEngine.Add(path, "/dtcadmin/config_dtc_client");
                AdminAPI.ExtMenu.Add("DTC", "Client", path);
            }
            if (DTCConfig.Server.IsEnable)
            {
                string path = AdminConfig.Path + "/config_dtc_server";
                RouteEngine.Add(path, "/dtcadmin/config_dtc_server");
                AdminAPI.ExtMenu.Add("DTC", "Server", path);
            }

        }
        protected override string HtmlFolderName
        {
            get
            {
                return AdminConfig.HtmlFolderName;
            }
        }

        protected override string HtmlFileName
        {
            get
            {
                return "configitems";
            }
        }

        public void Config_DTC_Client()
        {
            MDataTable dt = new MDataTable();
            dt.Columns.Add("ConfigKey,ConfigValue,Description");

            Sets(dt, "DTC.Client.IsEnable", DTCConfig.Client.IsEnable, "Taurus.DTC is enable.");
            Sets(dt, "DTC.Client.IsPrintTraceLog", DTCConfig.Client.IsPrintTraceLog, "Print trace log 【to App_data/logs/】 for debug.");
            Sets(dt, "DTC.Client.Rabbit", DTCConfig.Client.Rabbit, "RabbitMQ connection string 【ip;username;password;virtualpath】.");
            Sets(dt, "DTC.Client.Kafka", DTCConfig.Client.Kafka, "Kafka connection string 【ip:port】.");
            Sets(dt, "DTC.Client.Conn", DTCConfig.Client.Conn, "Database connection string for save task log.");
            Sets(dt, "DTC.Client.TableName", DTCConfig.Client.TableName, "Database's table name for save task log.");
            dt.NewRow(true);
            Sets(dt, "DTC.Client.ConfirmClearMode", DTCConfig.Client.Worker.ConfirmClearMode, "0 - delete , 1 - move to history table.");
            Sets(dt, "DTC.Client.TimeoutClearMode", DTCConfig.Client.Worker.TimeoutClearMode, "0 - delete , 1 - move to history table.");
            Sets(dt, "DTC.Client.RetryIntervalSecond", DTCConfig.Client.Worker.RetryIntervalSecond, "The interval time between task restarts, in seconds.");
            Sets(dt, "DTC.Client.TimeoutKeepSecond", DTCConfig.Client.Worker.TimeoutKeepSecond, "Data retention time when task timeout occurs.");
            Sets(dt, "DTC.Client.MaxRetries", DTCConfig.Client.Worker.MaxRetries, "Maximum number of task retries.");
            dt.Bind(View);
        }
        public void Config_DTC_Server()
        {
            MDataTable dt = new MDataTable();
            dt.Columns.Add("ConfigKey,ConfigValue,Description");

            Sets(dt, "DTC.Server.IsEnable", DTCConfig.Server.IsEnable, "Taurus.DTC is enable.");
            Sets(dt, "DTC.Server.IsPrintTraceLog", DTCConfig.Server.IsPrintTraceLog, "Print trace log 【to App_data/logs/】 for debug.");
            Sets(dt, "DTC.Server.Rabbit", DTCConfig.Server.Rabbit, "RabbitMQ connection string 【ip;username;password;virtualpath】.");
            Sets(dt, "DTC.Server.Kafka", DTCConfig.Server.Kafka, "Kafka connection string 【ip:port】.");
            Sets(dt, "DTC.Server.Conn", DTCConfig.Server.Conn, "Database connection string for save task log.");
            Sets(dt, "DTC.Server.TableName", DTCConfig.Server.TableName, "Database's table name for save task log.");
            dt.NewRow(true);
            Sets(dt, "DTC.Server.ConfirmClearMode", DTCConfig.Server.Worker.ConfirmClearMode, "0 - delete , 1 - move to history table.");
            Sets(dt, "DTC.Server.TimeoutClearMode", DTCConfig.Server.Worker.TimeoutClearMode, "0 - delete , 1 - move to history table.");
            Sets(dt, "DTC.Server.RetryIntervalSecond", DTCConfig.Server.Worker.RetryIntervalSecond, "The interval time between task restarts, in seconds.");
            Sets(dt, "DTC.Server.TimeoutKeepSecond", DTCConfig.Server.Worker.TimeoutKeepSecond, "Data retention time when task timeout occurs.");
            Sets(dt, "DTC.Server.MaxRetries", DTCConfig.Server.Worker.MaxRetries, "Maximum number of task retries.");
            dt.Bind(View);
        }
        private void Sets(MDataTable dt, string key, object objValue, string description)
        {
            string value = Convert.ToString(objValue);
            if (objValue is Boolean)
            {
                value = value == "True" ? "√" : "×";
            }
            if (AdminAPI.Durable.ContainsKey(key))
            {
                value = value + " 【durable】";
            }
            else if (AdminAPI.Durable.ContainsTempKey(key))
            {
                value = value + " 【temp modify】";
            }
            dt.NewRow(true).Sets(0, key, value, description);
        }
    }
}
