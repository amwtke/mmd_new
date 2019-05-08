using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Supply
{
    public class SupplyParameter:BaseParameter
    {
        public Guid sid { get; set; }
        public int? category { get; set; }
    }
}