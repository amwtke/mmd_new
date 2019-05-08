using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Activity
{
    public class ladderParameter : BaseParameter
    {
        public Guid gid { get; set; }
        public Guid oid { get; set; }
        public Guid goid { get; set; }
    }
}