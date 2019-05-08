using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters
{
    public class BaseParameter
    {
        public Guid mid { get; set; }
        public string appid { get; set; }
        public string openid { get; set; }
        public Guid uid { get; set; }
        public int pageIndex { get; set; }
        public int pageSize { get; set; }
        public double? from { get; set; }
        public double? to { get; set; }
        public string QueryStr { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public string shareopenid { get; set; }
    }
}