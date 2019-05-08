using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB.Activity;
using MD.Model.DB.Code;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Activity;
using MD.WeChat.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/sign")]
    [AccessFilter]
    public class SignController : ApiController
    {
        [HttpPost]
        [Route("isexistssign")]
        public async Task<HttpResponseMessage> IsExistsSign(signParameter parameter)
        {
            //
            if (parameter == null || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!appid:{parameter.appid},opid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var fxUrl = MdWxSettingUpHelper.GenSignUrl(parameter.appid);
            var index_act_sign = await EsAct_signManager.GetByAppidAsync(parameter.appid, (int)ESignStatus.已上线);
            if (index_act_sign == null)//无配置签到信息
                return JsonResponseHelper.HttpRMtoJson(new { isexists = 0, issign = -1, signCount = 0, usid = "", fxUrl, hxUrl = "", signMessage = index_act_sign }, HttpStatusCode.OK, ECustomStatus.Success);
            var signCount = await EsAct_usersignManager.GetCountBySidAsync(Guid.Parse(index_act_sign.Id));
            var index_act_usersign = await EsAct_usersignManager.GetBysidAsync(Guid.Parse(index_act_sign.Id), parameter.openid);
            var stock = index_act_sign.awardQuatoCount <= 0 ? 0 : index_act_sign.awardQuatoCount;
            if (index_act_usersign != null)//我的签到数据
            {
                //已经签到了
                var hxUrl = MdWxSettingUpHelper.GenWriteOffSignUrl(parameter.appid, index_act_usersign.Id);
                return JsonResponseHelper.HttpRMtoJson(new { stock = stock, isexists = 1, issign = index_act_usersign.status, fxUrl, hxUrl, signCount, usid = index_act_usersign.Id, signMessage = index_act_sign }, HttpStatusCode.OK, ECustomStatus.Success);
            }
            //该用户还没签到
            return JsonResponseHelper.HttpRMtoJson(new { stock= stock, isexists = 1, issign = -1, fxUrl, hxUrl = "", usid = "", signCount, signMessage = index_act_sign }, HttpStatusCode.OK, ECustomStatus.Success);
        }
        [HttpPost]
        [Route("clicksign")]
        public async Task<HttpResponseMessage> clickSign(signParameter parameter)
        {
            if (parameter == null || parameter.sid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"参数错误,bid{parameter.sid},appid:{parameter.appid},opid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null || !user.wx_appid.Equals(parameter.appid))
                return JsonResponseHelper.HttpRMtoJson($"当前用户不存在,uid:{user?.Id},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var index_act_sign = await EsAct_signManager.GetByAppidAsync(parameter.appid, (int)ESignStatus.已上线);
            if (index_act_sign == null || index_act_sign.awardQuatoCount <= 0)
                return JsonResponseHelper.HttpRMtoJson($"该活动已过期或已经被领完", HttpStatusCode.OK, ECustomStatus.Fail);
            string hxUrl = "";
            string fxUrl = MdWxSettingUpHelper.GenSignUrl(parameter.appid);
            var signCount = await EsAct_usersignManager.GetCountBySidAsync(Guid.Parse(index_act_sign.Id));
            var index_act_usersign = await EsAct_usersignManager.GetBysidAsync(parameter.sid, parameter.openid);
            if (index_act_usersign != null)
            {
                hxUrl = MdWxSettingUpHelper.GenWriteOffSignUrl(parameter.appid, index_act_usersign.Id);
                return JsonResponseHelper.HttpRMtoJson(new { isexists = 1, issign =0, fxUrl, hxUrl, signCount, usid = index_act_usersign?.Id, signMessage = index_act_sign }, HttpStatusCode.OK, ECustomStatus.Success);
            }
            //开始签到
            UserSign us = new UserSign()
            {
                usid = Guid.NewGuid(),
                uid = Guid.Parse(user.Id),
                openid = user.openid,
                sid = Guid.Parse(index_act_sign.Id),
                mid = Guid.Parse(user.mid),
                status = (int)EUserSignStatus.已签到未领取,
                signTime = CommonHelper.ToUnixTime(DateTime.Now),
            };
            using (var acti = new ActivityRepository())
            {
                if (await acti.AddOrUpdateUserSignAsync(us))
                {
                    //更新ES
                    var index_user_sign = await EsAct_usersignManager.GenObjectAsync(us.usid);
                    if (index_user_sign != null)
                        await EsAct_usersignManager.AddOrUpdateAsync(index_user_sign);
                }
            }
            hxUrl = MdWxSettingUpHelper.GenWriteOffSignUrl(parameter.appid, us.usid.ToString());
            return JsonResponseHelper.HttpRMtoJson(new { isexists = 1, issign = us.status, fxUrl, hxUrl, signCount, usid = index_act_usersign?.Id, signMessage = index_act_sign }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("hexiao")]
        public async Task<HttpResponseMessage> hexiao(signParameter parameter)
        {
            try
            {
                if (parameter == null || parameter.usid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid))
                {
                    return JsonResponseHelper.HttpRMtoJson($"parameter error!usid:{parameter.usid},openid:{parameter.openid},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                #region 核销逻辑

                using (var Activ = new ActivityRepository())
                {
                    //查询该订单是否存在
                    var userSign = await Activ.GetUserSignByIdAsync(parameter.usid);
                    if (userSign == null || userSign.usid.Equals(Guid.Empty))
                        return JsonResponseHelper.HttpRMtoJson("未找到签到数据！", HttpStatusCode.OK, ECustomStatus.Fail);
                    var index_act_sign = await EsAct_signManager.GetByAppidAsync(parameter.appid, (int)ESignStatus.已上线);//签到宝贝信息
                    //验证商品状态
                    if (userSign.status != (int)EUserSignStatus.已签到未领取)
                        return JsonResponseHelper.HttpRMtoJson(new { isexists = 1, issign = 2, signMessage = index_act_sign }, HttpStatusCode.OK, ECustomStatus.Success);

                    var currentuser = await EsUserManager.GetByOpenIdAsync(parameter.openid);//获取当前用户信息
                    if (currentuser == null)
                        return JsonResponseHelper.HttpRMtoJson("核销员不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                    Guid currentUid = Guid.Parse(currentuser.Id);

                    //验证是否核销
                    if (!userSign.writeoffer.Equals(Guid.Empty))
                    {
                        return JsonResponseHelper.HttpRMtoJson(new { isexists = 1, issign = 2, signMessage = index_act_sign }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                    //验证商家与核销员
                    using (var repo = new BizRepository())
                    {
                        var mer = await repo.GetMerchantByMidAsync(userSign.mid);
                        if (mer == null)
                            return JsonResponseHelper.HttpRMtoJson($"订单所在商家错误！mid:{userSign.mid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (!mer.wx_appid.Equals(parameter.appid))
                            return JsonResponseHelper.HttpRMtoJson($"订单商家错误，appid不符！ut's appid:{mer.wx_appid},而appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (!await repo.WoerCanWriteOff(mer.mid, currentUid))
                        {
                            return JsonResponseHelper.HttpRMtoJson($"您无权核销此订单！编号：{userSign.usid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        }
                        var woer = await repo.WoerGetByUidAsync(currentUid);
                        //完成核销
                        userSign.writeoffer = currentUid;
                        userSign.writeoffTime = CommonHelper.GetUnixTimeNow();
                        userSign.status = (int)EUserSignStatus.已领取;
                        if (await Activ.AddOrUpdateUserSignAsync(userSign))
                        {
                            //更新ES
                            var index_us = await EsAct_usersignManager.GenObjectAsync(userSign.usid);
                            var flag = await EsAct_usersignManager.AddOrUpdateAsync(index_us);
                            var sign = await Activ.UpdateSignQuota_countAsync(userSign.sid);
                            if (sign != null)
                            {
                                //更新ES
                                var index_sign = await EsAct_signManager.GenObjectAsync(sign);
                                await EsAct_signManager.AddOrUpdateAsync(index_sign);
                            }
                        }
                        return JsonResponseHelper.HttpRMtoJson(new { isexists = 1, issign = userSign.status, signMessage = index_act_sign }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResponseHelper.HttpRMtoJson("核销失败" + ex.Message, HttpStatusCode.InternalServerError,
                      ECustomStatus.Fail);
            }
        }
    }
}
