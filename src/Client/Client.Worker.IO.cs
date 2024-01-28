using CYQ.Data;
using CYQ.Data.Tool;
using CYQ.Data.Cache;
using System.Collections.Generic;
using CYQ.Data.Json;
using Taurus.Plugin.DistributedLock;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTC
    {
        public static partial class Client
        {
            /// <summary>
            /// dtc 写数据库、写队列
            /// </summary>
            internal static partial class Worker
            {

                internal static class IO
                {
                    static string clientPath = AppConfig.WebRootPath + "App_Data/dtc/client/";
                    private static string GetKey(string key)
                    {
                        return "DTC.Client:" + key;
                    }
                    /// <summary>
                    /// 写入数据
                    /// </summary>
                    public static bool Write(Table table)
                    {
                        string id = table.TraceID;
                        string json = table.ToJson();
                        string path = clientPath + table.ExeType.ToLower() + "/" + id.Replace(':', '_') + ".txt";
                        return IOHelper.Write(path, json);

                    }
                    /// <summary>
                    /// 删除数据
                    /// </summary>
                    public static bool Delete(string traceID, string exeType)
                    {
                        string id = traceID;
                        string path = clientPath + exeType.ToLower() + "/" + id.Replace(':', '_') + ".txt";
                        return IOHelper.Delete(path);
                    }
                    /// <summary>
                    /// 读取数据
                    /// </summary>
                    public static string Read(string traceID, string exeType)
                    {
                        string id = traceID;
                        string path = clientPath + exeType.ToLower() + "/" + id.Replace(':', '_') + ".txt";
                        return IOHelper.ReadAllText(path);
                    }

                    /// <summary>
                    /// 获取需要扫描重发的数据。
                    /// </summary>
                    public static List<Table> GetScanTable()
                    {
                        List<Table> tables = new List<Table>();
                        if (System.IO.Directory.Exists(clientPath))
                        {
                            string[] files = IOHelper.GetFiles(clientPath, "*.txt", System.IO.SearchOption.AllDirectories);
                            if (files != null && files.Length > 0)
                            {
                                foreach (string file in files)
                                {
                                    string json = IOHelper.ReadAllText(file, 0);
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        tables.Add(JsonHelper.ToEntity<Table>(json));
                                    }
                                }
                            }
                        }

                        return tables;
                    }
                }
            }

        }
    }
}
