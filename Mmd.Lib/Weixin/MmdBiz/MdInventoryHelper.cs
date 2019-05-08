using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.MQ.MD;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Pay;
using MD.Lib.Weixin.Robot;
using MD.Lib.Weixin.Vector.Vectors;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.MQ.MD;
using MD.Model.Redis.RedisObjects.WeChat.Biz;
using MD.Model.DB.Activity;
using MD.Lib.Util;

namespace MD.Lib.Weixin.MmdBiz
{
    public enum EWxProcessResultCode
    {
        Success = 0,
        /// <summary>
        /// 参团人数已经满了但是多人付款，必须要退款的情况。
        /// </summary>
        CtRaceError = 1,
    }

    public static class MdInventoryHelper
    {
        //static readonly object _InventoryLock = new object(); 
        /// <summary>
        /// 获取库存数
        /// </summary>
        /// <param name="gid"></param>
        /// <returns></returns>
        private static int? GetInventory(Guid gid)
        {
            try
            {
                int? count = 0;
                //lock (_InventoryLock)
                //{
                using (var repo = new BizRepository())
                {
                    count = repo.GroupGetInventoryCount(gid);
                    //if (count == 0)
                    //{
                    //    BatchRefundCausedBy0Invertory(gid);
                    //}
                }
                //}
                return count;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MdInventoryHelper), ex);
            }
        }

        /// <summary>
        /// 支付成功，设置状态成功，后执行。
        /// Go----开团失败
        /// G-----库存空,已结束
        /// </summary>
        public static async void BatchRefundCausedBy0InvertoryAsync(string otn)
        {
            MDLogger.LogInfoAsync(typeof(MdInventoryHelper), $"BatchRefundCausedBy0InvertoryAsync,开始处理otn:{otn}");
            try
            {
                using (var repo = new BizRepository())
                {
                    var order = await repo.OrderGetByOutTradeNoAsync(otn);
                    if (order == null) return;
                    var go = await repo.GroupOrderGet(order.goid);
                    if (go == null) return;
                    var group = await repo.GroupGetGroupById(go.gid);

                    //支付成功后的统计更新
                    RedisMerchantStatisticsOp.AfterPaySuccessAsync(order.mid.ToString(), order.actual_pay.Value, order.waytoget.Value);

                    //团的库存大于等于成团人数时，则继续。
                    if (group == null || group.product_quota >= group.person_quota) return;

                    //库存用完了----
                    //更改group的状态
                    group.status = (int)EGroupStatus.已结束;
                    group.group_end_time = CommonHelper.GetUnixTimeNow();
                    repo.GroupUpdate(group);

                    //找出为成团的grouporder
                    var goList = await repo.GroupOrderGetByGidAsync(group.gid, EGroupOrderStatus.拼团进行中);

                    if (goList == null || goList.Count <= 0) return;
                    ////所有需要退款的订单

                    foreach (var ggoo in goList)
                    {
                        //拼团成功则不管
                        if (!(ggoo.user_left > 0)) continue;

                        //拼团失败，则更改状态
                        FailAGroupOrder(ggoo);
                    }
                }
            }

            catch (Exception ex)
            {

                throw new MDException(typeof(MdInventoryHelper), ex);
            }
        }

        /// <summary>
        /// 使得一个团订单失败！发送退款请求！时间到期了引起。
        /// </summary>
        /// <param name="go"></param>
        public static async void FailAGroupOrder(GroupOrder go)
        {
            if (go == null || go.goid.Equals(Guid.Empty))
                return;
            try
            {
                using (var repo = new BizRepository())
                {
                    //更新团订单状态到——失败！
                    go.status = (int)EGroupOrderStatus.拼团失败;
                    if (await repo.GroupOrderUpdateAsync(go))
                    {
                        MDLogger.LogInfoAsync(typeof(MdInventoryHelper), $"FailAGroup开始,goid:{go.goid}");

                        var orderList = await repo.OrderGetByGoidAsync2(go.goid, EOrderStatus.已支付);
                        if (orderList == null || orderList.Count <= 0) return;
                        foreach (var o in orderList)
                        {
                            var mer = await repo.GetMerchantByMidAsync(o.mid);
                            if (mer == null)
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
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(MdInventoryHelper), ex);
            }
        }

        /// <summary>
        /// 支付成功后才能更改库存！！！
        /// </summary>
        public static EWxProcessResultCode ChangeInventoryAndGoStatus(string openid, GroupOrder go, string appid, string otn)
        {
            try
            {
                bool isRobot = RobotHelper.IsRobot(openid);

                //出现竞争，需要退款
                if (go.user_left <= 0 || go.status != (int)EGroupOrderStatus.拼团进行中)
                {
                    return EWxProcessResultCode.CtRaceError;
                }

                using (var repo = new BizRepository())
                {
                    repo.GroupOrderVerifyUserLeft(go.goid);
                    //1.拼团差人减少1
                    go.user_left = go.user_left - 1;

                    //1.1 拼团成功
                    if (go.user_left == 0)
                    {
                        go.status = (int)EGroupOrderStatus.拼团成功;
                        if (!isRobot)
                        {
                            RedisMerchantStatisticsOp.AfterGroupOrderOk(go.gid);
                        }
                    }


                    //2.立马保存团订单信息
                    if (!repo.GroupOrderUpdate(go))
                    {
                        throw new MDException(typeof(MdInventoryHelper),
                            new Exception($"团人员数量或者状态更新失败！go:{go.goid}，需要退款！"));
                    }


                    //2.1 团没有拼满,直接返回。
                    if (go.user_left != 0)
                    {
                        //发送时间线事件
                        if (!isRobot)
                        {
                            TimeLineVectorHelper.VectorTuanSend(openid, go.goid);
                        }
                        return EWxProcessResultCode.Success;
                    }


                    //3. 通过goid获取团信息
                    var group = repo.GroupGetGroupById_TB(go.gid);
                    if (group == null)
                    {
                        throw new MDException(typeof(MdInventoryHelper),
                            new Exception($"团:{go.gid},不存在，需要退款！"));
                    }

                    //3.1 减少库存,团满了以后，库存减少,排除订单中的机器人
                    var orderList = repo.OrderGetByGoid(go.goid);
                    if (orderList != null && orderList.Count > 0)
                    {
                        group.product_quota = group.product_quota - orderList.Count;
                        if (!repo.GroupUpdate(group))
                        {
                            //库存减少失败暂时不退款
                            MDLogger.LogErrorAsync(typeof(MdInventoryHelper),
                                new Exception($"更新团库存失败！团:{go.gid}不存在！"));
                        }
                    }

                    //4.校正订单的状态，如果拼团成功，则需要将已支付的订单跳转到后面的状态。
                    repo.GroupOrderVerifyOrderStatusAfterGoSuccess(go, group);

                    return EWxProcessResultCode.Success;
                } //using
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MdInventoryHelper), ex);
            }
        }

        /// <summary>
        /// 支付成功后，还需要再次判断是否可以减少库存！
        /// </summary>
        /// <returns></returns>
        public static bool PreChangeInventory(string otn)
        {
            using (var repo = new BizRepository())
            {
                var order = repo.OrderGetByOutTradeNo(otn);
                if (order != null)
                {
                    var go = repo.GroupOrderGet_TB(order.goid);
                    if (go != null)
                    {
                        var count = GetInventory(go.gid);
                        return count > 0;
                    }
                }
                return false;
            }
        }

        public static async Task<bool> PreChangeInventoryAsync(string otn)
        {
            using (var repo = new BizRepository())
            {
                var order = await repo.OrderGetByOutTradeNoAsync(otn);
                if (order != null)
                {
                    var go = await repo.GroupOrderGet(order.goid);
                    if (go != null)
                    {
                        var count = GetInventory(go.gid);
                        return count > 0;
                    }
                }
                return false;
            }
        }


        private static List<Order> GetOrder(Guid gid, Guid buyer)
        {
            try
            {
                using (var repo = new BizRepository())
                {
                    var order = repo.OrderGetGidAndBuyer(gid, buyer, new List<int>() { 2, 3, 4, 5, 6, 9 });
                    return order;
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MdInventoryHelper), ex);
            }
        }


        public static bool CanOpenGroup(Group group, string openid, out string message)
        {
            message = "";
            #region 判断库存
            if (group.person_quota > group.product_quota)
            {
                message = "库存不足，无法开团！";
                return false;
            }
            #endregion
            #region 判断是否操作最大购买次数
            if (group.order_limit > 0)//0表示无限制
            {
                using (var repo = new BizRepository())
                {
                    var user = repo.UserGetByOpenId(openid);
                    if (user == null)
                        throw new MDException(typeof(MdOrderBizHelper), $"openid:{openid}，未注册！");
                    var order = GetOrder(group.gid, user.uid);
                    if (order != null && order.Count() >= group.order_limit.Value) //说明已经超过限制了
                    {
                        message = "您已超过此商品的最大购买限制，无法购买！";
                        return false;
                    }
                }
            }
            #endregion
            return true;
        }

        #region 阶梯团
        public static async void SuccessLadderGroupOrder(LadderGroupOrder go)
        {
            if (go == null || go.goid.Equals(Guid.Empty))
                return;
            try
            {
                using (var acti = new ActivityRepository())
                {
                    //更新团订单状态到——成功！
                    var group = acti.GetGroupById(go.gid);
                    if (group == null || group.PriceList == null || group.PriceList.Count == 0)
                        return;
                    go.status = (int)ELadderGroupOrderStatus.拼团成功;
                    go.go_price = MdOrderBizHelper.GetGroupPrice(group, false, go.goid);
                    if (await acti.GroupOrderUpdateStatusAsync(go))//修改go状态并更新ES
                    {
                        MDLogger.LogInfoAsync(typeof(MdInventoryHelper), $"使阶梯团拼团成功,goid:{go.goid}");
                        var orderList = await acti.GetOrderByGoidAsync(go.goid, (int)ELadderOrderStatus.已支付);
                        if (orderList == null || orderList.Count <= 0) return;
                        foreach (var o in orderList)
                        {
                            o.status = (int)ELadderOrderStatus.已成团未提货;
                            if (!await acti.OrderUpDateStatusAsync(o)) //修改订单状态并且修改ES
                            {
                                MDLogger.LogErrorAsync(typeof(MdInventoryHelper), new Exception($"阶梯团订单order状态变更到已成团未提货失败！oid:{o.oid}"));
                            }
                            //发送拼团成功的模板消息
                            var obj = await MqWxTempMsgManager.GenFromLadderGroupPtSucess(o);
                            await MqWxTempMsgManager.SendMessageAsync(obj);
                        }
                    }
                    else
                    {
                        MDLogger.LogErrorAsync(typeof(MdInventoryHelper), new Exception($"阶梯团go状态变更到拼团成功失败！goid:{go.goid}"));
                    }
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(MdInventoryHelper), ex);
            }
        }
        #endregion
    }
}
