using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.DB.Redis;
using MD.Lib.Log;
using MD.Lib.MQ.RPC;
using MD.Lib.MQ.RPC.ArgsAndRets;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration;
using MD.Model.Configuration.MQ.RPC;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.Objects.WeChat;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using Nest;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.Open;
using Senparc.Weixin.Open.ComponentAPIs;
using Senparc.Weixin.Open.Entities;

namespace MD.Lib.Weixin.Component
{
    /// <summary>
    /// 第三方平台的授权api
    /// </summary>
    public static class WXComponentHelper
    {
        static readonly object LockObject = new object();
        static readonly object ticket_sync_object = new object();
        private static MQRpcClient<RpcGetComponentAtConfig> _ComponentAtClient = new MQRpcClient<RpcGetComponentAtConfig>();
        private static MQRpcClient<RpcGetAuthorizerAtConfig> _AuthorizerAtClient = new MQRpcClient<RpcGetAuthorizerAtConfig>();

        static WXComponentHelper()
        {
            if (_ComponentAtClient.IsStarted && _AuthorizerAtClient.IsStarted) return;
            lock (LockObject)
            {
                if (_ComponentAtClient.IsStarted && _AuthorizerAtClient.IsStarted) return;

                _ComponentAtClient.Start();
                _AuthorizerAtClient.Start();
            }
        }
        public static void Close()
        {
            _ComponentAtClient.Close();
            _AuthorizerAtClient.Close();
        }
        #region ticket

        public static async Task<string> GetVerifyTicketAync()
        {
            return
                await
                    new RedisManager2<WeChatRedisConfig>()
                        .StringGetAsync<WXComponentTicketRedis, ComponentTicketAttribute>();
        }

        public static string GetVerifyTicket()
        {
            return
                new RedisManager2<WeChatRedisConfig>()
                    .StringGet<WXComponentTicketRedis, ComponentTicketAttribute>();
        }

        public static bool SaveVerifyTicket(RequestMessageComponentVerifyTicket ticketRequest)
        {
            if (string.IsNullOrEmpty(ticketRequest?.ComponentVerifyTicket))
                return false;
            lock (ticket_sync_object)
            {
                return
                    new RedisManager2<WeChatRedisConfig>()
                        .StringSet<WXComponentTicketRedis, ComponentTicketAttribute>(
                            ticketRequest.ComponentVerifyTicket);
            }
        }

        #endregion

        #region accesstoken

        public static string GetComponentAccessToken()
        {
            try
            {
                //MDLogger.LogInfoAsync(typeof(WXComponentHelper), temp.ToString());
                string atString = null;
                var ret = _ComponentAtClient.Call(GetComponentAtFunc.GenArgs());
                if (ret != null)
                {
                    atString = GetComponentAtFunc.GetAtFromRpcResult(ret);
                }
                return atString;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(WXComponentHelper), ex);
                throw;
            }
        }

        /// <summary>
        /// 异步获取at。
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetComponentAccessTokenAsync()
        {
            string at = await AsyncHelper.RunAsync(GetComponentAccessToken);
            return at;
        }

        #endregion

        #region precode

        //[Obsolete("因为每个authcode对应一个公众号，所以每次对不同公众号授权时都要使用新的precode。因此此方法不适用了。")]
        //public static string GetComponentPreCode()
        //{
        //    WXOpenConfig config = MdConfigurationManager.GetConfig<WXOpenConfig>();
        //    if (config == null)
        //        throw new MDException(typeof(WXComponentHelper), "GetComponentPreCode！获取WXOpenConfig配置对象失败！");

        //    double currenExpireIn;

        //    var atString = new RedisManager2<WeChatRedisConfig>().StringGet<WXComponentPreAutCodeRedis, ComponentPreauthcodeAttribute>();

        //    var atExpireIn = new RedisManager2<WeChatRedisConfig>().StringGet<WXComponentPreAutCodeRedis, ComponentPreauthcodeExpireInAttribute>();
        //    if (!atString.IsNull && !atExpireIn.IsNull && atExpireIn.TryParse(out currenExpireIn))
        //    {
        //        if (CommonHelper.GetUnixTimeNow() <= currenExpireIn)
        //            return atString;
        //    }
        //    lock (accesstoken_sync_object)//双重判断
        //    {
        //        atString = new RedisManager2<WeChatRedisConfig>().StringGet<WXComponentPreAutCodeRedis, ComponentPreauthcodeAttribute>();
        //        atExpireIn = new RedisManager2<WeChatRedisConfig>().StringGet<WXComponentPreAutCodeRedis, ComponentPreauthcodeExpireInAttribute>();
        //        if (!atString.IsNull && !atExpireIn.IsNull && atExpireIn.TryParse(out currenExpireIn))
        //        {
        //            if (CommonHelper.GetUnixTimeNow() <= currenExpireIn)
        //                return atString;
        //        }

