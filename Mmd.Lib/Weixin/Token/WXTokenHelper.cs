using MD.Lib.DB.Redis;
using MD.Lib.Util;
using MD.Configuration;
using MD.Model.Configuration;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.Objects.Messaging;
using MD.Model.Redis.Objects.WeChat;

namespace MD.Lib.Weixin.Token
{
    //public static class WXTokenHelper
    //{
    //    static object SyncObject = new object();
    //    public static string GetSiteAccessTokenFromRedis()
    //    {
    //        WeixinConfig config = MdConfigurationManager.GetConfig<WeixinConfig>();
    //        if (config == null)
    //            throw new Exception("GetAccessToken失败！获取Weixinconfig配置对象失败！");

    //        double currenExpireIn;

    //        int dbNumber =(int) Enum.Parse(typeof(RedisKeyMap), RedisKeyMap.AuthDB.ToString());
    //        var db = RedisManager.GetRedisDB(dbNumber,null);

    //        var atString = db.StringGet(RedisKeyMap.String_Site_AccessToken.ToString());
    //        var atExpireIn = db.StringGet(RedisKeyMap.String_Site_TokenExpireIn.ToString());
    //        if (!atString.IsNull && !atExpireIn.IsNull && atExpireIn.TryParse(out currenExpireIn))
    //        {
    //            if (CommonHelper.GetUnixTimeNow() <= currenExpireIn)
    //                return atString;
    //        }
    //        lock (SyncObject)
    //        {
    //            atString = db.StringGet(RedisKeyMap.String_Site_AccessToken.ToString());
    //            atExpireIn = db.StringGet(RedisKeyMap.String_Site_TokenExpireIn.ToString());
    //            if (!atString.IsNull && !atExpireIn.IsNull && atExpireIn.TryParse(out currenExpireIn))
    //            {
    //                if (CommonHelper.GetUnixTimeNow() <= currenExpireIn)
    //                    return atString;
    //            }

    //            AccessTokenResult result = CommonApi.GetToken(config.WeixinAppId, config.WeixinAppSecret);
    //            if (result == null)
    //                throw new Exception("GetAccessToken Failed!");
    //            double newExpireIn = CommonHelper.GetUnixTimeNow() + result.expires_in;


    //            if (!db.StringSet(RedisKeyMap.String_Site_AccessToken.ToString(), result.access_token))
    //                throw new Exception("GetAcessToken set redis key失败！accessToken=" + result.access_token);
    //            if (!db.StringSet(RedisKeyMap.String_Site_TokenExpireIn.ToString(), newExpireIn))
    //                throw new Exception("GetAcessToken set redis key失败！ExpireIn=" + result.access_token);
    //            return result.access_token;
    //        }
    //    }
    //}

    public static class WXTokenHelper
    {
        static readonly object SyncObject = new object();
        public static string GetSiteAccessTokenFromRedis()
        {
            WeixinConfig config = MdConfigurationManager.GetConfig<WeixinConfig>();
            if (config == null)
                throw new Exception("GetAccessToken失败！获取Weixinconfig配置对象失败！");

            double currenExpireIn;

            var atString = new RedisManager2<WeChatRedisConfig>().StringGet<WXTokenRedis, AccessTokenStringAttribute>();
            var atExpireIn = new RedisManager2<WeChatRedisConfig>().StringGet<WXTokenRedis, AccessTokenExpireInStringAttribute>();
            if (!atString.IsNull && !atExpireIn.IsNull && atExpireIn.TryParse(out currenExpireIn))
            {
                if (CommonHelper.GetUnixTimeNow() <= currenExpireIn)
                    return atString;
            }
            lock (SyncObject)//双重判断
            {
                atString = new RedisManager2<WeChatRedisConfig>().StringGet<WXTokenRedis, AccessTokenStringAttribute>();
                atExpireIn = new RedisManager2<WeChatRedisConfig>().StringGet<WXTokenRedis, AccessTokenExpireInStringAttribute>();
                if (!atString.IsNull && !atExpireIn.IsNull && atExpireIn.TryParse(out currenExpireIn))
                {
                    if (CommonHelper.GetUnixTimeNow() <= currenExpireIn)
                        return atString;
                }

                AccessTokenResult result = CommonApi.GetToken(config.WeixinAppId, config.WeixinAppSecret);
                if (result == null)
                    throw new Exception("GetAccessToken Failed!");
                double newExpireIn = CommonHelper.GetUnixTimeNow() + result.expires_in;


                if (!new RedisManager2<WeChatRedisConfig>().StringSet<WXTokenRedis, AccessTokenStringAttribute>(result.access_token))
                    throw new Exception("GetAcessToken set redis key失败！accessToken=" + result.access_token);

                if (!new RedisManager2<WeChatRedisConfig>().StringSet<WXTokenRedis, AccessTokenExpireInStringAttribute>(newExpireIn))
                    throw new Exception("GetAcessToken set redis key失败！ExpireIn=" + result.access_token);
                return result.access_token;
            }
        }
    }
}
