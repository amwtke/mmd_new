using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.MQ.MD;
using MD.Lib.Weixin.Pay;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using MD.Model.MQ.MD;
using MD.Wechat.Controllers.PinTuanController.Pay;
using MD.WeChat.Filters;
using MD.Lib.Weixin.Services;
using System.Net.Http;
using MD.Lib.Util;
using System.Net;
using MD.Lib.DB.Redis.MD;

namespace MD.WeChat.Controllers.ApiTest
{
    public partial class WebApiController : ApiController
    {
        [HttpGet]
        [Route("test/pay/queryorder")]
        public async Task<string> QueryOrder(string tid, string oid)
        {
            var xmlDoc = await WXPayHelper.GZHPay_QueryOrderAsync(TestPayConfig.Appid, TestPayConfig.Mid, TestPayConfig.Key,
                tid, oid);
            if (xmlDoc != null)
                return xmlDoc.ToString();
            return "返回空！";
        }

        [HttpGet]
        [Route("test/pay/refund")]
        public string Refund(string tid, string oid)
        {
            var certFile = WXPayHelper.GetCetFilePath(TestPayConfig.Appid);
            var xmlDoc = WXPayHelper.Refund(TestPayConfig.Appid, TestPayConfig.Mid, TestPayConfig.Key, tid, oid,
                WXPayHelper.GenWXOrderId(), certFile, 1, 1, null);
            if (xmlDoc != null)
                return xmlDoc.ToString();
            return "返回空！";
        }

        [HttpGet]
        [Route("test/pay/refund2")]
        public async Task<bool> Refund2(string appid, string oid)
        {
            return await MdWxPayUtil.RefundAsync(appid, oid);
        }

        [HttpGet]
        [Route("test/pay/refund3")]
        public bool Refund3(string appid, string oid)
        {
            try
            {
                MqRefundManager.SendMessageAsync(new MqWxRefundObject() { appid = appid, out_trade_no = oid });
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        [Route("test/pay/queryrefund")]
        public string Refund(string tid)
        {
            var xmlDoc = WXPayHelper.QueryRefund(TestPayConfig.Appid, TestPayConfig.Mid, tid, TestPayConfig.Key, null);
            if (xmlDoc != null)
                return xmlDoc.ToString();
            return "返回空！";
        }
        [HttpGet]
        [Route("test/pay/queryrefund2")]
        public string SearchRefund(Guid mid, string tid)
        {
            var mer = RedisMerchantOp.GetByMid(mid);
            var xmlDoc = WXPayHelper.QueryRefund(mer.wx_appid, mer.wx_mch_id, tid, mer.wx_apikey, null);
            if (xmlDoc != null)
                return xmlDoc.ToString();
            return "返回空！";
        }


        [HttpGet]
        [Route("product/es/setstatus")]
        public async Task<bool> setstatus(Guid pid, int status)
        {
            var p = await EsProductManager.GetByPidAsync(pid);
            if (p != null)
            {
                p.status = 0;
                return await EsProductManager.AddOrUpdateAsync(p);
            }
            return false;
        }

        [HttpGet]
        [Route("user/es/openid")]
        public async Task<IndexUser> getuserbyopenid(string openid)
        {
            var p = await EsUserManager.GetByOpenIdAsync(openid);
            return p;
        }

        #region 退款
        /// <summary>
        /// 矫正数据库和ES，把退款成功又退款失败的订单状态修改为已退款
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("pay/util/verify")]
        public async Task<bool> refundVerify()
        {
            using (var repo = new BizRepository())
            {
                //矫正数据库
                var orders = await repo.OrderGetByStatus(new List<int>() { (int)EOrderStatus.退款中 });
                if (orders != null && orders.Count > 0)
                {
                    foreach (var o in orders)
                    {
                        //退款失败 而且 不需要重新退款的需要更改订单状态。
                        if (!await repo.RefundIsNeedAsnyc(o.o_no) && o.status == (int)EOrderStatus.退款中)
                        {
                            o.status = (int)EOrderStatus.已退款;
                            await repo.OrderUpDateAsync(o);
                        }
                    }
                }

                //矫正index
                var indexOrders =
                    await EsOrderManager.SearchByStatusesAsnyc("", new List<int>() { (int)EOrderStatus.退款中 }, 10000);
                if (indexOrders != null && indexOrders.Count > 0)
                {
                    foreach (var o in indexOrders)
                    {
                        if (!await repo.RefundIsNeedAsnyc(o.o_no))
                        {
                            o.status = (int)EOrderStatus.已退款;
                            await EsOrderManager.AddOrUpdateAsync(o);
                        }
                    }
                }

                return true;
            }
        }
        /// <summary>
        /// 查询因【交易未结算资金不足，请使用可用余额退款】而退款失败的订单
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("test/refund/refundFailByNOTENOUGH")]
        public async Task<HttpResponseMessage> refundFailByNOTENOUGH()
        {
            //WxServiceHelper.Md_GoExpire_RefundingOrderProcess();
            //return JsonResponseHelper.HttpRMtoJson("", HttpStatusCode.OK, ECustomStatus.Success); ;
            using (var repo = new BizRepository())
            {
                //查询出退款失败的订单
                var otnList = repo.OrderGetByStatus_TB(new List<int>() { (int)EOrderStatus.退款中 });
                if (otnList != null && otnList.Count > 0)
                {
                    //退款失败的，并且报错信息为：NOTENOUGH，交易未结算资金不足，请使用可用余额退款
                    //var wxrefundList = repo.GetWXRefundFail(otnList, new List<string>() { "NOTENOUGH" });
                    if (otnList != null && otnList.Count > 0)
                    {
                        List<object> List = new List<object>();
                        foreach (var order in otnList)
                        {
                            var user = EsUserManager.GetById(order.buyer);
                            if (user == null) continue;
                            var mer = repo.GetMerchantByAppid(user.wx_appid);
                            if (mer == null) continue;
                            var wxrefund = repo.WXRefundByOtn(order.o_no);
                            if (wxrefund == null) continue;
                            var refund = await repo.RefundIsNeedAsnyc(order.o_no);
                            if (refund == false) continue;

                            //MqRefundManager.SendMessageAsync(new MqWxRefundObject() { appid = mer.wx_appid, out_trade_no = order.o_no });
                            List.Add(new
                            {
                                商家 = mer.name,
                                订单号 = order.o_no,
                                订单状态 = order.status == 0 ? "退款中" : order.status == 8 ? "退款失败" : "状态未知" + order.status,
                                最近一次退款时间 = CommonHelper.FromUnixTime(wxrefund.init_time).ToString(),
                                result_code = wxrefund.result_code,
                                err_code = wxrefund.err_code,
                                err_code_des = wxrefund.err_code_des,
                                refund_id = wxrefund.refund_id,
                                cash_fee = wxrefund.cash_fee
                            });
                        }

                        return JsonResponseHelper.HttpRMtoJson(new { total = otnList.Count, list = List }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                }
            }
            return JsonResponseHelper.HttpRMtoJson("暂无退款失败（交易未结算资金不足，请使用可用余额退款）的订单", HttpStatusCode.OK, ECustomStatus.Success);
        }

        #endregion 
    }
}