        //        //获取accessToken
        //        string accessToken = GetComponentAccessToken();
        //        if (string.IsNullOrEmpty(accessToken))
        //            throw new MDException(typeof(WXComponentHelper), "accessToken异常，没有存redis！");

        //        PreAuthCodeResult result =
        //            Senparc.Weixin.Open.ComponentAPIs.ComponentApi.GetPreAuthCode(config.AppId,accessToken);

        //        if (result == null)
        //            throw new MDException(typeof(WXComponentHelper), "调用GetPreAuthCode返回空！");
        //        double newExpireIn = CommonHelper.GetUnixTimeNow() + result.expires_in;


        //        if (!new RedisManager2<WeChatRedisConfig>().StringSet<WXComponentPreAutCodeRedis, ComponentPreauthcodeAttribute>(result.pre_auth_code))
        //            throw new MDException(typeof(WXComponentHelper), "GetComponentPreCode set redis key失败！pre_auth_code=" + result.pre_auth_code);

        //        if (!new RedisManager2<WeChatRedisConfig>().StringSet<WXComponentPreAutCodeRedis, ComponentPreauthcodeExpireInAttribute>(newExpireIn))
        //            throw new Exception("GetComponentPreCode set redis key失败！ExpireIn=" + result.expires_in);
        //        return result.pre_auth_code;
        //    }
        //}
        /// <summary>
        /// 强制刷新precode。每次授权的时候使用这种方式！
        /// </summary>
        /// <returns></returns>
        public static string GetComponentPreCodeForce()
        {
            //获取accessToken
            string accessToken = GetComponentAccessToken();
            if (string.IsNullOrEmpty(accessToken))
                throw new MDException(typeof(WXComponentHelper), "ComponentAccessToken异常，没有存redis！");

            PreAuthCodeResult result =
                Senparc.Weixin.Open.ComponentAPIs.ComponentApi.GetPreAuthCode(GetConfigObject().AppId, accessToken);

            if (result == null)
                throw new MDException(typeof(WXComponentHelper), "调用GetPreAuthCode返回空！");
            double newExpireIn = CommonHelper.GetUnixTimeNow() + result.expires_in;


            if (!new RedisManager2<WeChatRedisConfig>().StringSet<WXComponentPreAutCodeRedis, ComponentPreauthcodeAttribute>(result.pre_auth_code))
                throw new MDException(typeof(WXComponentHelper), "GetComponentPreCode set redis key失败！pre_auth_code=" + result.pre_auth_code);

            if (!new RedisManager2<WeChatRedisConfig>().StringSet<WXComponentPreAutCodeRedis, ComponentPreauthcodeExpireInAttribute>(newExpireIn))
                throw new Exception("GetComponentPreCode set redis key失败！ExpireIn=" + result.expires_in);
            return result.pre_auth_code;
        }

        public static async Task<string> GetComponentPreCodeForceAsync()
        {
            //获取accessToken
            string accessToken = await GetComponentAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                throw new MDException(typeof(WXComponentHelper), "ComponentAccessToken异常，没有存redis！");

            PreAuthCodeResult result = await
                Senparc.Weixin.Open.ComponentAPIs.ComponentApi.GetPreAuthCodeAsync(GetConfigObject().AppId, accessToken);

            if (result == null)
                throw new MDException(typeof(WXComponentHelper), "调用GetPreAuthCode返回空！");
            double newExpireIn = CommonHelper.GetUnixTimeNow() + result.expires_in;


            if (!await new RedisManager2<WeChatRedisConfig>().StringSetAsync<WXComponentPreAutCodeRedis, ComponentPreauthcodeAttribute>(result.pre_auth_code))
                throw new MDException(typeof(WXComponentHelper), "GetComponentPreCode set redis key失败！pre_auth_code=" + result.pre_auth_code);

            if (!await new RedisManager2<WeChatRedisConfig>().StringSetAsync<WXComponentPreAutCodeRedis, ComponentPreauthcodeExpireInAttribute>(newExpireIn))
                throw new Exception("GetComponentPreCode set redis key失败！ExpireIn=" + result.expires_in);
            return result.pre_auth_code;
        }

