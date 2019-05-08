using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Lib.DB.Redis.Objects;
using MD.Lib.DB.Repositorys;
using MD.Lib.Weixin.User;
using MD.Model.Configuration;
using MD.Model.DB;
using MD.Model.MQ;

namespace mmd.wechat.Controllers.Base
{
    public class MVCNeedWeixinCallBackBaseController : Controller
    {
        protected const string LOGIN_PAGE_URL = @"F2E/index.html";

        //protected virtual async Task<ActionResult> LoginCallBack(string code, string state, string xueshengUrl, string jiaoshouUrl)
        //{
        //    try
        //    {
        //        if (code == null)
        //            return Content("code是空！");
        //        //LogHelper.LogInfoAsync(typeof(WeChatCallBackController), @"1\code=" + code);

        //        var config = BK.Configuration.BK_ConfigurationManager.GetConfig<WeixinConfig>();
        //        bool Isbinded = true;

        //        var openid = await WXAuthHelper.GetOpenIdWXCallBackAsync(config.WeixinAppId, config.WeixinAppSecret, code, async delegate (OAuthAccessTokenResult result)
        //        {
        //            //是否需要绑定账号

        //            using (UserRepository userRepository = new UserRepository())
        //            {
        //                //如果OPenid绑定了，就不需要再向微信请求userinfo的信息了.
        //                //如果没有绑定，则需要刷新accesstoken，然后请求userinfo；并将userinfo存入redis。
        //                Isbinded = await userRepository.IsUserOpenidExist(result.openid);
        //            }

        //            if (!Isbinded)
        //                //如果没有绑定就要存储token信息
        //                await WeChatCallBackControllerHelper.SaveOAuthUserTokenAsync(result);
        //            return !Isbinded;//如果绑定了就不需要获取userinfo信息了
        //        }, async delegate (OAuthUserInfo user)
        //        {
        //            //如果需要绑定用户信息则，此处存储用户信息
        //            return await WeChatCallBackControllerHelper.SaveOAuthUserInfoToRedis(user);
        //        });
        //        //再次判断是否需要绑定

        //        //存入cookie供前端代码调用
        //        Response.Cookies["openid"].Value = openid;
        //        Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);


        //        //如果是tester则不管怎么样都要去注册。
        //        //可以通过wechat.51science.cn/weixinapi/addtester/?openid=xxx来添加
        //        //wechat.51science.cn/weixinapi/rmtester/?openid=xxx删除
        //        bool isToRegister = !Isbinded || await WXAuthHelper.IsTester(openid);
        //        //BKLogger.LogInfoAsync(typeof(MVCNeedWeixinCallBackBaseController), "isToRegister:"+isToRegister);
        //        //BKLogger.LogInfoAsync(typeof(MVCNeedWeixinCallBackBaseController), "Isbinded:" + Isbinded);
        //        if (!isToRegister)
        //        {
        //            //记录用户行为
        //            await UserLoginBehaviorOp.AddLoginCountAsync(openid);
        //            await UserLoginBehaviorOp.AddUpdateLastLoginTimeAsync(openid);
        //            //跳转到个人主页
        //            UserInfo userinfo = null;
        //            using (UserRepository userRepository = new UserRepository())
        //            {
        //                userinfo = await userRepository.GetUserInfoByOpenid(openid);
        //            }

        //            BKLogger.LogInfoAsync(typeof(MVCNeedWeixinCallBackBaseController), "userinfo:" + userinfo.uuid.ToString() + " type:" + userinfo.IsBusiness.Value.ToString());

        //            BizMQ bizobj = new BizMQ("微信登陆", openid, userinfo.uuid, userinfo.Name + " 登陆了！");
        //            BKLogger.LogBizAsync(typeof(MVCNeedWeixinCallBackBaseController), bizobj);

        //            //存cookie
        //            var cookieResult = Response.Cookies["type"].Value = userinfo.IsBusiness.Value.ToString();
        //            Response.Cookies["type"].Expires = DateTime.Now.AddYears(1);
        //            Response.Cookies["uuid"].Value = userinfo.uuid.ToString();
        //            Response.Cookies["uuid"].Expires = DateTime.Now.AddYears(1);

        //            //LogHelper.LogInfoAsync(typeof(MVCNeedWeixinCallBackBaseController), cookieResult.ToString());
        //            if (userinfo.IsBusiness.Value == 0)
        //                return Redirect(jiaoshouUrl);
        //            else
        //                return Redirect(xueshengUrl);
        //        }
        //        else
        //        {
        //            // login页面 
        //            return Redirect(LOGIN_PAGE_URL);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        BKLogger.LogErrorAsync(typeof(MVCNeedWeixinCallBackBaseController), ex);
        //        return Content(ex.ToString());
        //    }
        //}
    }
}