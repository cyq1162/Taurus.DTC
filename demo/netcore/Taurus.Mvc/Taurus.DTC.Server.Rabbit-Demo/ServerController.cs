using System;
using Taurus.Mvc.Attr;
using Taurus.Plugin.DistributedTransaction;

namespace DTC_Server_Rabbit_Demo
{
    /// <summary>
    /// transation api provier
    /// </summary>
    public class ServerController : Taurus.Mvc.Controller
    {

        /// <summary>
        /// provide a Create api , and it provide a transation , call https://localhost:5001/server/create
        /// </summary>
        [HttpPost]
        [HttpGet]
        [Require("name")]
        public void Create(string name)
        {
            //do something insert
            int createID = 123456;
            //here will receive a header:X-Request-ID 
            if (DTC.Server.Subscribe(createID.ToString(), "OnCreate"))
            {
                Console.WriteLine("call : DTC.Server.Subscribe call.");
            }
            Write(createID, true);
        }


        [DTCServerSubscribe("OnCreate")]
        private static bool AnyMethodNameForOnCreateCallBack(DTCServerSubscribePara para)
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
