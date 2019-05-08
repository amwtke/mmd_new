using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using MD.Configuration;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.MQ;
using MD.Lib.MQ.MD;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Message;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Services;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.MQ;
using MD.Model.MQ.MD;
using Senparc.Weixin.MP.AdvancedAPIs.GroupMessage;
using Senparc.Weixin.MP.TenPayLibV3;
using MD.Model.DB.Professional;

namespace MD.Lib.Weixin.Pay
{
    public static class MdWxPayUtil
    {
        private static int numberOfQs = 100;
        private static ConcurrentDictionary<string, MqClient> _qName2Client = new ConcurrentDictionary<string, MqClient>();
        private static MqWxPayCallbackConfig config = MdConfigurationManager.GetConfig<MqWxPayCallbackConfig>();

        private static async Task SendToWxPayQueue(WXPayResult obj)
        {
            try
            {
                using (var repo = new BizRepository())
                {
                    //根据otn获取gid
                    var otn = obj.out_trade_no;
                    if (string.IsNullOrEmpty(otn))
                        return;
                    var order = await repo.OrderGetByOutTradeNoAsync(otn);
                    if (order == null)
                    {
                        MDLogger.LogInfoAsync(typeof(MdWxPayUtil), $"SendToWxPayQueue->error!otn:{obj.out_trade_no}");
                        return;
                    }

                    //根据gid获取queueName
                    Guid gid = order.gid;

                    //绝对值很重要
                    int i = Math.Abs(gid.GetHashCode()) % numberOfQs;
                    string queueName = WxServiceHelper.GetWxPayQueueName(config.QueueName, i);

                    //看看有没有缓存
                    MqClient client;
                    if (!_qName2Client.TryGetValue(queueName, out client))
                    {
                        client = new MqClient(queueName, config.HostName, int.Parse(config.Port), config.UserName, config.Password);
                        _qName2Client[queueName] = client;
                    }
                    //await client.SendMessageAsync(obj);
                    await client.SendMessageAsync(obj);
                    MDLogger.LogInfoAsync(typeof(MdWxPayUtil), $"我来了3，QueueName:{queueName},datetime:{DateTime.Now}");
                }
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(MdWxPayUtil), ex);
            }

        }
        /// <summary>
        /// 记录日志的程序
        /// </summary>
        /// <param name="resHandler"></param>
        public static void Log(ResponseHandler resHandler)
        {
            string appid = resHandler.GetParameter("appid");
            string mch_id = resHandler.GetParameter("mch_id");
            string result_code = resHandler.GetParameter("result_code");
            string openid = resHandler.GetParameter("openid");
            string trade_type = resHandler.GetParameter("trade_type");
            string total_fee = resHandler.GetParameter("total_fee");
            string cash_fee = resHandler.GetParameter("cash_fee");
            string transaction_id = resHandler.GetParameter("transaction_id");
            string out_trade_no = resHandler.GetParameter("out_trade_no");
            string time_end = resHandler.GetParameter("time_end");

            if (!string.IsNullOrEmpty(openid))
                MDLogger.LogBizAsync(typeof(MdWxPayUtil), new BizMQ("支付回调", openid, Guid.NewGuid(), $"appid:{appid},mch_id:{mch_id},result_code:{result_code},openid:{openid},trade_type={trade_type},total_fee:{total_fee},cash_fee:{cash_fee},transaction_id:{transaction_id},out_trade_no:{out_trade_no},time_end:{time_end}"));
        }


        public static void LogCallBackError(ResponseHandler resHandler)
        {
            string appid = resHandler.GetParameter("appid");
            string mch_id = resHandler.GetParameter("mch_id");
            string result_code = resHandler.GetParameter("result_code");
            string openid = resHandler.GetParameter("openid");
            string trade_type = resHandler.GetParameter("trade_type");
            string total_fee = resHandler.GetParameter("total_fee");
            string cash_fee = resHandler.GetParameter("cash_fee");
            string transaction_id = resHandler.GetParameter("transaction_id");
            string out_trade_no = resHandler.GetParameter("out_trade_no");
            string time_end = resHandler.GetParameter("time_end");

            if (!string.IsNullOrEmpty(openid))
                MDLogger.LogBizAsync(typeof(MdWxPayUtil), new BizMQ("!!!支付回调处理失败", openid, Guid.NewGuid(), $"appid:{appid},mch_id:{mch_id},result_code:{result_code},openid:{openid},trade_type={trade_type},total_fee:{total_fee},cash_fee:{cash_fee},transaction_id:{transaction_id},out_trade_no:{out_trade_no},time_end:{time_end}"));
        }

