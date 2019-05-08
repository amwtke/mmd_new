using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Order
{
    public class OrderParameter :BaseParameter
    {
        public Guid oid { get; set; }
        public Guid pid { get; set; }
        public Guid gid { get; set; }
        public int waytoget { get; set; }
       public string companyCode { get; set; }
        public string number { get; set; }
    }
}