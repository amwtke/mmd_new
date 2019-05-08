using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Redis;

namespace MD.Model.Redis.Objects.WeChat
{
    [RedisDBNumber("0")]
    public class WXTokenRedis
    {
        [RedisKey]
        public string UslessKey { get; set; }
        [AccessTokenString("WX.MD.AccessToken")]
        public string AccessToken { get; set; }
        [AccessTokenExpireInString("WX.MD.AccessToken.ExpireIn")]
        public string ExpireIn { get; set; }
    }

    public class AccessTokenStringAttribute : RedisStringAttribute
    {
        public  AccessTokenStringAttribute(string name) : base(name) { }
    }

    public class AccessTokenExpireInStringAttribute : RedisStringAttribute
    {
        public AccessTokenExpireInStringAttribute(string name) : base(name) { }
    }
}
