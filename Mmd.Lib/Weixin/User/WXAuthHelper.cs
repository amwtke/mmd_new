using MD.Lib.DB.Redis;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Model.Configuration;
using MD.Model.Redis.att.CustomAtts.sets;
using MD.Model.Redis.Att.CustomAtts.Sets;
using MD.Model.Redis.Objects;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Redis;
using Senparc.Weixin.MP.AdvancedAPIs.MyExtension;

namespace MD.Lib.Weixin.User
{
    public static class WXAuthHelper
    {
        static object lockObject = new object();
        /// <summary>
        /// 获取Auth相关db在redis中的Db号。
        /// </summary>
        /// <returns></returns>

        /// <summary>
        /// 获取第三方认证的登陆链接.约束：必须在wechat.51science.cn这个域名底下的url才行。
        /// 默认是http://wechat.51science.cn/wxcallback。
        /// </summary>
        /// <returns></returns>
        public static string GetAuthUrl(string redirect_url = "http://wechat.51science.cn/wxcallback")
        {
            var config = MD.Configuration.MdConfigurationManager.GetConfig<WeixinConfig>();
            return OAuthApi.GetAuthorizeUrl(config.WeixinAppId, redirect_url, config.WeixinToken, OAuthScope.snsapi_userinfo);
        }

        /// <summary>
        /// 当页面授权回调时获取openid时使用。
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="secretCode"></param>
        /// <param name="callBackCode"></param>
        /// <param name="saveUserInfo"></param>
        /// <returns></returns>
        public static async Task<string> GetOpenIdWXCallBackAsync(string appid, string secretCode, string callBackCode, Func<OAuthAccessTokenResult, Task<bool>> isNeedGetUserInfoFunc,Func<OAuthUserInfo,Task<bool>> saveUserinfo)
        {
            if (isNeedGetUserInfoFunc == null || saveUserinfo == null)
                throw new Exception("GetOpenIdWXCallBackAsync->judgeFunc与SaveUseinfo必须补全！");
            try
            {
                OAuthAccessTokenResult userAt = await MyOAuthApi.GetAccessTokenAsync(appid, secretCode, callBackCode);

                if (!string.IsNullOrEmpty(userAt.openid))
                {
                    if (await isNeedGetUserInfoFunc(userAt))
                    {
                        OAuthAccessTokenResult token = await MyOAuthApi.RefreshTokenAsync(appid, userAt.refresh_token);
                        var userinfo = await MyOAuthApi.GetUserInfoAsync(token.access_token, userAt.openid);
                        if (await saveUserinfo(userinfo))
                            return userAt.openid;
                        else
                            throw new Exception("Userinfo保存失败！");
                    }
                }
                return userAt.openid;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(WXAuthHelper), ex);
                throw ex;
            }
        }

        #region user tester related
        public static async Task<UserInfoRedis> GetUserInfoByOPenId(string openid)
        {
            return await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(openid);
        }

        public static UserInfoRedis GetUserInfoByOPenId_TongBu(string openid)
        {
            return new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<UserInfoRedis>(openid);
        }

        //public static string GetUserinfoOpenIdSetName()
        //{
        //    return RedisManager.GetKeyName<UserInfoRedis, UserInfoSetOpenIdSetAttribute>();
        //}

        public static IDatabase GetUserinfoDB()
        {
            return new RedisManager2<WeChatRedisConfig>().GetDb<UserInfoRedis>();
        }
        /// <summary>
        /// 移除测试用户
        /// </summary>
        /// <param name="openid"></param>
        /// <returns></returns>
        public static async Task<bool> RemoveTester(string openid)
        {
            string setname = new RedisManager2<WeChatRedisConfig>().GetKeyName<TesterRedis, TesterOpenIdSetAttribute>();
            var db = GetUserinfoDB();
            return await db.SetRemoveAsync(setname, openid);
        }

        /// <summary>
        /// 添加测试用户
        /// </summary>
        /// <param name="openid"></param>
        /// <returns></returns>
        public static async Task<bool> AddTester(string openid)
        {
            string setname = new RedisManager2<WeChatRedisConfig>().GetKeyName<TesterRedis, TesterOpenIdSetAttribute>();//"tester." + GetUserinfoSetName();
            var db = GetUserinfoDB();
            return await db.SetAddAsync(setname, openid);
        }
        /// <summary>
        /// 是否为测试用户
        /// </summary>
        /// <param name="openid"></param>
        /// <returns></returns>
        public static async Task<bool> IsTester(string openid)
        {
            var setname = new RedisManager2<WeChatRedisConfig>().GetKeyName<TesterRedis, TesterOpenIdSetAttribute>();//"tester." + GetUserinfoSetName();
            var db = GetUserinfoDB();
            return await db.SetContainsAsync(setname, openid);
        }

       /// <summary>
       /// 获取所有测试用户
       /// </summary>
       /// <returns></returns>
        public static async Task<List<string>> GetAllTesters()
        {
            var setkey = new RedisManager2<WeChatRedisConfig>().GetKeyName<TesterRedis, TesterOpenIdSetAttribute>();//"tester." + GetUserinfoSetName();
            var db = GetUserinfoDB();
            return new RedisManager2<WeChatRedisConfig>().ConvertRedisValueToString(await db.SetMembersAsync(setkey));
        }
        #endregion
    }
}
