using CYQ.Data.Json;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Taurus.Plugin.DistributedTransaction;

namespace Taurus_DTC_Server_Kafka_Demo_Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServerController : ControllerBase
    {
        public ServerController()
        {
            DTCConfig.Server.IsPrintTraceLog = true;
        }
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        //private readonly ILogger<ServerController> _logger;

        //public ServerController(ILogger<ServerController> logger)
        //{
        //    _logger = logger;
        //}
        [Route("~/server/get")]
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return Summaries.ToArray();
        }


        /// <summary>
        /// provide a Create api , and it provide a transation , call https://localhost:5001/server/create
        /// </summary>
        [HttpGet]
        [HttpPost]
        [Route("~/server/create")]
        public string Create(string name)
        {
            //do something insert
            int createID = 123456;
            //here will receive a header:X-Request-ID 
            if (DTC.Server.Subscribe(createID.ToString(), "OnCreate"))
            {
                Console.WriteLine("call : DTC.Server.Subscribe call.");
            }
            return JsonHelper.OutResult(true, createID);
        }


        [DTCSubscribe("OnCreate")]
        private static bool AnyMethodNameForOnCreateCallBack(DTCSubscribePara para)
        {
            para.CallBackContent = "what message you need?";
            Console.WriteLine("call back :" + para.ExeType + " , content :" + para.Content);
            if (para.ExeType == ExeType.Commit) { return true; }
            if (para.ExeType == ExeType.RollBack)
            {
                string createID = para.Content;
                //return DeleteByID(createID);
                return true;
            }
            return false;
        }
    }
}
