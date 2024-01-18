using CYQ.Data.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Taurus.Plugin.DistributedTransaction;
using Taurus.Plugin.MicroService;

namespace Taurus.DTC_Demo.ClientControllers
{
    /// <summary>
    /// api demo for rabbitmq , start https://localhost:5000/client/transation
    /// </summary>
    public class ClientController : Taurus.Mvc.Controller
    {
        /// <summary>
        /// to call commit transation , start https://localhost:5000/client/transation
        /// </summary>
        [HttpGet]
        public void Transation()
        {
            //do something
            RpcTask task = Rpc.StartPostAsync("https://localhost:5001/server/create", Encoding.UTF8.GetBytes("name=hello world"));
            if (task.Result.IsSuccess)
            {
                if (JsonHelper.IsSuccess(task.Result.ResultText))
                {
                    if (DTC.Client.CommitAsync(1, "OnOK"))
                    {
                        Console.WriteLine("call : DTC.Client.CommitAsync.");
                    }
                    Write("Commit OK.", true);
                    return;
                }
            }
            if (DTC.Client.RollBackAsync(1, "OnFail"))
            {
                Console.WriteLine("call : DTC.Client.RollBackAsync call.");
            }
            Write("RollBack ing....", false);
        }


        [DTCClientCallBack("OnFail")]
        [DTCClientCallBack("OnOK")]
        private void OnCallBack(DTCClientCallBackPara para)
        {
            Console.WriteLine("call back : " + para.ExeType + " - " + para.CallBackKey + " - " + para.CallBackContent);
        }

    }
}
