using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.WeChat.Component
{
    [RedisDBNumber("0")]
    public class WxJsSdkTiketRedis
    {
        [RedisKey]
        [JsSdkTiket("WX.JsSdk.Tiket")]
        public string Tiket { get; set; }

        [JsSdkTiketExpireIn("WX.JsSdk.Tiket.ExpireIn")]
        public string ExpireIn { get; set; }
    }

    public class JsSdkTiketAttribute : RedisStringAttribute
    {
        public JsSdkTiketAttribute(string name) : base(name)
        {
        }
    }

    public class JsSdkTiketExpireInAttribute : RedisStringAttribute
    {
        public JsSdkTiketExpireInAttribute(string name) : base(name) { }
    }
}
