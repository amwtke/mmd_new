using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using Mmd.Statistics.Controllers.Parameters.Biz;
using Mmd.Statistics.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.IdentityModel.Tokens.Jwt;
using JWT;
using System.Security.Cryptography;
using MD.Model.DB.Professional;
using MD.Lib.ElasticSearch.MD;

namespace Mmd.Statistics.Controllers
{
    [RoutePrefix("api/user")]
    [AccessFilter]
    public class UserController : ApiController
    {
        [HttpPost]
        [Route("login")]
        public async Task<HttpResponseMessage> login(UserParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.loginname) || string.IsNullOrEmpty(parameter.pwd))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!loginname:{parameter.loginname}", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var reop = new BizRepository())
            {
                var user = await reop.sta_userGetAsync(parameter.loginname, CommonHelper.GetMD5(parameter.pwd));
                if (user != null)
                {
                    var redisuser = FilterHelp.SaveRedis(user.uid.ToString());
                    user.expi_time = redisuser.expTime;
                    string token = JWT.JsonWebToken.Encode(user, CommonHelper.JWTsecretKey, JWT.JwtHashAlgorithm.HS256);
                    return JsonResponseHelper.HttpRMtoJson(new { Authorization = token }, HttpStatusCode.OK, ECustomStatus.Success);
                }
                else
                {
                    return JsonResponseHelper.HttpRMtoJson("用户名或密码错误！", HttpStatusCode.OK, ECustomStatus.Fail);
                }

            }

        }
        [HttpPost]
        [Route("addstauser")]
        public async Task<HttpResponseMessage> addsta_user(UserParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.loginname) || string.IsNullOrEmpty(parameter.pwd) || string.IsNullOrEmpty(parameter.nickname) || string.IsNullOrEmpty(parameter.tel))
                return JsonResponseHelper.HttpRMtoJson("parameter is error", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var reop = new BizRepository())
            {
                var user = new sta_user();
                user.loginname = parameter.loginname;
                user.pwd = CommonHelper.GetMD5(parameter.pwd);
                user.nickname = parameter.nickname;
                user.tel = parameter.tel;
                bool flag = await reop.addOrUpdapesta_userAsync(user);
                return JsonResponseHelper.HttpRMtoJson(flag, HttpStatusCode.OK, ECustomStatus.Success);
            }

        }
    }
}
