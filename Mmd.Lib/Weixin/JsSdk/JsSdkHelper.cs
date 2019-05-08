using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Redis;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using Senparc.Weixin.MP.Helpers;
using Senparc.Weixin.Open.ComponentAPIs;
using Senparc.Weixin.Open.Entities;

namespace MD.Lib.Weixin.JsSdk
{
    public static class MdJsSdkHelper
    {
        private static string GetNonce()
        {
            return JSSDKHelper.GetNoncestr();
        }

        private static async Task<string> GetTiket(string appid, string type = "jsapi")
        {
            var authorizer =
                await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<AuthorizerInfoRedis>(appid);
            if(string.IsNullOrEmpty(authorizer?.AccessToken))
                throw new MDException(typeof(MdJsSdkHelper),$"取jsticke出错！appid:{appid}");


            string ticket = authorizer.JsSdkTicket;
            string expireIn = authorizer.JsSdkTicketExpireIn;

            if (string.IsNullOrEmpty(ticket) || string.IsNullOrEmpty(expireIn) || double.Parse(expireIn)<=CommonHelper.GetUnixTimeNow())
            {
                var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
                var ret = await ComponentApi.GetJsApiTicketAsync(authorizerAccessToken);
                ticket = ret.ticket;
                int retExpir = ret.expires_in;
                double expireTime = CommonHelper.GetUnixTimeNow() + retExpir;
                
                //store
                authorizer.JsSdkTicket = ticket;
                authorizer.JsSdkTicketExpireIn = expireTime.ToString();
                await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(authorizer);
            }
            return ticket;
        }

        private static string GetTimestamp()
        {
            return JSSDKHelper.GetTimestamp();
        }

        /// <summary>
        /// timestamp,nonceStr,signature
        /// </summary>
        /// <param name="authorizerAccessToken"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<Tuple<string,string,string>> GetSignatureAsync(string appid, string url)
        {
            string ticket = await GetTiket(appid);
            var timestamp = GetTimestamp();
            string nonceStr = GetNonce();
            string signature = JSSDKHelper.GetSignature(ticket, nonceStr, timestamp, url);
            
            return Tuple.Create(timestamp,nonceStr,signature);
        }
    }
}