        public static string StandardResponse(string returnCode, string returnMsg)
        {
            string xml = string.Format(@"<xml>
   <return_code><![CDATA[{0}]]></return_code>
   <return_msg><![CDATA[{1}]]></return_msg>
</xml>", returnCode, returnMsg);
            return xml;
        }

        /// <summary>
        /// 将回调的out_trade_no放入回调处理队列。
        /// </summary>
        /// <param name="resHandler"></param>
        /// <returns></returns>
        public static async Task PayCallbackProcess(ResponseHandler resHandler)
        {
            try
            {
                string return_code = resHandler.GetParameter("return_code");
                string return_msg = resHandler.GetParameter("return_msg");

                string appid = resHandler.GetParameter("appid");
                string mch_id = resHandler.GetParameter("mch_id");
                string result_code = resHandler.GetParameter("result_code");
                string openid = resHandler.GetParameter("openid");
                string trade_type = resHandler.GetParameter("trade_type");
                string total_fee = resHandler.GetParameter("total_fee");
                string cash_fee = resHandler.GetParameter("cash_fee");
                string transaction_id = resHandler.GetParameter("transaction_id");
                string out_trade_no = resHandler.GetParameter("out_trade_no");
                string time_end = resHandler.GetParameter("time_end");

                string xml = resHandler.ParseXML();


                WXPayResult result = new WXPayResult();
                result.id = Guid.Empty;
                result.appid = appid;
                result.mch_id = mch_id;
                result.result_code = result_code;
                result.out_trade_no = out_trade_no;
                result.openid = openid;
                result.trade_type = trade_type;
                result.total_fee = int.Parse(total_fee);
                result.cash_fee = int.Parse(cash_fee);
                result.transaction_id = transaction_id;
                result.time_end = time_end;
                result.return_code = return_code;
                result.return_msg = return_msg;
                result.notify_xml = xml;
                result.timestamp = CommonHelper.GetUnixTimeNow();

                //MqWxPayResultManager.SendMessageAsync(result);

                await SendToWxPayQueue(result);
                //MDLogger.LogInfoAsync(typeof(MdWxPayUtil), $"我来了2！时间：{DateTime.Now}");
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MdWxPayUtil), ex);
            }

        }

        public static async Task PayCallbackProcess_robot(WXPayResult result)
        {
            try
            {

                //WXPayResult result = new WXPayResult();
                //result.id = Guid.Empty;
                //result.appid = appid;
                //result.mch_id = mch_id;
                //result.result_code = result_code;
                //result.out_trade_no = out_trade_no;
                //result.openid = openid;
                //result.trade_type = trade_type;
                //result.total_fee = int.Parse(total_fee);
                //result.cash_fee = int.Parse(cash_fee);
                //result.transaction_id = transaction_id;
                //result.time_end = time_end;
                //result.return_code = return_code;
                //result.return_msg = return_msg;
                //result.notify_xml = xml;
                //result.timestamp = CommonHelper.GetUnixTimeNow();

                //MqWxPayResultManager.SendMessageAsync(result);

                await SendToWxPayQueue(result);
                //MDLogger.LogInfoAsync(typeof(MdWxPayUtil), $"我来了2！时间：{DateTime.Now}");
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MdWxPayUtil), ex);
            }

        }

        /// <summary>
        /// 设置支付key。
        /// </summary>
        /// <param name="resHandler"></param>
        /// <returns></returns>
        public static async Task<bool> SetMidKeyAsync(ResponseHandler resHandler)
        {
            string appid = resHandler.GetParameter("appid");
            using (var repo = new BizRepository())
            {
                var mer = await repo.GetMerchantByAppidAsync(appid);
                if (mer != null && !string.IsNullOrEmpty(mer.wx_apikey))
                {
                    resHandler.SetKey(mer.wx_apikey);
                    return true;
                }
            }
            return false;
        }

        #region refund

        public static async Task<bool> RefundAsync(string appid, string out_trade_no)
        {
            try
            {
                if (string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(out_trade_no))
                    return false;
                using (var repo = new BizRepository())
                {
                    var payResult = await repo.WxPayResultGetByOutTradeNoAsync(out_trade_no, "SUCCESS");
                    //对于没有支付成功记录的退款历程。可能是因为支付成功的记录没有记录成功。
                    if (payResult == null)
                    {
                        var order = await repo.OrderGetByOutTradeNoAsync(out_trade_no);
                        if (order == null)
                            return false;

                        return await
                            RefundAsnyc(appid, order.actual_pay.Value, order.actual_pay.Value, "",
                                order.o_no);
                    }

                    return
                        await
                            RefundAsnyc(appid, payResult.total_fee.Value, payResult.cash_fee.Value, payResult.transaction_id,
                                payResult.out_trade_no);
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MdWxPayUtil), new Exception($"fun:RefundAsync,appid:{appid},otn:{out_trade_no},ex:{ex.Message}"));
            }
        }

        private static async Task<bool> RefundAsnyc(string appid, int total_fee, int cash_fee, string transaction_id, string out_trade_no)
        {
            using (var repo = new BizRepository())
            {
                try
                {
                    var mer = await repo.GetMerchantByAppidAsync(appid);
                    if (string.IsNullOrEmpty(mer?.wx_apikey) || string.IsNullOrEmpty(mer.wx_mch_id))
                    {
                        MDLogger.LogErrorAsync(typeof(MdWxPayUtil), new Exception($"退款失败！从appid:{appid}获取mer失败！"));
                        return false;
                    }
                    if (string.IsNullOrEmpty(transaction_id) && string.IsNullOrEmpty(out_trade_no))
                    {
                        MDLogger.LogErrorAsync(typeof(MdWxPayUtil), new Exception($"退款失败！tid与oid都是空！"));
                        return false;
                    }

                    //判断是否已经退款成功
                    if (!await repo.RefundIsNeedAsnyc(out_trade_no))
                        return false;

                    var appKey = mer.wx_apikey;
                    var certFile = WXPayHelper.GetCetFilePath(appid);
                    var outFundNo = CommonHelper.GetId32(EIdPrefix.RF);
                    var mch_id = mer.wx_mch_id;

                    var xDoc = WXPayHelper.Refund(appid, mch_id, appKey, transaction_id, out_trade_no,
                        outFundNo, certFile, total_fee, cash_fee, null);

                    return await ProcessRefundResponse(xDoc, transaction_id, out_trade_no);
                }
                catch (Exception ex)
                {
                    throw new MDException(typeof(MdWxPayUtil), new Exception($"fun:RefundAsnyc,appid:{appid},total_fee:{total_fee},transaction_id:{transaction_id},out_trade_no:{out_trade_no},ex:{ex.Message}"));
                }
            }
        }

        /// <summary>
        /// 退款的函数。考虑做队列。
        /// </summary>
        /// <param name="resHandler"></param>
        /// <param name="processReturnXml"></param>
        /// <returns></returns>
        public static void RefundAsync(ResponseHandler resHandler, Action<XDocument> processReturnXml)
        {

            string appid = resHandler.GetParameter("appid");
            string mch_id = resHandler.GetParameter("mch_id");
            string result_code = resHandler.GetParameter("result_code");
            string openid = resHandler.GetParameter("openid");
            string trade_type = resHandler.GetParameter("trade_type");
            string total_fee = resHandler.GetParameter("total_fee");
            string cash_fee = resHandler.GetParameter("cash_fee");
            string transaction_id = resHandler.GetParameter("transaction_id");
            string out_trade_no = resHandler.GetParameter("out_trade_no");
            string time_end = resHandler.GetParameter("time_end");

            //return await RefundAsync(appid,out_trade_no);
            MqRefundManager.SendMessageAsync(new MqWxRefundObject() { appid = appid, out_trade_no = out_trade_no });
        }

        /// <summary>
        /// 对微信退款返回的xml进行分析，确定错误或者消息。
        /// </summary>
        /// <param name="response"></param>
        /// <param name="tid"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        private static async Task<bool> ProcessRefundResponse(XDocument response, string tid, string oid)
        {
            var dic = ParseWxResponseXml(response);
            if (dic != null)
            {
                //存入数据库
                using (var repo = new BizRepository())
                {
                    WXRefund refund = new WXRefund()
                    {
                        id = Guid.NewGuid(),
                        out_trade_no = dic.ContainsKey("out_trade_no") ? dic["out_trade_no"] : oid,
                        transaction_id = dic.ContainsKey("transaction_id") ? dic["transaction_id"] : tid,
                        return_code = dic.ContainsKey("return_code") ? dic["return_code"] : null,
                        return_msg = dic.ContainsKey("return_msg") ? dic["return_msg"] : null,
                        appid = dic.ContainsKey("appid") ? dic["appid"] : null,
                        mch_id = dic.ContainsKey("mch_id") ? dic["mch_id"] : null,
                        out_refund_no = dic.ContainsKey("out_refund_no") ? dic["out_refund_no"] : null,
                        refund_id = dic.ContainsKey("refund_id") ? dic["refund_id"] : null,
                        refund_fee = dic.ContainsKey("refund_fee") ? int.Parse(dic["refund_fee"]) : (int?)null,
                        cash_fee = dic.ContainsKey("cash_fee") ? int.Parse(dic["cash_fee"]) : (int?)null,
                        total_fee = dic.ContainsKey("total_fee") ? int.Parse(dic["total_fee"]) : (int?)null,
                        refund_count = dic.ContainsKey("refund_count") ? int.Parse(dic["refund_count"]) : (int?)null,
                        result_code = dic.ContainsKey("result_code") ? dic["result_code"] : null,
                        err_code = dic.ContainsKey("err_code") ? dic["err_code"] : null,
                        err_code_des = dic.ContainsKey("err_code_des") ? dic["err_code_des"] : null,
                        init_response_xml = response.ToString(),
                        init_time = CommonHelper.GetUnixTimeNow(),
                        operation_flag = 1
                    };
                    //保存refund结果
                    await repo.SaveWxRefundCallBackAsync(refund);

                    //根据结果更改状态
                    if (dic.ContainsKey("return_code"))
                    {
                        var return_code = dic["return_code"].ToUpper();

                        //通信正常
                        if (return_code.Equals("SUCCESS"))
                        {
                            //退款成功
                            if (dic.ContainsKey("result_code") && dic["result_code"].ToUpper().Equals("SUCCESS"))
                            {
                                MDLogger.LogInfoAsync(typeof(MdWxPayUtil), $"out_trade_no:{dic["out_trade_no"]}退款成功！");

                                //将订单状态改成已退款,如果otn不为空，则为正确退款。
                                if (!string.IsNullOrEmpty(refund?.out_trade_no))
                                {
                                    var order = await repo.OrderGetByOutTradeNoAsync(refund.out_trade_no);
                                    if (order != null)
                                    {
                                        order.status = (int)EOrderStatus.已退款;
                                        if (await repo.OrderUpDateAsync(order))
                                        {
                                            //修正grouporder的user left
                                            repo.GroupOrderVerifyUserLeftAsync(order.goid);
                                            Group group = await repo.GroupGetGroupById(order.gid);
                                            //抽奖团退款不发拼团失败模板消息
                                            if (group != null && group.group_type == (int)EGroupTypes.普通团)
                                            {
                                                await MqWxTempMsgManager.SendMessageAsync(order, TemplateType.PTFail);
                                            }
                                        }
                                    }
                                }
                                return true;
                            }
                            //退款失败，且没有成功退款的记录。
                            if (dic.ContainsKey("result_code") && dic["result_code"].ToUpper().Equals("FAIL") &&
                                await repo.RefundIsNeedAsnyc(refund.out_trade_no))
                            {
                                MDLogger.LogInfo(typeof(MdWxPayUtil),
                                    $"退款失败了！！！result_code:{dic["result_code"]};error code:{dic["err_code"]};原因：{dic["err_code_des"]}");

                                //将订单状态改成 退款失败
                                if (!string.IsNullOrEmpty(refund?.out_trade_no))
                                {
                                    var order = await repo.OrderGetByOutTradeNoAsync(refund.out_trade_no);
                                    if (order != null)
                                    {
                                        order.status = (int)EOrderStatus.退款失败;
                                        if (await repo.OrderUpDateAsync(order))
                                        {
                                            //修正grouporder的user left
                                            repo.GroupOrderVerifyUserLeftAsync(order.goid);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //将订单状态改成 退款失败
                            if (!string.IsNullOrEmpty(refund?.out_trade_no))
                            {
                                var order = await repo.OrderGetByOutTradeNoAsync(refund.out_trade_no);
                                if (order != null)
                                {
                                    order.status = (int)EOrderStatus.退款失败;
                                    if (await repo.OrderUpDateAsync(order))
                                    {
                                        //修正grouporder的user left
                                        repo.GroupOrderVerifyUserLeftAsync(order.goid);
                                    }
                                }
                            }
                            MDLogger.LogErrorAsync(typeof(MdWxPayUtil), new Exception($"退款通信失败，returncode:{refund?.return_code},returnmessage:{refund?.return_msg},appid:{refund?.appid},out_trade_no:{refund?.out_trade_no}"));
                        }
                    }
                }//using
            }//dic
            return false;
        }
        #endregion

        #region 后台手动退款单个订单
        public static async Task<Tuple<bool, string>> OperationRefundAsync(string appid, string out_trade_no)
        {
            try
            {
                if (string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(out_trade_no))
                    return Tuple.Create(false, $"退款失败,参数为空,appid:{appid},out_trade_no:{out_trade_no}");
                using (var repo = new BizRepository())
                {
                    var payResult = await repo.WxPayResultGetByOutTradeNoAsync(out_trade_no, "SUCCESS");
                    //对于没有支付成功记录的退款历程。可能是因为支付成功的记录没有记录成功。
                    if (payResult == null)
                    {
                        var order = await repo.OrderGetByOutTradeNoAsync(out_trade_no);
                        if (order == null)
                            return Tuple.Create(false, $"查不到该订单,oid:{out_trade_no}");

                        return await
                            BeginRefundAsnyc(appid, order.actual_pay.Value, order.actual_pay.Value, "",
                                order.o_no);
                    }

                    return
                        await
                            BeginRefundAsnyc(appid, payResult.total_fee.Value, payResult.cash_fee.Value, payResult.transaction_id,
                                payResult.out_trade_no);
                }
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(MdWxPayUtil), ex);
            }
        }
        private static async Task<Tuple<bool, string>> BeginRefundAsnyc(string appid, int total_fee, int cash_fee, string transaction_id, string out_trade_no)
        {
            using (var repo = new BizRepository())
            {
                try
                {
                    var mer = await repo.GetMerchantByAppidAsync(appid);
                    if (string.IsNullOrEmpty(mer?.wx_apikey) || string.IsNullOrEmpty(mer.wx_mch_id))
                    {
                        return Tuple.Create(false, $"退款失败！从appid:{appid}获取mer失败！");
                    }
                    if (string.IsNullOrEmpty(transaction_id) && string.IsNullOrEmpty(out_trade_no))
                    {
                        return Tuple.Create(false, $"退款失败！tid与oid都是空！");
                    }

                    //判断是否已经退款成功
                    if (!await repo.RefundIsNeedAsnyc(out_trade_no))
                        return Tuple.Create(false, $"该订单{out_trade_no}已经退款过。");

                    var appKey = mer.wx_apikey;
                    var certFile = WXPayHelper.GetCetFilePath(appid);
                    var outFundNo = CommonHelper.GetId32(EIdPrefix.RF);
                    var mch_id = mer.wx_mch_id;

                    var xDoc = WXPayHelper.Refund(appid, mch_id, appKey, transaction_id, out_trade_no,
                        outFundNo, certFile, total_fee, cash_fee, null);

                    return await EndRefundResponse(xDoc, transaction_id, out_trade_no);
                }
                catch (Exception ex)
                {
                    throw new MDException(typeof(MdWxPayUtil), ex);
                }
            }
        }
        private static async Task<Tuple<bool, string>> EndRefundResponse(XDocument response, string tid, string oid)
        {
            var dic = ParseWxResponseXml(response);
            if (dic != null)
            {
                //存入数据库
                using (var repo = new BizRepository())
                {
                    #region 初始化wxrefund
                    WXRefund refund = new WXRefund()
                    {
                        id = Guid.NewGuid(),
                        out_trade_no = dic.ContainsKey("out_trade_no") ? dic["out_trade_no"] : oid,
                        transaction_id = dic.ContainsKey("transaction_id") ? dic["transaction_id"] : tid,
                        return_code = dic.ContainsKey("return_code") ? dic["return_code"] : null,
                        return_msg = dic.ContainsKey("return_msg") ? dic["return_msg"] : null,
                        appid = dic.ContainsKey("appid") ? dic["appid"] : null,
                        mch_id = dic.ContainsKey("mch_id") ? dic["mch_id"] : null,
                        out_refund_no = dic.ContainsKey("out_refund_no") ? dic["out_refund_no"] : null,
                        refund_id = dic.ContainsKey("refund_id") ? dic["refund_id"] : null,
                        refund_fee = dic.ContainsKey("refund_fee") ? int.Parse(dic["refund_fee"]) : (int?)null,
                        cash_fee = dic.ContainsKey("cash_fee") ? int.Parse(dic["cash_fee"]) : (int?)null,
                        total_fee = dic.ContainsKey("total_fee") ? int.Parse(dic["total_fee"]) : (int?)null,
                        refund_count = dic.ContainsKey("refund_count") ? int.Parse(dic["refund_count"]) : (int?)null,
                        result_code = dic.ContainsKey("result_code") ? dic["result_code"] : null,
                        err_code = dic.ContainsKey("err_code") ? dic["err_code"] : null,
                        err_code_des = dic.ContainsKey("err_code_des") ? dic["err_code_des"] : null,
                        init_response_xml = response.ToString(),
                        init_time = CommonHelper.GetUnixTimeNow(),
                        operation_flag = 2
                    };
                    #endregion
                    //保存refund结果
                    await repo.SaveWxRefundCallBackAsync(refund);

                    //根据结果更改状态
                    if (dic.ContainsKey("return_code"))
                    {
                        var return_code = dic["return_code"].ToUpper();

                        //通信正常
                        if (return_code.Equals("SUCCESS"))
                        {
                            //退款成功
                            if (dic.ContainsKey("result_code") && dic["result_code"].ToUpper().Equals("SUCCESS"))
                            {
                                //将订单状态改成已退款,如果otn不为空，则为正确退款。
                                if (!string.IsNullOrEmpty(refund?.out_trade_no))
                                {
                                    var order = await repo.OrderGetByOutTradeNoAsync(refund.out_trade_no);
                                    if (order != null)
                                    {
                                        order.status = (int)EOrderStatus.已退款;
                                        if (await repo.OrderUpDateAsync(order))
                                        {
                                            //退款之后扣除核销员的佣金
                                            var distribution = repo.GetDistributionByOid(order.oid);
                                            if (distribution != null && distribution.isptsucceed == 1)
                                            {
                                                int amount = distribution.commission;
                                                int userCommission = repo.GetWriteOfferCommission(order.buyer);
                                                var resWriteOffer = repo.addWriteOfferCommission(order.buyer, order.mid, -amount);
                                                if (resWriteOffer != null)
                                                {
                                                    Distribution dis = new Distribution();
                                                    dis.last_commission = userCommission;
                                                    dis.commission = amount;
                                                    dis.finally_commission = userCommission - amount;
                                                    dis.mid = order.mid;
                                                    dis.oid = Guid.NewGuid();
                                                    dis.gid = Guid.Empty;
                                                    dis.buyer = order.buyer;
                                                    dis.sharer = distribution.sharer;
                                                    dis.isptsucceed = 1;
                                                    dis.lastupdatetime = CommonHelper.GetUnixTimeNow();
                                                    dis.sourcetype = (int)EDisSourcetype.佣金结算;
                                                    dis.remark = order.o_no;
                                                    bool res = await repo.AddDistributionAsync(dis);
                                                }
                                            }
                                            return Tuple.Create(true, $"订单{refund.out_trade_no}退款成功!");
                                        }
                                    }
                                }
                                return Tuple.Create(true, $"订单{refund.out_trade_no}退款成功!，但订单状态修改失败。");
                            }
                            //退款失败，且没有成功退款的记录。
                            if (dic.ContainsKey("result_code") && dic["result_code"].ToUpper().Equals("FAIL") && await repo.RefundIsNeedAsnyc(refund.out_trade_no))
                            {
                                return Tuple.Create(false, $"退款失败了！result_code:{dic["result_code"]};error code:{dic["err_code"]};原因：{dic["err_code_des"]}");
                            }
                        }
                        return Tuple.Create(false, $"退款失败!returncode:{return_code},return_msg:{refund.return_msg}");
                    }
                }//using
            }//dic
            return Tuple.Create(false, "退款失败!微信服务器无返回XML");
        }
        #endregion

        #region prePay

        public static async Task<bool> SavePrepayResultAsync(string appid, string mch_id, string out_trade_no, int totalFee, XDocument xml)
        {
            var dic = ParseWxResponseXml(xml);
            if (dic == null)
                return false;
            using (var repo = new BizRepository())
            {
                WXPrePay prePay = new WXPrePay();
                prePay.id = Guid.NewGuid();
                prePay.mch_id = mch_id;
                prePay.out_trade_no = out_trade_no;
                prePay.total_fee = totalFee;
                prePay.return_code = dic["return_code"];
                prePay.return_msg = dic["return_msg"];
                prePay.result_code = dic.ContainsKey("result_code") ? dic["result_code"] : null;
                prePay.err_code = dic.ContainsKey("err_code") ? dic["err_code"] : null;
                prePay.err_code_des = dic.ContainsKey("err_code_des") ? dic["err_code_des"] : null;
                prePay.response_xml = xml.ToString();
                prePay.appid = appid;
                prePay.timestamp = CommonHelper.GetUnixTimeNow();

                return await repo.SaveWxPrepayAsync(prePay);
            }
        }

        public static bool SavePrepayResult(string appid, string mch_id, string out_trade_no, int totalFee, XDocument xml)
        {
            var dic = ParseWxResponseXml(xml);
            if (dic == null)
                return false;
            using (var repo = new BizRepository())
            {
                WXPrePay prePay = new WXPrePay();
                prePay.id = Guid.NewGuid();
                prePay.mch_id = mch_id;
                prePay.out_trade_no = out_trade_no;
                prePay.total_fee = totalFee;
                prePay.return_code = dic["return_code"];
                prePay.return_msg = dic["return_msg"];
                prePay.result_code = dic.ContainsKey("result_code") ? dic["result_code"] : null;
                prePay.err_code = dic.ContainsKey("err_code") ? dic["err_code"] : null;
                prePay.err_code_des = dic.ContainsKey("err_code_des") ? dic["err_code_des"] : null;
                prePay.response_xml = xml.ToString();
                prePay.appid = appid;
                prePay.timestamp = CommonHelper.GetUnixTimeNow();

                return repo.SaveWxPrepay(prePay);
            }
        }
        #endregion

        public static Dictionary<string, string> ParseWxResponseXml(XDocument xdoc)
        {
            if (xdoc == null)
                return null;
            Dictionary<string, string> ret = new Dictionary<string, string>();
            var root = xdoc.Element("xml");
            foreach (var node in root.Elements())
            {
                if (!string.IsNullOrEmpty(node.Name.ToString()) && !string.IsNullOrEmpty(node.Value))
                {
                    ret[node.Name.ToString()] = node.Value;
                }
            }
            return ret;
        }
    }
}
