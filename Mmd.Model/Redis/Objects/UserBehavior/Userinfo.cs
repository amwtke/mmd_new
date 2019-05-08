using MD.Model.Redis.att.CustomAtts.sets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects
{
    [RedisHash("userinfo.hash")]
    [RedisDBNumber("0")]
    public class UserInfoRedis
    {
        [UserInfoSetOpenIdSet("openid.set")]
        //[RedisZSet("openid_zset", 1.0)]
        //[RedisList("openid_list", ListPush.Left)]
        [RedisKey]
        public string Openid { get; set; }

        [UserInfoSetUnionIdSet("unionid.set")]
        //[RedisZSet("unionid_zset", 2.0)]
        //[RedisList("unionid_list", ListPush.Right)]
        [RedisHashEntry("Unionid")]
        public string Unionid { get; set; }

        [RedisHashEntry("Country")]
        public string Country { get; set; }

        [RedisHashEntry("City")]
        public string City { get; set; }

        [RedisHashEntry("Province")]
        public string Province { get; set; }

        [RedisHashEntry("Sex")]
        public string Sex { get; set; }

        [RedisHashEntry("NiceName")]
        public string NiceName { get; set; }

        [RedisHashEntry("HeadImageUrl")]
        public string HeadImageUrl { get; set; }

        [RedisHashEntry("AccessToken")]
        public string AccessToken { get; set; }

        [RedisHashEntry("RefreshToken")]
        public string RefreshToken { get; set; }

        [RedisHashEntry("ExpireIn")]
        public string ExpireIn { get; set; }

        #region PreRegister
        [RedisHashEntry("PreRegisterAccount")]
        public string PreRegisterAccount { get; set; }
        [RedisHashEntry("PreRegisterValidationCode")]
        public string PreRegisterValidationCode { get; set; }
        [RedisHashEntry("PreRegisterTryTimes")]
        public string PreRegisterTryTimes { get; set; }

        #endregion
    }
}
