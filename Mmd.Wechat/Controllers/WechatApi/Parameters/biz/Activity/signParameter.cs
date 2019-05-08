using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Activity
{
    public class signParameter:BaseParameter
    {
        public Guid sid { get; set; }
        public Guid usid { get; set; }
    }
}