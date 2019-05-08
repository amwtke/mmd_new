using MD.Lib.ElasticSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Group
{
    public class ShareParameter:BaseParameter
    {
        public Guid gid { get; set; }
        public string message { get; set; }
    }
}