using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.HttpUtility;

namespace Senparc.Weixin.MP.AdvancedAPIs.MyExtension
{
    public static class MyOAuthApi
    {
        /// <summary>
        /// 刷新access_token（如果需要）
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="refreshToken">填写通过access_token获取到的refresh_token参数</param>
        /// <param name="grantType"></param>
        /// <returns></returns>
        public static async Task<OAuthAccessTokenResult> RefreshTokenAsync(string appId, string refreshToken, string grantType = "refresh_token")
        {
            var url =
                string.Format("https://api.weixin.qq.com/sns/oauth2/refresh_token?appid={0}&grant_type={1}&refresh_token={2}",
                                appId, grantType, refreshToken);

            return await Senparc.Weixin.CommonAPIs.CommonJsonSend.SendAsync<OAuthAccessTokenResult>(null, url, null, CommonJsonSendType.GET);
        }

        public static async Task<OAuthUserInfo> GetUserInfoAsync(string accessToken, string openId, Language lang = Language.zh_CN)
        {
            var url = string.Format("https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}&lang={2}", accessToken, openId, lang);
            return await Senparc.Weixin.CommonAPIs.CommonJsonSend.SendAsync<OAuthUserInfo>(null, url, null, CommonJsonSendType.GET);
        }

        public static async Task<WxJsonResult> AuthAsync(string accessToken, string openId)
        {
            var url = string.Format("https://api.weixin.qq.com/sns/auth?access_token={0}&openid={1}", accessToken, openId);
            return await Senparc.Weixin.CommonAPIs.CommonJsonSend.SendAsync<WxJsonResult>(null, url, null, CommonJsonSendType.GET);
        }

        public static async Task<OAuthAccessTokenResult> GetAccessTokenAsync(string appId, string secret, string code, string grantType = "authorization_code")
        {
            var url =
                string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type={3}",
                                appId, secret, code, grantType);
            return await Senparc.Weixin.CommonAPIs.CommonJsonSend.SendAsync<OAuthAccessTokenResult>(null, url, null, CommonJsonSendType.GET);
            //return CommonJsonSend.Send<OAuthAccessTokenResult>(null, url, null, CommonJsonSendType.GET);
        }
    }
}
