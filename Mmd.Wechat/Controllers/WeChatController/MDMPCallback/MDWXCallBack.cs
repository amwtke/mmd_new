using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using mmd.wechat.Controllers.Base;

namespace MD.Wechat.Controllers.MDMPCallback
{
    public class MDWXCallBack
    {
        /// <summary>
        /// 美美哒自己公众号跳转页。
        /// </summary>
        [RoutePrefix("wxcallback")]
        [Route("{action=index}")]
        public class WeChatCallBackController : MVCNeedWeixinCallBackBaseController
        {
            // GET: WeChatCallBack
            public async System.Threading.Tasks.Task<ActionResult> Index(string code, string state)
            {
                // TODO: mmd自己微信公众号的页面跳转，以后可能会用到。
                //return await LoginCallBack(code, state, @"F2E/xuesheng_findds.html", @"F2E/dynamic_list.html");
                return null;
            }
        }
    }
}