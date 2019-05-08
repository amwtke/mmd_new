using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz
{
    public class WopParameter :BaseParameter
    {
        public Guid wopid { get; set; }

        public Guid gid { get; set; }

    }
}