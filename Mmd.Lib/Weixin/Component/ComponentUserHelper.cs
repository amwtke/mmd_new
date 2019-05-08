using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Redis;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using Nest;
using Senparc.Weixin;
using Senparc.Weixin.Open;
using Senparc.Weixin.Open.OAuthAPIs;
using OAuthScope = Senparc.Weixin.Open.OAuthScope;

namespace MD.Lib.Weixin.Component
{
    /// <summary>
    /// 第三方平台的用户授权API
    /// </summary>
    public static class WXComponentUserHelper
    {
        static object AccessTokenSyncObject = new object();

        /// <summary>
        /// 默认的回调域名为：{appid}.wx.mmpintuan.com
        /// </summary>
        /// <param name="gzh_appid">公众号的appid</param>
        /// <param name="object_id">业务对象的id</param>
        /// <param name="bizid">业务id</param>
        /// <param name="state">微信的回调state</param>
        /// <param name="controller">要跳转到得controller</param>
        /// <param name="action">要跳转到得action</param>
        /// <returns></returns>
        public static string GenComponentCallBackUrl(string gzh_appid, string bizid, string state = "",
            string controller = "group", string action = "kt")
        {
            string redirectUrl = @"http://" + gzh_appid + @".wx.mmpintuan.com/" + controller + @"/" + action + @"?bizid=" +
                                 bizid;
            OAuthScope[] array = new OAuthScope[1];
            array[0] = OAuthScope.snsapi_userinfo;
            //array[1] = OAuthScope.snsapi_base;
            string ret = OAuthApi.GetAuthorizeUrl(gzh_appid, WXComponentHelper.GetConfigObject().AppId, redirectUrl, state, array);
            return ret;
        }
        public static string GenComponentCallBackUrl_fx(string gzh_appid, string bizid, string shareopenid, string state,
           string controller, string action)
        {
            string redirectUrl = @"http://" + gzh_appid + @".wx.mmpintuan.com/" + controller + @"/" + action + @"?bizid=" + bizid + @"&shareopenid=" + shareopenid;
            OAuthScope[] array = new OAuthScope[1];
            array[0] = OAuthScope.snsapi_userinfo;
            //array[1] = OAuthScope.snsapi_base;
            string ret = OAuthApi.GetAuthorizeUrl(gzh_appid, WXComponentHelper.GetConfigObject().AppId, redirectUrl, state, array);
            return ret;
        }
        public static string GenCallBackUrl(string bizid, string controller, string action)
        {
            string redirectUrl = @"http://mmpintuan.com/" + controller + @"/" + action + @"?bizid=" + bizid;
            return redirectUrl;
        }
        /// <summary>
        /// 如果没有初始化过则返回空。从code回调流程初始化at再使用。这个userAT实际上是用来获取页面授权的AT，跟第三方公众号调用的
        /// AuthorizerAT不同。这个可以每次获取授权时再取，而且没有次数限制。
        /// </summary>
        /// <param name="gzhAppid"></param>
        /// <returns></returns>
        public static string GetUserAccessToken(string gzhAppid)
        {
            AuthorizerInfoRedis obj =
                new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<AuthorizerInfoRedis>(gzhAppid);
            //如果没有初始化过则返回空
            if (string.IsNullOrEmpty(obj?.UserAccessToken))
                return string.Empty;
            double expireIn;
            if (double.TryParse(obj.UserAccessTokenExpireIn, out expireIn))
            {
                if (expireIn - CommonHelper.GetUnixTimeNow() >= 0)
                    return obj.UserAccessToken;
                else
                {
                    //需要刷新UserAT
                    lock (AccessTokenSyncObject)
                    {
                        //再次判断是否需要刷新
                        var obj2 = new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<AuthorizerInfoRedis>(gzhAppid);
                        expireIn = double.Parse(obj2.UserAccessTokenExpireIn);
                        if (expireIn - CommonHelper.GetUnixTimeNow() >= 0)
                            return obj2.UserAccessToken;
                        //需要刷新
                        string ret = RefreshUserAccessToken(gzhAppid, obj2.UserAccessRefreshToken);
                        return ret;
                    }
                }
            }
            return string.Empty;
        }

