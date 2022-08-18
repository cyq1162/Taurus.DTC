using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Text;
using Taurus.Core;

namespace Taurus.Controllers
{
    /// <summary>
    /// 微服务测试
    /// </summary>
    public class APIController : Controller
    {
        /// <summary>
        /// Get or Post 测试
        /// </summary>
        [HttpGet]
        public void Hello()
        {
            if (Request.UrlReferrer != null)
            {
                Write("From : " + Request.UrlReferrer.ToString() + "<br/>");
            }
            Write("MicroService : " + MicroService.Config.AppRunUrl + Request.Url.LocalPath + " : Hello ： " + MicroService.Config.ServerName);
        }
    }
}
