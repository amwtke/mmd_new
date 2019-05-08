using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using MD.Model.DB.Professional;
using MD.Lib.Util;
using MD.Lib.DB.Redis;
using MD.Model.Redis.RedisObjects.Statistics;
using MD.Model.Configuration.Redis;

namespace Mmd.Statistics.Filters
{
    public class AccessFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            string errorMessage = "token is null";
            var token = actionContext.Request.Headers.Authorization;

            //判断是否为登录
            string path = actionContext.Request.RequestUri.AbsolutePath;
            if (path == "/api/user/login")
            {
                base.OnActionExecuting(actionContext);
                return;
            }

            if (token != null)
            {
                //验证token是否过期
                try
                {
                    string jsonPayload = JWT.JsonWebToken.Decode(token.ToString(), CommonHelper.JWTsecretKey);
                    sta_user sta_user = JsonHelper.DeserializeJsonToObject<sta_user>(jsonPayload);
                    if (sta_user != null)
                    {
                        string uid = sta_user.uid.ToString();
                        //从redis中获取该用户最后一次的操作时间戳
                        var redisUser = FilterHelp.getRedis(uid);
                        if (redisUser.expTime > CommonHelper.ToUnixTime(DateTime.Now))
                        {
                            //重新更新redis过期时间
                            FilterHelp.SaveRedis(uid);
                            base.OnActionExecuting(actionContext);
                            return;
                        }
                        else
                        {
                            errorMessage = "expired";
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = "expired";
                }
            }
            var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(errorMessage, Encoding.UTF8)
            };
            actionContext.Response = response;
        }
    }
    public static class FilterHelp
    {
        public static StaLoginCheckRedis SaveRedis(string uid)
        {
            StaLoginCheckRedis redisObj = new StaLoginCheckRedis()
            {
                userid = uid,
                expTime = CommonHelper.ToUnixTime(DateTime.Now.AddHours(1))
            };
            bool b = new RedisManager2<WeChatRedisConfig>().SaveObject(redisObj);

            return redisObj;
        }
        public static StaLoginCheckRedis getRedis(string uid)
        {
            var obj = new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<StaLoginCheckRedis>(uid);
            return obj;
        }
    }
}