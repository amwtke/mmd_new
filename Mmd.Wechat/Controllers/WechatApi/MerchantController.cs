using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Biz.Merchant;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.WeChat.Filters;
using MD.Lib.ElasticSearch;
using MD.Lib.ElasticSearch.MD;
using MD.Model.DB.Code;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/merchant")]
    [AccessFilter]
    public class MerchantController : ApiController
    {
        [HttpPost]
        [Route("GetInfo")]
        public async Task<HttpResponseMessage> getinfo(BaseParameter postParameter)
        {
            if(postParameter.mid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,mid is empty!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var mer = await repo.GetMerchantByMidAsync(postParameter.mid);
                if(mer==null)
                    return JsonResponseHelper.HttpRMtoJson($"mid;{postParameter.mid}的商家找不到！", HttpStatusCode.OK, ECustomStatus.Fail);
                var retobj =
                    new
                    {
                        mid = postParameter.mid.ToString(),
                        headpic = mer.logo_url,
                        advertisPic=mer.advertise_pic_url,
                        slogen = mer.slogen,
                        name = mer.name,
                        intro=mer.brief_introduction,
                        service=mer.service_intro,
                        qrurl= mer.qr_url,
                        ad_pic=mer.advertise_pic_url,
                        shareurl= MdWxSettingUpHelper.GenEntranceUrl(mer.wx_appid)
                    };
                return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        [HttpPost]
        [Route("getView")]
        public async Task<HttpResponseMessage> getView(BaseParameter parameter)
        {
            if(parameter.mid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,mid is empty!", HttpStatusCode.OK, ECustomStatus.Fail);

            var result= await EsBizLogStatistics.GetViewCount(parameter.mid,parameter.from, parameter.to);
            return JsonResponseHelper.HttpRMtoJson(result, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 根据成交订单数排序查询商家
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getbyordercount")]
        public async Task<HttpResponseMessage> GetbyOrderCount(BaseParameter parameter)
        {
            if(parameter==null||parameter.pageIndex<=0||parameter.pageSize<=0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error,pageIndex:{parameter.pageIndex},pageSize:{parameter.pageSize}",
                    HttpStatusCode.OK, ECustomStatus.Fail);
            var tuple =await EsOrderManager.GetMid_OrderCountAsync(new List<int> {
                (int)EOrderStatus.已成团未提货,
                (int)EOrderStatus.已成团未发货,
                (int)EOrderStatus.已成团配货中,
                (int)EOrderStatus.已发货待收货,
                (int)EOrderStatus.拼团成功 },parameter.pageIndex, parameter.pageSize,parameter.from,parameter.to);
            int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
            List<object> retobj = new List<object>();
            foreach (var o in tuple.Item2)
            {
                Guid mid = Guid.Parse(o.Key);
                var temp = await RedisMerchantOp.GetByMidAsync(mid);
                retobj.Add(new {
                   temp.name,
                   temp.logo_url,
                   ordercount=o.DocCount
                });
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
        }
    }
}