        #endregion

        #region author code

        public static bool SaveAuthCode(string authcode, int expireIn)
        {
            return
                new RedisManager2<WeChatRedisConfig>().StringSet<WXComponentAuthorCodeRedis, ComponentAuthCodeAttribute>
                    (authcode) &&
                new RedisManager2<WeChatRedisConfig>()
                    .StringSet<WXComponentAuthorCodeRedis, ComponentAuthCodeExpireInAttribute>(CommonHelper.GetUnixTimeNow() + expireIn);
        }

        public static string GetAuthCode()
        {
            return new RedisManager2<WeChatRedisConfig>().StringGet<WXComponentAuthorCodeRedis, ComponentAuthCodeAttribute>();
        }

        public static async Task<string> GetAuthCodeAsync()
        {
            return await new RedisManager2<WeChatRedisConfig>().StringGetAsync<WXComponentAuthorCodeRedis, ComponentAuthCodeAttribute>();
        }
        #endregion

        public static WXOpenConfig GetConfigObject()
        {
            WXOpenConfig config = MdConfigurationManager.GetConfig<WXOpenConfig>();
            if (config == null)
                throw new MDException(typeof(WXComponentHelper), "GetComponentPreCode！获取WXOpenConfig配置对象失败！");
            return config;
        }

        /// <summary>
        /// 获取微信授权的url。回调函数最好带上http://
        /// </summary>
        /// <param name="redirectUrl"></param>
        /// <returns></returns>
        public static string GetAuthUrl(string redirectUrl = "http://wx.mmpintuan.com/callback")
        {
            var config = GetConfigObject();
            string preCode = GetComponentPreCodeForce();
            return Senparc.Weixin.Open.ComponentAPIs.ComponentApi.GetComponentLoginPageUrl(config.AppId, preCode, redirectUrl);
        }

        /// <summary>
        /// 获取第三方公众号登录后台与授权的url
        /// </summary>
        /// <param name="redirectUrl"></param>
        /// <returns></returns>
        public static async Task<string> GetAuthUrlAsync(string redirectUrl = "http://mmpintuan.com/Redirect")
        {
            var config = GetConfigObject();
            string preCode = await GetComponentPreCodeForceAsync();
            return Senparc.Weixin.Open.ComponentAPIs.ComponentApi.GetComponentLoginPageUrl(config.AppId, preCode, redirectUrl);
        }

        /// <summary>
        /// 单纯的第4步，可以用于公众号扫码登录后台。剔除了刷新流程。
        /// 回调进来后要知道是哪个公众号主扫了我的码。
        /// </summary>
        /// <param name="auth_code"></param>
        /// <returns></returns>
        public static async Task<string> WxCallbackSimpleAsync(string auth_code)
        {
            //MDLogger.LogInfoAsync(typeof(WXComponentHelper),$"收到一个回调！auth_code={auth_code}");

            string component_accessToken = await GetComponentAccessTokenAsync();
            string component_appid = GetConfigObject().AppId;
            //MDLogger.LogInfoAsync(typeof(WXComponentHelper),$"component_at:{component_accessToken};component_appid:{component_appid}");


            var queryAuthResult = await ComponentApi.QueryAuthAsync(component_accessToken, component_appid, auth_code);
            if (queryAuthResult == null)
            {
                throw new MDException(typeof(WXComponentHelper), "获取simpleAuthoinfo失败！");

            }

            // MDLogger.LogInfoAsync(typeof(WXComponentHelper), $"authorizer_appid={queryAuthResult.authorization_info.authorizer_appid};authorizer_access_token={queryAuthResult.authorization_info.authorizer_access_token};refresh={queryAuthResult.authorization_info.authorizer_refresh_token}");


            return queryAuthResult.authorization_info.authorizer_appid;
        }

