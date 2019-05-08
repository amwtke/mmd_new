using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Group
{
    public class VerifyParameter :BaseParameter
    {
        public Guid oid { get; set; }
    }
}