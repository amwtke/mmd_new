using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.JsSdk;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;
using MD.Model.DB.Code;
using MD.Wechat.Controllers.PinTuanController.Group;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.Wechat.Controllers.WechatApi.Parameters.biz;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.za;
using MD.WeChat.Controllers.ApiTest;
using MD.WeChat.Filters;
using Senparc.Weixin.MP.AdvancedAPIs;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api")]
    [AccessFilter]
    public class WechatApiController : ApiController
    {
        [HttpPost]
        [Route("test")]
        public HttpResponseMessage test(BaseParameter postParameter)
        {
            if (string.IsNullOrEmpty(postParameter?.openid) || postParameter.uid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson("Error", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
            var ret = new { openid = postParameter.openid, uid = postParameter.uid };
            return JsonResponseHelper.HttpRMtoJson(ret, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpGet]
        [Route("GetPayUrl")]
        public HttpResponseMessage GetPayUrl(string appid, string id, int type)
        {
            if (!string.IsNullOrEmpty(appid))
            {
                string state = type == (int)ETuan.KT ? ETuan.KT.ToString() : ETuan.CT.ToString();
                string url = WXPayHelper.GenPayUrl(appid, id, state);
                return JsonResponseHelper.HttpRMtoJson(new { url }, HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson(null, HttpStatusCode.NotAcceptable, ECustomStatus.Fail);
        }

        [HttpPost]
        [Route("getjssdk")]
        public async Task<HttpResponseMessage> GetJsSdk(JsSdkParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.url))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            //string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(parameter.appid);
            string url = parameter.url;
            if (parameter.url.Contains("#"))
            {
                int index = parameter.url.IndexOf("#", StringComparison.Ordinal);
                url = url.Substring(0, index);
            }

            //MDLogger.LogInfoAsync(typeof(WechatApiController), $"jssdk日志。appid:{parameter.appid},url:{url}");

            var tuple = await MdJsSdkHelper.GetSignatureAsync(parameter.appid, url);

            var mer = await RedisMerchantOp.GetByAppidAsync(parameter.appid);
            if (string.IsNullOrEmpty(mer?.wx_appid))
                return JsonResponseHelper.HttpRMtoJson($"mer is null:{parameter.appid}", HttpStatusCode.OK,
                    ECustomStatus.Fail);

            return
                JsonResponseHelper.HttpRMtoJson(
                    new
                    {
                        appId = parameter.appid,
                        timeStamp = tuple.Item1,
                        nonceStr = tuple.Item2,
                        signature = tuple.Item3,
                        mid = mer.mid.ToString()
                    }, HttpStatusCode.OK, ECustomStatus.Success);

        }

        [HttpPost]
        [Route("getuwinfo")]
        public async Task<HttpResponseMessage> getuwinfo(BaseParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            using (var repo = new BizRepository())
            {
                var ret = await repo.UserWriteoffGetByMidAndUidAsync(parameter.mid, parameter.uid);
                if (ret == null)
                {
                    return JsonResponseHelper.HttpRMtoJson(new { name = "", tel = "" }, HttpStatusCode.OK, ECustomStatus.Success);
                }

                return JsonResponseHelper.HttpRMtoJson(new { name = ret.user_name, tel = ret.cellphone, wopid = ret.woid }, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }
        [HttpPost]
        [Route("getupinfo")]
        public async Task<HttpResponseMessage> getupinfo(GroupParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty) || parameter.gid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var group = await EsGroupManager.GetByGidAsync(parameter.gid);
            if (group == null)
                return JsonResponseHelper.HttpRMtoJson($"es group is null,gid:{parameter.gid}!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var ret = await repo.GetUserPostDefaultByUidAsync(parameter.uid);
                if (ret == null||string.IsNullOrEmpty(group.ltid)||group.ltid.Equals(Guid.Empty.ToString()))
                {
                    return JsonResponseHelper.HttpRMtoJson(new
                    {
                        name = "",
                        tel = "",
                        province = "",
                        city = "",
                        district = "",
                        code = "",
                        address = "",
                        upid = "",
                        post_price = 0
                    }, HttpStatusCode.OK, ECustomStatus.Success);
                }
                else
                {
                    var lt = await EsLogisticsTemplateManager.GetFeeByCode(Guid.Parse(group.ltid), ret.code);//获取运费
                    return JsonResponseHelper.HttpRMtoJson(new
                    {
                        name = ret.name,
                        tel = ret.cellphone,
                        province = ret.province,
                        ret.city,
                        ret.district,
                        ret.code,
                        ret.address,
                        upid = ret.upid,
                        post_price = lt > 0 ? lt / 100.00 : lt
                    }, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
        }

        [HttpPost]
        [Route("getgidbygoid")]
        public async Task<HttpResponseMessage> getgidbygoid(getgidbygoidParameter parameter)
        {
            if (parameter == null || parameter.goid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            var indexGroupOrder = await EsGroupOrderManager.GetByIdAsync(parameter.goid);
            if (indexGroupOrder == null)
                return JsonResponseHelper.HttpRMtoJson($"goid:{parameter.goid}不存在！", HttpStatusCode.OK,
                    ECustomStatus.Success);
            return JsonResponseHelper.HttpRMtoJson(new { gid = indexGroupOrder.gid }, HttpStatusCode.OK,
                ECustomStatus.Success);
        }
    }
}