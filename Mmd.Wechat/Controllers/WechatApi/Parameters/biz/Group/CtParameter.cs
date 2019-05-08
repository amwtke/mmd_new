using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Group
{
    public class CtParameter : BaseParameter
    {
        public int fee { get; set; }
        public Guid goid { get; set; }
        public Guid wopid { get; set; }
        public string user_name { get; set; }
        public string tel { get; set; }
        public Guid gid { get; set; }
        public int waytoget { get; set; }
        public Guid upid { get; set; }
        public int post_price { get; set; }
    }
}