        public static string RefreshUserAccessToken(string gzhAppid, string refreshToken)
        {
            OAuthAccessTokenResult fromNet = OAuthApi.RefreshToken(gzhAppid, refreshToken, WXComponentHelper.GetConfigObject().AppId,
                WXComponentHelper.GetComponentAccessToken());
            if (fromNet == null)
                throw new MDException(typeof(WXComponentUserHelper), "RefreshUserAccessToken微信返回错误！");
            AuthorizerInfoRedis obj = new AuthorizerInfoRedis();
            obj.Appid = gzhAppid;
            obj.UserAccessToken = fromNet.access_token;
            obj.UserAccessRefreshToken = fromNet.refresh_token;
            obj.UserAccessTokenExpireIn = (CommonHelper.GetUnixTimeNow() + fromNet.expires_in).ToString();
            new RedisManager2<WeChatRedisConfig>().SaveObject(obj);
            return obj.UserAccessToken;
        }

        public static async Task<string> RefreshUserAccessTokenAsync(string gzhAppid, string refreshToken)
        {
            OAuthAccessTokenResult fromNet = await OAuthApi.RefreshTokenAsync(gzhAppid, refreshToken, WXComponentHelper.GetConfigObject().AppId,
                 await WXComponentHelper.GetComponentAccessTokenAsync());
            if (fromNet == null)
                throw new MDException(typeof(WXComponentUserHelper), "RefreshUserAccessToken微信返回错误！");
            AuthorizerInfoRedis obj = new AuthorizerInfoRedis();
            obj.Appid = gzhAppid;
            obj.UserAccessToken = fromNet.access_token;
            obj.UserAccessRefreshToken = fromNet.refresh_token;
            obj.UserAccessTokenExpireIn = (CommonHelper.GetUnixTimeNow() + fromNet.expires_in).ToString();
            await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(obj);
            return obj.UserAccessToken;
        }

        /// <summary>
        /// 用户授权的回调处理函数。网页授权at跟调用api的at不是一个at。这个接口调用没有次数限制。
        /// 7天会重置一下用户信息
        /// </summary>
        /// <param name="gzhAppid"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async Task<UserInfoRedis> GetUserInfoAsync(string gzhAppid, string code)
        {
            OAuthAccessTokenResult fromNet = await OAuthApi.GetAccessTokenAsync(gzhAppid, WXComponentHelper.GetConfigObject().AppId,
                 await WXComponentHelper.GetComponentAccessTokenAsync(), code);
            if (fromNet != null && fromNet.errcode == ReturnCode.请求成功)
            {
                //log
                //MDLogger.LogInfoAsync(typeof(WXComponentUserHelper),$"appid的at是：{fromNet.access_token},expirin:{fromNet.expires_in}");

                //刷新托管公众号相关信息
                AuthorizerInfoRedis redisObject = new AuthorizerInfoRedis()
                {
                    Appid = gzhAppid,
                    UserAccessToken = fromNet.access_token,
                    UserAccessRefreshToken = fromNet.refresh_token,
                    UserAccessTokenExpireIn = (CommonHelper.GetUnixTimeNow() + fromNet.expires_in).ToString()
                };
                //更新gzh的相关授权信息。
                if (!await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(redisObject))
                {
                    throw new MDException(typeof(WXComponentUserHelper), $"更新appid:{gzhAppid}相关信息失败！");
                }
                //更新userinfo
                string openid = fromNet.openid;
                if (string.IsNullOrEmpty(openid)) return null;
                var userinfo =
                    await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(openid);
                if (userinfo.Openid.Equals(openid) && !string.IsNullOrEmpty(userinfo.ExpireIn))
                {
                    double expireIn = double.Parse(userinfo.ExpireIn);
                    if (expireIn - CommonHelper.GetUnixTimeNow() >= 0)
                        return userinfo;
                }
                //从微信拉去用户信息.用户信息7天内不更新！
                var userinfoFromNet = await OAuthApi.GetUserInfoAsync(fromNet.access_token, openid);
                if (userinfoFromNet == null)
                    throw new MDException(typeof(WXComponentUserHelper), "微信获取用户信息失败！");

                UserInfoRedis u = new UserInfoRedis();
                u.Openid = userinfoFromNet.openid;
                u.City = userinfoFromNet.city;
                u.Country = userinfoFromNet.country;
                u.HeadImgUrl = userinfoFromNet.headimgurl;
                u.NickName = userinfoFromNet.nickname;
                u.Sex = userinfoFromNet.sex.ToString();
                foreach (var s in userinfoFromNet.privilege)
                {
                    u.Privilege += s + ";";
                }
                u.Unionid = userinfoFromNet.unionid;
                u.Province = userinfoFromNet.province;
                u.ExpireIn = (CommonHelper.GetUnixTimeNow() + TimeSpan.FromDays(7).TotalSeconds).ToString();
                //保存
                await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(u);
                return u;
            }
            throw new MDException(typeof(WXComponentUserHelper), "从微信获取UserAccessToken失败！");
        }
    }
}
