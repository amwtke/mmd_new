using MD.Wechat.Controllers.WechatApi.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Activity
{
    public class findboxParameter:BaseParameter
    {
        public Guid bid { get; set; }
        public Guid utid { get; set; }
    }
}