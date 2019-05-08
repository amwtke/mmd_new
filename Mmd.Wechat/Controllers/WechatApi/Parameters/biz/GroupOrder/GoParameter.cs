using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.GroupOrder
{
    public class GoParameter:BaseParameter
    {
        public Guid goid { get; set; }

        public Guid gid { get; set; }
    }
}