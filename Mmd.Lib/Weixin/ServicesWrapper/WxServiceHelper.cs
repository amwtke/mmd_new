using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.MQ.MD;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB;
using MD.Model.MQ.MD;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.DB.Redis.MD;
using MD.Model.Redis.RedisObjects.WeChat.Biz.Merchant;
using MD.Model.DB.Code;
using MD.Lib.Weixin.Robot;
using MD.Lib.Util;

namespace MD.Lib.Weixin.Services
{
    public static class WxServiceHelper
    {
        #region mmd 定期扫描退款中订单退款与过期的go退款 process
        /// <summary>
        /// 定期扫描退款中订单退款与过期的go退款
        /// </summary>
        public static void Md_GoExpire_RefundingOrderProcess()
        {
            string go_no = "", o_no = "";
            try
            {
                using (var repo = new BizRepository())
                {
                    //过期的grouporder
                    var list = repo.GroupOrderGetFailsByTimeLimits();
                    if (list != null && list.Count > 0)
                    {
                        foreach (var go in list)
                        {
                            MdInventoryHelper.FailAGroupOrder(go);
                            go_no = go.go_no;
                        }
                    }

                    //退款中的订单退款
                    var needRefundOrders = repo.OrderGetNeedRefund();
                    if (needRefundOrders != null && needRefundOrders.Count > 0)
                    {
                        foreach (var o in needRefundOrders)
                        {
                            var mer = repo.GetMerchantByMid(o.mid);
                            if (mer == null) continue;
                            o_no = o.o_no;
                            MqRefundManager.SendMessageAsync(new MqWxRefundObject() { appid = mer.wx_appid, out_trade_no = o.o_no });
                        }
                    }
                }
                using (var acti=new ActivityRepository())
                {
                    //过期的laddergrouporder,全部改为拼团成功
                    var list = acti.GroupOrderGetFailsByTimeLimits();
                    foreach (var go in list)
                    {
                        MdInventoryHelper.SuccessLadderGroupOrder(go);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(WxServiceHelper), new Exception($"go_no:{go_no},o_no:{o_no}" + ex.Message));
            }
        }
        /// <summary>
        /// 定期扫描退款失败的订单，再重新退款
        /// </summary>
        public static void Md_refundFailProcess()
        {
            try
            {
                using (var repo = new BizRepository())
                {
                    //查询出退款失败的订单
                    var otnList = repo.OrderGetByStatus_TB(new List<int>() { (int)EOrderStatus.退款失败 });
                    if (otnList != null && otnList.Count > 0)
                    {
                        //退款失败的订单，再重新退款
                        foreach (var order in otnList)
                        {
                            var user = EsUserManager.GetById(order.buyer);
                            if (user == null) continue;
                            var mer = repo.GetMerchantByAppid(user.wx_appid);
                            if (mer == null) continue;
                            var wxrefund = repo.WXRefundByOtn(order.o_no);
                            if (wxrefund == null) continue;
                            MqRefundManager.SendMessageAsync(new MqWxRefundObject() { appid = wxrefund.appid, out_trade_no = wxrefund.out_trade_no });
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(WxServiceHelper), new Exception("定期扫描退款失败的订单失败,方法名：Md_refundFailProcess" + ex.Message));
            }
        }

        #endregion

        #region wxPayCallback process

        public static void Md_PrcessWxPayCallBack(WXPayResult env)
        {
            if (!string.IsNullOrEmpty(env?.appid) && !string.IsNullOrEmpty(env.out_trade_no) && !string.IsNullOrEmpty(env.openid))
            {
                try
                {
                    //判断是否重复接收了相同的微信回调消息
                    if (IsWxPayResultDuplicated(env))
                    {
                        MDLogger.LogInfoAsync(typeof(WxServiceHelper), $"!某个微信支付回调重复发送:otn:{env.out_trade_no},openid:{env.openid},appid:{env.appid}，result_code:{env.result_code},mch_id:{env.mch_id},transaction_id:{env.transaction_id}");
                        return;
                    }


                    MDLogger.LogInfoAsync(typeof(WxServiceHelper), $"!开始处理新的微信支付回调:otn:{env.out_trade_no},openid:{env.openid},appid:{env.appid}");

                    using (var repo = new BizRepository())
                    {
                        //支付成功,而且库存还是可以减少
                        if (env.result_code.Equals("SUCCESS") && MdInventoryHelper.PreChangeInventory(env.out_trade_no))
                        {
                            //存入db
                            var flag = repo.SaveWxCallBack(env);
                            if (flag)
                            {
                                //TODO Redis save

                                //更改团订单与订单的状态以及库存处理
                                flag = MdOrderBizHelper.OrderPayCallbackProcess(env.out_trade_no, env.appid, env.openid);
                                if (flag)
                                {
                                    //MDLogger.LogInfoAsync(typeof(WxServiceHelper), $"service1，微信支付回调成功！out trade no:{env.out_trade_no},openid:{env.openid}!");
                                    MdInventoryHelper.BatchRefundCausedBy0InvertoryAsync(env.out_trade_no);
                                    //发送订单支付成功的模板消息(需要排除机器人)
                                    bool isRobot = RobotHelper.IsRobot(env.appid);
                                    if (!isRobot)
                                    {
                                        var order = repo.OrderGetByOutTradeNo(env.out_trade_no);
                                        var obj = MqWxTempMsgManager.GenFromPaySuccess(order, env.appid, env.openid);
                                        MqWxTempMsgManager.SendMessage(obj);
                                    }
                                }
                                else
                                {
                                    MDLogger.LogInfoAsync(typeof(WxServiceHelper), $"service1，支付回调处理异常！更改订单状态或者库存状态失败！马上发起退款！out trade no:{env.out_trade_no},openid:{env.openid}!");
                                    MqRefundManager.SendMessageAsync(new MqWxRefundObject()
                                    {
                                        appid = env.appid,
                                        out_trade_no = env.out_trade_no
                                    });
                                }
                            }
                            else
                            {
                                MDLogger.LogInfoAsync(typeof(WxServiceHelper), $"service1，支付回调处理异常！存wxpayresult失败！马上发起退款！out trade no:{env.out_trade_no},openid:{env.openid}!");
                                MqRefundManager.SendMessageAsync(new MqWxRefundObject()
                                {
                                    appid = env.appid,
                                    out_trade_no = env.out_trade_no
                                });
                            }
                        }
                        else
                        {
                            //支付失败
                            MDLogger.LogInfoAsync(typeof(WxServiceHelper), $"service1，支付失败或者库存没有了！out trade no:{env.out_trade_no},openid:{env.openid}，result_code:{env.result_code},err_code:{env.err_code}");
                            MqRefundManager.SendMessageAsync(new MqWxRefundObject()
                            {
                                appid = env.appid,
                                out_trade_no = env.out_trade_no
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MDLogger.LogErrorAsync(typeof(WxServiceHelper), ex);
                    MDLogger.LogInfoAsync(typeof(WxServiceHelper), $"service1，支付回调处理异常！马上发起退款！out trade no:{env.out_trade_no},openid:{env.openid}!");
                    MqRefundManager.SendMessageAsync(new MqWxRefundObject()
                    {
                        appid = env.appid,
                        out_trade_no = env.out_trade_no
                    });
                }
            }
            else
            {
                MDLogger.LogErrorAsync(typeof(WxServiceHelper), new Exception($"支付回调失败！反序列化问题！appid或者otn,openid为空！servicLog:appid{env?.appid},out trade no:{env?.out_trade_no},openid:{env?.openid}"));
            }
        }
        #endregion

        #region 扫描将要结束的团,发送模板消息
        /// <summary>
        /// 扫描将要结束的团,发送模板消息
        /// </summary>
        /// <param name="timeEndHours">团将要结束的时间，单位：小时</param>
        /// <param name="timeInterval">扫描时间间隔，单位：分钟</param>
        public static void Md_GroupRemindProcess(int timeEndHours, int timeInterval)
        {
            try
            {
                using (var repo = new BizRepository())
                {
                    //timeEndHours小时后将要结束的grouporder
                    var list = repo.GroupOrdersGetByTime(timeEndHours, timeInterval);
                    if (list != null && list.Count > 0)
                    {
                        //取 商户appid，团购商品，剩余拼团时间，剩余拼团人数
                        foreach (var go in list)
                        {
                            //每个团订单对应的个人订单集合
                            //List<Order> listOrders = repo.OrderGetByGoidAsync2(go.goid).Result;
                            List<Order> listOrders = repo.OrderGetByGoid(go.goid);
                            if (listOrders != null && listOrders.Count > 0)
                            {
                                for (int i = 0; i < listOrders.Count; i++)
                                {
                                    Order o = listOrders[i];
                                    MerchantRedis mer = RedisMerchantOp.GetByMid(o.mid);
                                    //Merchant mer = repo.GetMerchantByMid(o.mid);
                                    string openid = EsUserManager.GetById(o.buyer)?.openid;
                                    string productName = EsProductManager.GetByPidAsync(go.pid).Result?.name;
                                    var obj = MqWxTempMsgManager.GenGroupRemind(mer?.wx_appid, openid, o.goid, productName, timeEndHours, go.user_left.Value);
                                    MqWxTempMsgManager.SendMessage(obj);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(WxServiceHelper), ex);
            }
        }
        #endregion

        #region 扫描已经结束的抽奖团，进行抽奖处理
        /// <summary>
        /// 扫描已经结束的抽奖团
        /// </summary>
        public static void Md_GroupLottery_Process()
        {
            try
            {
                using (var repo = new BizRepository())
                {
                    var attrepo = new AttRepository();
                    //1.获取已经结束且未处理的Group集合,取中奖人数
                    List<Group> listGroup = repo.luckyGroupGet();
                    Random ran = new Random();
                    if (listGroup != null && listGroup.Count > 0)
                    {
                        for (int i = 0; i < listGroup.Count; i++)
                        {
                            Guid gid = listGroup[i].gid;
                            Guid pid = listGroup[i].pid;
                            string groupName = listGroup[i].title;
                            //MerchantRedis mer = RedisMerchantOp.GetByMid(listGroup[i].mid);
                            Merchant mer = repo.GetMerchantByMid(listGroup[i].mid);
                            var lucky_count = AttHelper.GetValue(gid, EAttTables.Group.ToString(), EGroupAtt.lucky_count.ToString());
                            //该团活动的中奖人数
                            int lotteryUserCount = Convert.ToInt32(lucky_count);
                            //1.1获取属于这个抽奖团且组团成功的GroupOrder集合
                            List<Guid> listGuid = repo.GroupOrderGetByGidRetunGuidAsync(gid, EGroupOrderStatus.拼团成功).Result;
                            List<GroupOrder> listGroupFail = repo.GroupOrderGetByGidAsync(gid,EGroupOrderStatus.拼团进行中).Result;
                            #region 处理拼团进行中
                            foreach (var go in listGroupFail)
                            {
                                //拼团失败的订单发送到退款队列
                                //更新团订单状态到——失败！
                                go.status = (int)EGroupOrderStatus.拼团失败;
                                if (repo.GroupOrderUpdate(go))
                                {
                                    var orderList = repo.OrderGetByGoid2(go.goid, EOrderStatus.已支付);
                                    if (orderList == null || orderList.Count <= 0) continue;
                                    foreach (var o in orderList)
                                    {
                                        if (mer == null || RobotHelper.RobotMid.Equals(mer.mid))//找不到商家直接返回，防止机器人
                                            continue;
                                        //发送到refund队列处理
                                        MqRefundManager.SendMessageAsync(new MqWxRefundObject()
                                        {
                                            appid = mer.wx_appid,
                                            out_trade_no = o.o_no
                                        });
                                    }
                                }
                                else
                                {
                                    MDLogger.LogErrorAsync(typeof(MdInventoryHelper),
                                        new Exception($"FailAGroupOrder中团变更到：拼团失败，失败！goid:{go.goid}"));
                                }
                                
                            }
                            #endregion
                            if (listGuid.Count <= 0)
                            {
                                attrepo.lucky_statusAddOrUpdateAsync(gid, (int)EGroupLuckyStatus.已开奖);
                                continue;
                            }
                            #region 处理拼团成功
                            List<Order> listOrder = repo.OrderGetByGoidAsync2(listGuid,new List<int>(){ (int)EOrderStatus.已成团未提货, (int)EOrderStatus.已成团未发货 }).Result;
                            //参团的总人数
                            int joinUserCount = listOrder.Count;
                            bool isAllLucky = false;
                            List<int> listLuckyNumber = new List<int>();
                            if (lotteryUserCount >= joinUserCount)
                            {
                                isAllLucky = true;
                            }
                            else
                            {
                                listLuckyNumber = CommonHelper.GetRandomList(0, joinUserCount, lotteryUserCount);
                            }
                            for (int j = 0; j < joinUserCount; j++)
                            {
                                Order o = listOrder[j];
                                if (mer == null||RobotHelper.RobotMid.Equals(mer.mid))//找不到商家直接返回，防止机器人
                                    continue;
                                string openid = EsUserManager.GetById(o.buyer)?.openid;
                                //string productName = EsProductManager.GetByPid(pid)?.name;
                                bool isLucky = false;
                                
                                if (isAllLucky || listLuckyNumber.Contains(j))    //中奖订单修改状态为中奖
                                {
                                    isLucky = true;
                                    bool updateRes = attrepo.order_luckyStatusAddOrUpdateAsync(o.oid, (int)EOrderLuckyStatus.已中奖);
                                    //扣除商家剩余订单数量 2016-11-12 10:32
                                    repo.MerchantOrderConsumeAsnyc(o.mid, EBizType.DD.ToString(), 1, o.goid);
                                }
                                else   //未中奖订单修改状态为未中奖，发到退款订单集合
                                {
                                    bool flag = attrepo.order_luckyStatusAddOrUpdateAsync(o.oid, (int)EOrderLuckyStatus.未中奖);
                                    if (flag)//修改成功后才能退款
                                    {
                                        //退款
                                        MqRefundManager.SendMessageAsync(new MqWxRefundObject() { appid = mer.wx_appid, out_trade_no = o.o_no });
                                    }
                                }
                                //发到模板消息队列
                                string time = CommonHelper.FromUnixTime(Convert.ToDouble(listGroup[i].lucky_endTime)).ToString("yyyy年M月d日 HH:mm");
                                var obj = MqWxTempMsgManager.GenLotteryResult(mer?.wx_appid, openid, o.oid, groupName, time, isLucky);
                                MqWxTempMsgManager.SendMessage(obj);
                            }
                            #endregion
                            //3修改团活动为已开奖
                            attrepo.lucky_statusAddOrUpdateAsync(gid, (int)EGroupLuckyStatus.已开奖);
                        }
                    }
                    else
                    {
                        MDLogger.LogInfoAsync(typeof(WxServiceHelper), "没有未处理的抽奖团");
                    }
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogError(typeof(WxServiceHelper), ex);
            }
        }
        #endregion

        public static string GetWxPayQueueName(string queueName, int i)
        {
            string ret = queueName + "-" + i;
            return ret;
        }

        private static bool IsWxPayResultDuplicated(WXPayResult obj)
        {
            using (var repo = new BizRepository())
            {
                return repo.WxPayCallBackIsExits(obj);
            }
        }
    }
}
