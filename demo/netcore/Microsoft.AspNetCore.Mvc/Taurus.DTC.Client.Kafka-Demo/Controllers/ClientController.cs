﻿using CYQ.Data.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Taurus.Plugin.DistributedTransaction;

namespace DTC_Client_Kafka_Demo
{
    /// <summary>
    /// api demo for kafka , start https://localhost:44370/client/transation
    /// </summary>
    [Route("client/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        public ClientController()
        {
            DTCConfig.Client.IsPrintTraceLog = true;
        }
        // GET api/values
        [HttpGet]
        [Route("~/client/get")]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// to call commit transation , start https://localhost:44370/client/transation
        /// </summary>
        [HttpGet]
        [Route("~/client/transation")]
        public string Transation()
        {
            WebClient wc = new WebClient();
            wc.Headers.Add("X-Request-ID",System.Web.HttpContext.Current.GetTraceID());
            string json = wc.DownloadString("https://localhost:5001/server/create?name=hello world");
            //string json= Encoding.UTF8.GetString(data);
            //do something
            if (!string.IsNullOrEmpty(json))
            {
                if (JsonHelper.IsSuccess(json))
                {
                    if (DTC.Client.CommitAsync(1, "OnOK"))
                    {
                        Console.WriteLine("call : DTC.Client.CommitAsync.");
                    }
                    return JsonHelper.OutResult(true, "Commit OK.");
                }
            }
            if (DTC.Client.RollBackAsync(1, "OnFail"))
            {
                Console.WriteLine("call : DTC.Client.RollBackAsync call.");
            }
            return JsonHelper.OutResult(true, "RollBack ing....");
        }


        [DTCCallBack("OnFail")]
        [DTCCallBack("OnOK")]
        private void OnCallBack(DTCCallBackPara para)
        {
            Console.WriteLine("call back : " + para.ExeType + " - " + para.CallBackKey + " - " + para.CallBackContent);
        }
    }
}
