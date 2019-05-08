using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace MD.WeChat.Filters
{
    /// <summary>
    /// 只能在http://mmpintuan.com/api/....这个路径访问
    /// </summary>
    public class AccessFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var url = actionContext.Request.RequestUri;
            if (url.Authority.ToString().Split(new char[] { '.' }).Length >2 )//只有一级域名可以调用web api//&& !url.Authority.Contains("localhost"))//==1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("错误路径", Encoding.UTF8)
                };
                actionContext.Response = response;
            }
            else
            {
                base.OnActionExecuting(actionContext);
            }
        }
    }
}