        /// <summary>
        /// 4与5与第六步一起可以形成通用的开通流程
        /// 通过微信的回调auth_code获取第三方公众号的accesstoken,appid,refreshtoken,授权列表等信息。
        /// 只会返回一部分authorizer的信息：authorizer_appid、authorizer_access_token、authorizer_refresh_token与expires_in。
        /// </summary>
        /// <param name="auth_code"></param>
        /// <returns></returns>
        public static async Task<AuthorizerInfoRedis> WxCallbackAsync(string auth_code)
        {
            //WatchStopper ws = new WatchStopper(typeof(CommonHelper), "WxCallbackAsync-1");
            //ws.Start();
            //MDLogger.LogInfoAsync(typeof(WXComponentHelper),$"收到一个回调！auth_code={auth_code}");

            string component_accessToken = await GetComponentAccessTokenAsync();
            string component_appid = GetConfigObject().AppId;
            //MDLogger.LogInfoAsync(typeof(WXComponentHelper),$"component_at:{component_accessToken};component_appid:{component_appid}");
            //ws.Stop();

            //ws.Restart("WxCallbackAsync-2");
            var queryAuthResult = await ComponentApi.QueryAuthAsync(component_accessToken, component_appid, auth_code);
            if (queryAuthResult == null)
            {
                throw new MDException(typeof(WXComponentHelper), "获取simpleAuthoinfo失败！");

            }

            // MDLogger.LogInfoAsync(typeof(WXComponentHelper), $"authorizer_appid={queryAuthResult.authorization_info.authorizer_appid};authorizer_access_token={queryAuthResult.authorization_info.authorizer_access_token};refresh={queryAuthResult.authorization_info.authorizer_refresh_token}");


            AuthorizerInfoRedis ret = new AuthorizerInfoRedis()
            {
                Appid = queryAuthResult.authorization_info.authorizer_appid,
                AccessRefreshToken = queryAuthResult.authorization_info.authorizer_refresh_token,
            };
            //ws.Stop();
            //刷新authorizer_accesstoken
            //MDLogger.LogInfoAsync(typeof(WXComponentHelper),$"刷新前at:{queryAuthResult.authorization_info.authorizer_access_token}");
            //ws.Restart("WxCallbackAsync-3");
            ret = await RefreshAuthorizerAccessTokenAsnyc(ret.Appid, ret.AccessRefreshToken);
            //MDLogger.LogInfoAsync(typeof(WXComponentHelper), $"刷新后at:{ret.AccessToken}");
            if (await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(ret))
            {
                //ws.Stop();
                return await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<AuthorizerInfoRedis>(ret.Appid);
            }
            //ws.Stop();
            return null;
        }

        /// <summary>
        /// 5
        /// 刷新第三方公众平台的accesstoke。
        /// </summary>
        /// <param name="componentAccessToken"></param>
        /// <param name="componentAppId"></param>
        /// <param name="authorizerAppId"></param>
        /// <param name="authorizerRefreshToken"></param>
        /// <returns></returns>
        public static async Task<AuthorizerInfoRedis> RefreshAuthorizerAccessTokenAsnyc(string authorizerAppId, string authorizerRefreshToken)
        {
            var result = await ComponentApi.ApiAuthorizerTokenAsync(await GetComponentAccessTokenAsync(), GetConfigObject().AppId,
                authorizerAppId, authorizerRefreshToken);
            if (result == null)
                return null;
            AuthorizerInfoRedis ret = new AuthorizerInfoRedis()
            {
                Appid = authorizerAppId,
                AccessToken = result.authorizer_access_token,
                AccessRefreshToken = result.authorizer_refresh_token,
                ExpireIn = (CommonHelper.GetUnixTimeNow() + result.expires_in).ToString()
            };
            if (await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(ret))
                return await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<AuthorizerInfoRedis>(ret.Appid);
            return null;
        }

        /// <summary>
        /// 5
        /// 刷新第三方公众平台的accesstoke。
        /// </summary>
        /// <param name="componentAccessToken"></param>
        /// <param name="componentAppId"></param>
        /// <param name="authorizerAppId"></param>
        /// <param name="authorizerRefreshToken"></param>
        /// <returns></returns>
        public static AuthorizerInfoRedis RefreshAuthorizerAccessToken(string authorizerAppId, string authorizerRefreshToken)
        {
            var result = ComponentApi.ApiAuthorizerToken(GetComponentAccessToken(), GetConfigObject().AppId,
                authorizerAppId, authorizerRefreshToken);
            if (result == null)
                return null;
            AuthorizerInfoRedis ret = new AuthorizerInfoRedis()
            {
                Appid = authorizerAppId,
                AccessToken = result.authorizer_access_token,
                AccessRefreshToken = result.authorizer_refresh_token,
                ExpireIn = (CommonHelper.GetUnixTimeNow() + result.expires_in).ToString()
            };
            if (new RedisManager2<WeChatRedisConfig>().SaveObject(ret))
                return new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<AuthorizerInfoRedis>(ret.Appid);
            return null;
        }

