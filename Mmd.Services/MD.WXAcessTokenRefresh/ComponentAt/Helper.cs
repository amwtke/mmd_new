using System;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.DB.Redis;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using Senparc.Weixin.Open.ComponentAPIs;
using Senparc.Weixin.Open.Entities;

namespace MD.WXAcessTokenRefresh.ComponentAt
{
    public static class Helper
    {
        private static readonly object componentAtSyncObject = new object();
        private static readonly object componentAuthorizerAtSyncObject = new object();
        public static string GetComponentAt()
        {
            string at=null;
            try
            {
                WXOpenConfig config = MdConfigurationManager.GetConfig<WXOpenConfig>();
                if (config == null)
                    throw new MDException(typeof (WXComponentHelper), "GetAccessToken失败！获取WXOpenConfig配置对象失败！");
                var atString =
                    new RedisManager2<WeChatRedisConfig>()
                        .StringGet<WXComponentAccessTokeRedis, ComponentAccessTokenAttribute>();
                var atExpireIn =
                    new RedisManager2<WeChatRedisConfig>()
                        .StringGet<WXComponentAccessTokeRedis, ComponentAccessTokenExpireInAttribute>();
                double currenExpireIn;
                if (!atString.IsNull && !atExpireIn.IsNull && atExpireIn.TryParse(out currenExpireIn))
                {
                    //不需要跟微信服务器刷新at
                    if (CommonHelper.GetUnixTimeNow() < currenExpireIn)
                    {
                        at = atString;
                        return at;
                    }
                }
                lock (componentAtSyncObject)
                {
                    //再次判断是否超时
                    if (!atString.IsNull && !atExpireIn.IsNull && atExpireIn.TryParse(out currenExpireIn))
                    {
                        //不需要跟微信服务器刷新at
                        if (CommonHelper.GetUnixTimeNow() < currenExpireIn)
                        {
                            at = atString;
                            return at;
                        }
                    }

                    //获取AT

                    //获取ticket
                    string ticket = WXComponentHelper.GetVerifyTicket();
                    if (string.IsNullOrEmpty(ticket))
                        throw new MDException(typeof (WXComponentHelper), "还没有ticket!请等10分钟再试！");

                    ComponentAccessTokenResult result = ComponentApi.GetComponentAccessToken(config.AppId,
                        config.AppSecret, ticket);
                    if (result == null)
                        throw new MDException(typeof (WXComponentHelper), "调用accesstoken返回空！");
                    double newExpireIn = CommonHelper.GetUnixTimeNow() + result.expires_in;


                    if (
                        !new RedisManager2<WeChatRedisConfig>()
                            .StringSet<WXComponentAccessTokeRedis, ComponentAccessTokenAttribute>(
                                result.component_access_token))
                        throw new MDException(typeof (WXComponentHelper),
                            "GetAcessToken set redis key失败！accessToken=" + result.component_access_token);

                    if (
                        !new RedisManager2<WeChatRedisConfig>()
                            .StringSet<WXComponentAccessTokeRedis, ComponentAccessTokenExpireInAttribute>(newExpireIn))
                        throw new Exception("GetAcessToken set redis key失败！ExpireIn=" + result.expires_in);

                    at = result.component_access_token;
                    return at;
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof (WXComponentHelper), ex);
            }
            return at;
        }

        public static async Task<string> GetAuthorizerAtByAppIdAsync(string appId)
        {
            var simpleObject =
                await
                    new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<AuthorizerInfoRedis>(appId);

            if (simpleObject == null)
                return null;

            if (CommonHelper.GetUnixTimeNow() < double.Parse(simpleObject.ExpireIn))
                return simpleObject.AccessToken;
            return (await WXComponentHelper.RefreshAuthorizerAccessTokenAsnyc(appId, simpleObject.AccessRefreshToken))?.AccessToken;
        }

        public static string GetAuthorizerAtByAppId(string appId)
        {
            var simpleObject = new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<AuthorizerInfoRedis>(appId);

            if (string.IsNullOrEmpty(simpleObject?.ExpireIn))
                return null;

            if (CommonHelper.GetUnixTimeNow() < double.Parse(simpleObject.ExpireIn))
                return simpleObject.AccessToken;
            lock (componentAuthorizerAtSyncObject)
            {
                simpleObject = new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<AuthorizerInfoRedis>(appId);
                if (simpleObject == null)
                    return null;
                return CommonHelper.GetUnixTimeNow() < double.Parse(simpleObject.ExpireIn) ? simpleObject.AccessToken : (WXComponentHelper.RefreshAuthorizerAccessToken(appId, simpleObject.AccessRefreshToken))?.AccessToken;

                //刷新
            }
        }
    }
}
