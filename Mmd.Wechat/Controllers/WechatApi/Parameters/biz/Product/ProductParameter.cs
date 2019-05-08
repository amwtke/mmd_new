using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Product
{
    public class ProductParameter : BaseParameter
    {
        public int? category { get; set; }
        public Guid pid { get; set; }
        public int? score { get; set; }
        public string comment { get; set; }
        public Guid pcid { get; set; }
        public string[] imgList { get; set; }

    }
}