        /// <summary>
        /// 根据第三方appid，查询第三方accesstoken。前提是已经授权过了，而且redis里面存了。过期自动refresh。
        /// </summary>
        /// <param name="authorizerAppid"></param>
        /// <returns></returns>
        public static async Task<string> GetAuthorizerAccessTokenByAuthorizerAppIdAsync(string authorizerAppid)
        {
            return await AsyncHelper.RunAsync(GetAuthorizerAccessTokenByAuthorizerAppId, authorizerAppid);
        }

        public static string GetAuthorizerAccessTokenByAuthorizerAppId(string authorizerAppid)
        {
            try
            {
                string atString = null;
                var ret = _AuthorizerAtClient.Call(GetAuthorizerAtFunc.GenArgs(authorizerAppid));
                if (ret != null)
                {
                    atString = GetAuthorizerAtFunc.GetAtFromRpcResult(ret);
                }
                return atString;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(WXComponentHelper), ex);
                throw;
            }
        }

        /// <summary>
        /// 6——对于已经完成授权的公众号，可以直接调用此方法来获取公众号相关的信息。
        /// 根据component accesstoken与第三方公众号的appid获取第三方公众号的授权信息。
        /// 授权方昵称，授权方头像，授权方公众号类型，0代表订阅号，1代表由历史老帐号升级后的订阅号，2代表服务号，授权方公众号的原始ID，授权方公众号所设置的微信号
        /// 功能的开通状况。二维码图片的URL，开发者最好自行也进行保存，授权方appid。
        /// </summary>
        /// <param name="componentAccessToken"></param>
        /// <param name="componentAppId"></param>
        /// <param name="authorizerAppId"></param>
        /// <returns></returns>
        public static async Task<AuthorizerInfoRedis> GetAuthorizerInfoAsync(string authorizerAppId)
        {
            GetAuthorizerInfoResult fromNet = await ComponentApi.GetAuthorizerInfoAsync(await GetComponentAccessTokenAsync(), GetConfigObject().AppId, authorizerAppId);

            if (fromNet == null) return null;
            AuthorizerInfoRedis obj = new AuthorizerInfoRedis();
            obj.Appid = fromNet.authorization_info.authorizer_appid;
            obj.AccessToken = fromNet.authorization_info.authorizer_access_token;
            obj.AccessRefreshToken = fromNet.authorization_info.authorizer_refresh_token;
            //obj.ExpireIn = fromNet.authorization_info.expires_in.ToString();
            //MDLogger.LogInfoAsync(typeof(WXComponentHelper),$"expire_in{fromNet.authorization_info.expires_in}");
            foreach (var f in fromNet.authorization_info.func_info)
            {
                obj.FuncInfo += ((int)f.funcscope_category.id).ToString() + ",";
            }


            obj.Alias = fromNet.authorizer_info.alias;
            obj.BusinessInfo = fromNet.authorizer_info.business_info.open_pay.ToString() + "," +
                               fromNet.authorizer_info.business_info.open_shake.ToString() + "," +
                               fromNet.authorizer_info.business_info.open_card.ToString() + "," +
                               fromNet.authorizer_info.business_info.open_store.ToString();

            obj.HeadImgUrl = fromNet.authorizer_info.head_img;
            obj.NiceName = fromNet.authorizer_info.nick_name;
            obj.QrCodeUrl = fromNet.authorizer_info.qrcode_url;
            obj.ServiceType = fromNet.authorizer_info.service_type_info.id.ToString();
            obj.UserName = fromNet.authorizer_info.user_name;
            obj.VerifyType = fromNet.authorizer_info.verify_type_info.id.ToString();

            if (await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(obj))
                return await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<AuthorizerInfoRedis>(fromNet.authorization_info.authorizer_appid);
            return null;
        }

        #region 目录相关

        public static string GetPayDir(string appid)
        {
            string patten = GetConfigObject().PayDirPatten;
            string ret = patten.Replace("{appid}", appid);
            return ret;
        }

        public static string GetBizDir(string appid)
        {
            string patten = GetConfigObject().BizDomainUrlPatten;
            string ret = patten.Replace("{appid}", appid);
            return ret;
        }

        public static string GetJsDir(string appid)
        {
            string patten = GetConfigObject().JsSaftyDomainPatten;
            string ret = patten.Replace("{appid}", appid);
            return ret;
        }
        #endregion
    }
}
