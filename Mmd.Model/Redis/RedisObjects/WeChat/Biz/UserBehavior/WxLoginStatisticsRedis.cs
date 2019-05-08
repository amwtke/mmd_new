using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.WeChat.Biz.UserBehavior
{
    [RedisDBNumber("15")]
    public class WxLoginStatisticsRedis
    {
        [RedisKey]
        [WxExceptionNumber("Wx.Login.Exception.Count.String")]
        [WxLoginNumber("Wx.Login.Total.Count.String")]
        public string OpenId { get; set; }
    }

    public class WxExceptionNumberAttribute : RedisStringAttribute
    {
        public WxExceptionNumberAttribute(string name) : base(name) { }
    }

    public class WxLoginNumberAttribute : RedisStringAttribute
    {
        public WxLoginNumberAttribute(string name) : base(name) { }
    }
}
