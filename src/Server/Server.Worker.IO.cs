using CYQ.Data;
using CYQ.Data.Cache;
using CYQ.Data.Json;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Taurus.Plugin.DistributedLock;
using static System.Net.Mime.MediaTypeNames;

namespace Taurus.Plugin.DistributedTransaction
{
    public static partial class DTC
    {
        public static partial class Server
        {
            /// <summary>
            /// dtc 写数据库、写队列
            /// </summary>
            internal static partial class Worker
            {
                internal static class IO
                {
                    private static string serverPath = AppConfig.WebRootPath + "App_Data/dtc/server/";
                    private static string GetKey(string key)
                    {
                        return "DTC.Server:" + key;
                    }

                    /// <summary>
                    /// 写入数据
                    /// </summary>
                    public static bool Write(Table table)
                    {
                        string json = table.ToJson();
                        string path = serverPath + table.TraceID.Replace(':', '_') + "/" + table.MsgID + ".txt";
                        return IOHelper.Write(path, json);

                    }

                    /// <summary>
                    /// 删除数据
                    /// </summary>
                    public static bool Delete(string traceID, string msgID, string exeType)
                    {
                        string path = serverPath + traceID.Replace(':', '_') + "/" + msgID + ".txt";
                        return IOHelper.Delete(path);
                    }

                    /// <summary>
                    /// 获取数据：仅commit、rollback用到
                    /// </summary>
                    public static List<Table> GetListByTraceID(string traceID, string exeType)
                    {
                        List<Table> tables = new List<Table>();

                        string folder = serverPath + traceID.Replace(':', '_');
                        string[] files = IOHelper.GetFiles(folder);
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

                        return tables;
                    }

                    /// <summary>
                    /// 获取超时需要删除的数据，仅硬盘文件需要删除。
                    /// </summary>
                    public static void DeleteTimeoutTable()
                    {
                        var disCache = DistributedCache.Instance;
                        if (disCache.CacheType == CacheType.LocalCache)
                        {
                            try
                            {
                                DirectoryInfo directoryInfo = new DirectoryInfo(serverPath);
                                if (directoryInfo.Exists)
                                {
                                    //System.IO.Directory.em
                                    FileInfo[] files = directoryInfo.GetFiles("*.txt", SearchOption.AllDirectories);
                                    if (files != null && files.Length > 0)
                                    {

                                        int timeoutSecond = DTCConfig.Server.Worker.TimeoutKeepSecond;
                                        foreach (FileInfo file in files)
                                        {
                                            if (file.LastWriteTime < DateTime.Now.AddSeconds(-timeoutSecond))
                                            {
                                                file.Delete();
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                Log.Error(err);
                            }
                        }
                    }

                    /// <summary>
                    /// 删除空目录文件夹【由提交和回滚产生的】
                    /// </summary>
                    public static void DeleteEmptyDirectory()
                    {
                        try
                        {
                            if (IOHelper.ExistsDirectory(serverPath))
                            {
                                //删除空文件夹
                                string[] folders = Directory.GetDirectories(serverPath);
                                if (folders != null && folders.Length > 0)
                                {
                                    foreach (string path in folders)
                                    {
                                        if (IOHelper.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length == 0)
                                        {
                                            IOHelper.DeleteDirectory(path, false);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Error(err);
                        }

                    }
                }
            }

        }
    }
}
