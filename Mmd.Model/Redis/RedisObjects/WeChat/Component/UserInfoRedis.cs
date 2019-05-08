using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.WeChat.Component
{
    [RedisHash("WX.Component.User.hash")]
    [RedisDBNumber("0")]
    public class UserInfoRedis
    {
        [RedisKey]
        [RedisHashEntry("Openid")]
        public string Openid { get; set; }

        [RedisHashEntry("NickName")]
        public string NickName { get; set; }

        /// <summary>
        /// 用户的性别，值为1时是男性，值为2时是女性，值为0时是未知
        /// </summary>
        [RedisHashEntry("Sex")]
        public string Sex { get; set; }

        [RedisHashEntry("Province")]
        public string Province { get; set; }

        [RedisHashEntry("City")]
        public string City { get; set; }

        [RedisHashEntry("HeadImgUrl")]
        public string HeadImgUrl { get; set; }

        [RedisHashEntry("Country")]
        public string Country { get; set; }

        [RedisHashEntry("Privilege")]
        public string Privilege { get; set; }

        [RedisHashEntry("Unionid")]
        public string Unionid { get; set; }

        [RedisHashEntry("ExpireIn")]
        public string ExpireIn { get; set; }

        [RedisHashEntry("Uid")]
        public string Uid { get; set; }
    }

    [RedisHash("md.useropenid.map.hash")]
    [RedisDBNumber("0")]
    public class UserOpenIdMapRedis
    {
        [RedisKey]
        public string Uid { get; set; }

        [RedisHashEntry("OpenId")]
        public string OpenId { get; set; }
    }

    [RedisHash("md.usersub.map.hash")]
    [RedisDBNumber("0")]
    public class UserSubMapRedis
    {
        [RedisKey]
        public string OpenId { get; set; }

        [RedisHashEntry("Appid")]
        public string Appid { get; set; }
    }
}
