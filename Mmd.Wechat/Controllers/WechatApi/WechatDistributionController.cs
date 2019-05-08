using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB.Professional;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Distribution;
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
    [RoutePrefix("api/distribution")]
    [AccessFilter]
    public class WechatDistributionController : ApiController
    {
        /// <summary>
        /// 获取我推广的该团订单
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getmytuiguangorders")]
        public async Task<HttpResponseMessage> GetMyTuiGuangOrder(DistributionParameter parameter)
        {
            if (parameter == null || parameter.gid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty) || parameter.pageIndex < 1)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            List<object> retobj = new List<object>();
            using (var repo = new BizRepository())
            {
                int pageSize = MdWxSettingUpHelper.GetPageSize();
                var tuple = await repo.GetDistributionByGidandUidAsync(parameter.gid, parameter.uid, parameter.pageIndex, pageSize, (int)EDisSourcetype.订单佣金);
                if (tuple != null)
                {
                    int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
                    foreach (var dis in tuple.Item2)
                    {
                        var order = await EsOrderManager.GetByIdAsync(dis.oid);
                        if (order == null)
                            continue;
                        retobj.Add(new
                        {
                            order.o_no,
                            order_price = order.actual_pay / 100.00,
                            commission = dis.commission / 100.00
                        });
                    }
                    return JsonResponseHelper.HttpRMtoJson(new
                    {
                        totalCount = tuple.Item1,
                        totalPage = totalPage,
                        sumCommission = tuple.Item3 / 100.00,
                        olist = retobj
                    }, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
            return JsonResponseHelper.HttpRMtoJson(null, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 获取我的佣金记录
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Getmytuiguanglist")]
        public async Task<HttpResponseMessage> GetMyTuiGuangList(DistributionParameter parameter)
        {
            if (parameter == null || parameter.uid.Equals(Guid.Empty) || parameter.pageIndex < 1 || parameter.mid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            List<object> retobj = new List<object>();
            using (var repo = new BizRepository())
            {
                var writeoffer = repo.WoerCanWriteOff_TB(parameter.mid, parameter.uid);
                if (writeoffer == null)
                    return JsonResponseHelper.HttpRMtoJson("您当前不是核销员", HttpStatusCode.OK, ECustomStatus.Fail);
                double commission = writeoffer.commission / 100.00;//当前核销员的总佣金
                int pageSize = MdWxSettingUpHelper.GetPageSize();
                var tuple = await repo.GetDistributionByUidAsync(parameter.uid, parameter.pageIndex, pageSize);
                if (tuple != null)
                {
                    int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
                    foreach (var dis in tuple.Item2)
                    {
                        if (dis.sourcetype == (int)EDisSourcetype.订单佣金)
                        {
                            var order = await EsOrderManager.GetByIdAsync(dis.oid);
                            var group = await EsGroupManager.GetByGidAsync(dis.gid);
                            if (order == null || group == null)
                                continue;
                            retobj.Add(new
                            {
                                title = group.title,
                                order.o_no,
                                getcommissiontime = (int)dis.lastupdatetime,
                                commission = dis.commission / 100.00,
                                sourcetypeName = ((EDisSourcetype)dis.sourcetype).ToString(),
                                sourcetype = dis.sourcetype
                            });
                        }
                        else if(dis.sourcetype == (int)EDisSourcetype.佣金结算)
                        {
                            retobj.Add(new
                            {
                                title = EDisSourcetype.佣金结算.ToString(),
                                o_no = "",
                                getcommissiontime = (int)dis.lastupdatetime,
                                commission = dis.commission / 100.00,
                                sourcetypeName = ((EDisSourcetype)dis.sourcetype).ToString(),
                                sourcetype = dis.sourcetype
                            });
                        }
                    }
                    return JsonResponseHelper.HttpRMtoJson(new
                    {
                        commission,
                        totalPage = totalPage,
                        olist = retobj
                    }, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
            return JsonResponseHelper.HttpRMtoJson(null, HttpStatusCode.OK, ECustomStatus.Success);
        }
    }
}