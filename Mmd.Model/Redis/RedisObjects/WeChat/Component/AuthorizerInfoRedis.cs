using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Redis.att.CustomAtts.lists;
using MD.Model.Redis.att.CustomAtts.sets;
using MD.Model.Redis.att.CustomAtts.zsets;

namespace MD.Model.Redis.RedisObjects.WeChat.Component
{
    [RedisHash("WX.Component.AuthorizerInfo.hash")]
    [RedisDBNumber("0")]
    public class AuthorizerInfoRedis
    {
        [RedisKey]
        [RedisHashEntry("Appid")]
        public string Appid { get; set; }

        /// <summary>
        /// authorizer_accesstoken用于调用第三方mp的接口的at。
        /// </summary>
        [RedisHashEntry("AccessToken")]
        public string AccessToken { get; set; }

        [RedisHashEntry("ExpireIn")]
        public string ExpireIn { get; set; }

        [RedisHashEntry("AccessRefreshToken")]
        public string AccessRefreshToken { get; set; }

        /// /////
        [RedisHashEntry("NiceName")]
        public string NiceName { get; set; }

        [RedisHashEntry("HeadImgUrl")]
        public string HeadImgUrl { get; set; }

        /// <summary>
        /// 授权方公众号的原始ID
        /// </summary>
        [RedisHashEntry("UserName")]
        public string UserName { get; set; }

        /// <summary>
        /// 授权方公众号所设置的微信号，可能为空
        /// </summary>
        [RedisHashEntry("Alias")]
        public string Alias { get; set; }

        /// <summary>
        /// 二维码图片的URL，开发者最好自行也进行保存
        /// </summary>
        [RedisHashEntry("QrCodeUrl")]
        public string QrCodeUrl { get; set; }

        [RedisHashEntry("ServiceType")]
        public string ServiceType { get; set; }

        [RedisHashEntry("VerifyType")]
        public string VerifyType { get; set; }

        /// <summary>
        /// {1,1,1,1}
        /// open_pay、open_shake、open_card、open_store
        /// </summary>
        [RedisHashEntry("BusinessInfo")]
        public string BusinessInfo { get; set; }

        /// <summary>
        /// 1-15
        /// 消息管理权限
            //用户管理权限
            //帐号服务权限
            //网页服务权限
            //微信小店权限
            //微信多客服权限
            //群发与通知权限
            //微信卡券权限
            //微信扫一扫权限
            //微信连WIFI权限
            //素材管理权限
            //微信摇周边权限
            //微信门店权限
            //微信支付权限
            //自定义菜单权限
        /// </summary>
        [RedisHashEntry("FuncInfo")]
        public string FuncInfo { get; set; }

        /// <summary>
        /// 网页授权的accesstoken
        /// </summary>
        [RedisHashEntry("UserAccessToken")]
        public string UserAccessToken { get; set; }

        [RedisHashEntry("UserAccessTokenExpireIn")]
        public string UserAccessTokenExpireIn { get; set; }

        [RedisHashEntry("UserAccessRefreshToken")]
        public string UserAccessRefreshToken { get; set; }

        [RedisHashEntry("JsSdkTicket")]
        public string JsSdkTicket { get; set; }

        [RedisHashEntry("JsSdkTicketExpireIn")]
        public string JsSdkTicketExpireIn { get; set; }
    }
}
