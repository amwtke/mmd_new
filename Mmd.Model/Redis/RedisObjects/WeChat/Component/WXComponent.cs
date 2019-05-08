using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.WeChat.Component
{
    [RedisDBNumber("0")]
    public class WXComponentTicketRedis
    {
        [RedisKey]
        [ComponentTicket("WX.Component.Tikcket")]
        public string Ticket { get; set; }
    }

    public class ComponentTicketAttribute : RedisStringAttribute
    {
        public ComponentTicketAttribute(string name) : base(name)
        {
        }
    }



    [RedisDBNumber("0")]
    public class WXComponentAccessTokeRedis
    {
        [RedisKey]
        [ComponentAccessToken("WX.Component.AccessToken")]
        public string AccessToken { get; set; }

        [ComponentAccessTokenExpireIn("WX.Component.AccessToken.ExpireIn")]
        public string ExpireIn { get; set; }
    }

    public class ComponentAccessTokenAttribute : RedisStringAttribute
    {
        public ComponentAccessTokenAttribute(string name) : base(name)
        {
        }
    }

    public class ComponentAccessTokenExpireInAttribute : RedisStringAttribute
    {
        public ComponentAccessTokenExpireInAttribute(string name) : base(name) { }
    }



    [RedisDBNumber("0")]
    public class WXComponentPreAutCodeRedis
    {
        [RedisKey]
        [ComponentPreauthcode("WX.Component.PreAuthCode")]
        public string PreCode { get; set; }

        [ComponentPreauthcodeExpireIn("WX.Component.PreAuthCode.ExpireIn")]
        public string ExpireIn { get; set; }
    }

    public class ComponentPreauthcodeAttribute : RedisStringAttribute
    {
        public ComponentPreauthcodeAttribute(string name) : base(name)
        {
        }
    }

    public class ComponentPreauthcodeExpireInAttribute : RedisStringAttribute
    {
        public ComponentPreauthcodeExpireInAttribute(string name) : base(name) { }
    }


    [RedisDBNumber("0")]
    public class WXComponentAuthorCodeRedis
    {
        [RedisKey]
        [ComponentAuthCode("WX.Component.AuthCode")]
        public string PreCode { get; set; }

        [ComponentAuthCodeExpireIn("WX.Component.AuthCode.ExpireIn")]
        public string ExpireIn { get; set; }
    }

    public class ComponentAuthCodeAttribute : RedisStringAttribute
    {
        public ComponentAuthCodeAttribute(string name) : base(name)
        {
        }
    }

    public class ComponentAuthCodeExpireInAttribute : RedisStringAttribute
    {
        public ComponentAuthCodeExpireInAttribute(string name) : base(name) { }
    }
}
