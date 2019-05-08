using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.User
{
    public class UserParameter:BaseParameter
    {
        public Guid gid { get; set; }
        public int age { get; set; }
        public int skinCode { get; set; }
        public Guid upid { get; set; }
        public string name { get; set; }
        public string cellphone { get; set; }
        public string province { get; set; }
        public string city { get; set; }
        public string district { get; set; }
        public string districtcode { get; set; }
        public string address { get; set; }
        public bool is_default { get; set; }
    }
}