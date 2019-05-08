using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Robot;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using MD.Model.DB.Activity;

namespace MD.Lib.Weixin.MmdBiz
{
    public static class MdOrderBizHelper
    {
        /// <summary>
        /// 开团：1、产生一个GroupOrder，2、产生一个Order。状态是：GroupOrder：开团中，Order-未支付。
        /// 返回的Tuple中item1-grouporder的Guid,item2为order的Guid。
        /// </summary>
        /// <param name="group">要开的团</param>
        /// <param name="openId">谁开团</param>
        /// <param name="uw">用户的提货信息</param>
        /// <param name="fee">订单实际支付金额</param>
        /// <returns></returns>
        public static async Task<Tuple<GroupOrder, Order>> KtProcessAsync(Group group, string openId, User_WriteOff uw, int fee, int waytoget, UserPost up,int post_price)
        {
            if (group == null || string.IsNullOrEmpty(openId) || waytoget >= 2)
                return null;
            if (waytoget == (int)EWayToGet.自提)
            {
                if (uw == null || uw.woid.Equals(Guid.Empty))
                    return null;
            }
            else if (waytoget == (int)EWayToGet.物流)
            {
                if (up == null || up.upid.Equals(Guid.Empty))
                    return null;
            }

            if (group.status != (int)EGroupStatus.已发布)
            {
                throw new MDException(typeof(MdOrderBizHelper), $"开团状态不符合，group:{group.gid},status:{group.status}");
            }

            try
            {
                using (var repo = new BizRepository())
                {
                    //产生GO
                    var leader = await repo.UserGetByOpenIdAsync(openId);
                    if (leader == null)
                        throw new MDException(typeof(MdOrderBizHelper), $"openid:{openId}，未注册！");
                    var go = await repo.GroupOderOpenAsync(group, leader.uid);

                    if (go != null)
                    {
                        //产生Order
                        var order = await repo.OrderCreate(go, group, leader.uid, waytoget, uw, up, fee, post_price);
                        if (order != null)
                        {
                            //将grouporder与order存入es
                            string orderKw = order.o_no + "," + group.title + "," + order.cellphone;
                            string goKw = go.go_no + "," + group.title + "," + order.cellphone;
                            if (!await EsOrderManager.AddOrUpdateAsync(EsOrderManager.GenObject(order, orderKw)) ||
                                !await EsGroupOrderManager.AddOrUpdateAsync(EsGroupOrderManager.GenObject(go, goKw)))
                            {
                                MDLogger.LogErrorAsync(typeof(MdOrderBizHelper), new Exception($"Go与Order存入es失败！order id:{order.oid},Goid:{go.goid}"));
                            }

                            return Tuple.Create(go, order);
                        }
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MdOrderBizHelper), ex);
            }
        }

        public static async Task<Order> CtProcessAsync(GroupOrder go, Group group, string openid, User_WriteOff uw, int fee, int waytoget, UserPost up,int post_price)
        {
            if (group == null || string.IsNullOrEmpty(openid) || waytoget >= 2)
                return null;
            if (waytoget == (int)EGroupWaytoget.自提)
            {
                if (uw == null || uw.woid.Equals(Guid.Empty))
                    return null;
            }
            else if (waytoget == (int)EGroupWaytoget.快递到家)
            {
                if (up == null || up.upid.Equals(Guid.Empty))
                    return null;
            }

            if (group.status != (int)EGroupStatus.已发布)
            {
                throw new MDException(typeof(MdOrderBizHelper), $"参团状态不符合，group:{group.gid},status:{group.status}");
            }
            using (var repo = new BizRepository())
            {
                try
                {
                    var buyer = await repo.UserGetByOpenIdAsync(openid);
                    if (buyer == null)
                        return null;

                    var order = await repo.OrderCreate(go, group, buyer.uid, waytoget, uw, up, fee, post_price);
                    if (order != null)
                    {
                        //将order存入es

                        string orderKw = order.o_no + "," + group.title + "," + order.cellphone;
                        var index_order = EsOrderManager.GenObject(order, orderKw);
                        if (!await EsOrderManager.AddOrUpdateAsync(index_order))
                        {
                            MDLogger.LogErrorAsync(typeof(MdOrderBizHelper), new Exception($"Order存入es失败！oid:{order.oid}"));
                        }

                        return order;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    throw new MDException(typeof(MdOrderBizHelper), ex);
                }
            }
        }

        public static async Task<Order> CtProcess_robotAsync(GroupOrder go, Group group, string openid, Guid wo, int fee)
        {
            using (var repo = new BizRepository())
            {
                try
                {
                    var buyer = await repo.UserGetByOpenIdAsync(openid);
                    if (buyer == null)
                        return null;

                    var order = await repo.OrderCreate_robot(go, group, buyer, wo, fee);
                    if (order != null)
                    {
                        //将order存入es

                        string orderKw = order.o_no + "," + group.title + "," + buyer.cell_phone;

                        if (!await EsOrderManager.AddOrUpdateAsync(EsOrderManager.GenObject(order, orderKw)))
                        {
                            MDLogger.LogErrorAsync(typeof(MdOrderBizHelper),
                                new Exception($"机器人参团失败，Order存入es失败！order id:{order.oid}"));
                        }

                        return order;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    throw new MDException(typeof(MdOrderBizHelper), ex);
                }
            }
        }


        /// <summary>
        /// order 状态到：已支付
        /// go的状态到：拼团进行中
        /// 减少库存
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <param name="appid"></param>
        /// <param name="openid"></param>
        /// <returns></returns>
        public static bool OrderPayCallbackProcess(string out_trade_no, string appid, string openid)
        {
            try
            {
                bool isRobot = RobotHelper.IsRobot(openid);
                //MDLogger.LogInfoAsync(typeof(MdOrderBizHelper), $"进入支付回调——订单处理:otn:{out_trade_no},appid:{appid},openid:{openid}");

                if (string.IsNullOrEmpty(out_trade_no) || string.IsNullOrEmpty(openid) || string.IsNullOrEmpty(appid))
                    return false;
                using (var repo = new BizRepository())
                {
                    var user = repo.UserGetByOpenId(openid);
                    if (user == null)
                        return false;
                    var mer = repo.GetMerchantByAppid(appid);
                    if (mer == null && !isRobot)//商家为空，并且不是机器人
                        return false;
                    var order = repo.OrderGetByOutTradeNo(out_trade_no);
                    if (order == null)
                        return false;

                    //MDLogger.LogInfoAsync(typeof(MdOrderBizHelper),$"进入支付回调——开始校验:otn:{out_trade_no},appid:{appid},openid:{openid},mer.mid:{mer.mid},order.mid{order.mid},user.uid:{user.uid},order.buyer:{order.buyer}");
                    //校验
                    if (isRobot || (mer.mid.Equals(order.mid) && order.buyer.Equals(user.uid)))
                    {
                        //MDLogger.LogInfoAsync(typeof(MdOrderBizHelper), $"进入支付回调——开始校验-2:otn:{out_trade_no},appid:{appid},openid:{openid}");

                        var go = repo.GroupOrderGet_TB(order.goid);
                        if (go == null)
                            return false;
                        //MDLogger.LogInfoAsync(typeof(MdOrderBizHelper), $"进入支付回调——开始修改状态:otn:{out_trade_no},appid:{appid},openid:{openid}");
                        if (go.status == (int)EGroupOrderStatus.开团中)
                        {
                            go.status = (int)EGroupOrderStatus.拼团进行中;
                            repo.GroupOrderUpdate(go);
                        }

                        order.status = (int)EOrderStatus.已支付;
                        order.paytime = CommonHelper.GetUnixTimeNow();
                        repo.OrderUpDate(order);

                        //更改库存以及团订单状态

                        var resultCode = MdInventoryHelper.ChangeInventoryAndGoStatus(openid, go, appid, out_trade_no);

                        if (resultCode == EWxProcessResultCode.Success)
                        {
                            return true;
                        }

                        if (resultCode == EWxProcessResultCode.CtRaceError)
                        {
                            MDLogger.LogInfoAsync(typeof(MdInventoryHelper), ($"团订单:{go.goid}的人数已经为0了,团订单状态:{go.status}，需要退款！"));
                        }
                    }

                    //没有通过校验，又不是robot的情况。还有就是一些需要退款的情况。
                    return false;
                }//using
            }//try
            catch (Exception ex)
            {
                throw new MDException(typeof(MdOrderBizHelper), new Exception($"OrderPayCallbackProcess方法,out_trade_no:{out_trade_no},appid:{appid},openid:{openid}" + ex.Message));
            }
        }

        #region 阶梯团开团参团
        /// <summary>
        /// 获取当前阶梯团价格
        /// </summary>
        /// <param name="lp">group的价格列表</param>
        /// <param name="isAddone">人数是否+1（如果是我要开团或参团，+1可直接获取我参团的价格）</param>
        /// <param name="goid">参团必传，开团不传</param>
        /// <returns></returns>
        public static int GetGroupPrice(LadderGroup group, bool isAddone, Guid? goid = null)
        {
            if (group == null || group.PriceList == null || group.PriceList.Count == 0)
                throw new MDException(typeof(MdOrderBizHelper), $"商家未配置团价格");
            using (var acit = new ActivityRepository())
            {
                //获取go下面的订单总数
                int person_Count = 0;
                if (goid != null && !goid.Equals(Guid.Empty))//说明是参团，获取该团下面的订单数量
                {
                    person_Count = acit.GetOrderCountByGoid(goid.Value);
                }
                if (isAddone)
                    person_Count = person_Count + 1;
                foreach (var item in group.PriceList.OrderByDescending(p => p.person_count))
                {
                    if (person_Count >= item.person_count)
                        return item.group_price;
                }
                return group.origin_price;
            }
        }
        public static async Task<Tuple<LadderGroupOrder, LadderOrder>> Ladder_KtProcessAsync(LadderGroup group, string openId)
        {
            if (group == null || string.IsNullOrEmpty(openId))
                return null;
            if (group.status != (int)ELadderGroupStatus.已发布)
            {
                throw new MDException(typeof(MdOrderBizHelper), $"开团状态不符合，group:{group.gid},status:{group.status}");
            }
            try
            {
                using (var repo = new BizRepository())
                {
                    using (var acit = new ActivityRepository())
                    {
                        //产生GO
                        var leader = await repo.UserGetByOpenIdAsync(openId);
                        if (leader == null)
                            throw new MDException(typeof(MdOrderBizHelper), $"openid:{openId}，未注册.");
                        if (!leader.mid.Equals(group.mid))
                            throw new MDException(typeof(MdOrderBizHelper), $"用户与商家不匹配.");
                        int fee = GetGroupPrice(group, true);
                        var go = await acit.GroupOderOpenAsync(group, leader.uid, fee);
                        if (go != null)
                        {
                            //产生Order
                            var order = await acit.OrderCreate(go, group, leader.uid, fee);
                            if (order != null)
                            {
                                //将go与order存入es
                                var indexgorder = EsLadderGroupOrderManager.GenObject(go);
                                var index_ladderorder = EsLadderOrderManager.GenObject(order);
                                if (indexgorder != null && index_ladderorder != null)
                                {
                                    await EsLadderGroupOrderManager.AddOrUpdateAsync(indexgorder);
                                    await EsLadderOrderManager.AddOrUpdateAsync(index_ladderorder);
                                }
                                return Tuple.Create(go, order);
                            }
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MdOrderBizHelper), ex);
            }
        }

        public static async Task<LadderOrder> Ladder_CtProcessAsync(LadderGroupOrder go, LadderGroup group, string openid)
        {
            using (var repo = new BizRepository())
            {
                using (var acti = new ActivityRepository())
                {
                    try
                    {
                        var buyer = await repo.UserGetByOpenIdAsync(openid);
                        if (buyer == null)
                            return null;
                        if (!buyer.mid.Equals(group.mid))
                            throw new MDException(typeof(MdOrderBizHelper), $"用户与商家不匹配.");
                        int fee = GetGroupPrice(group, true, go.goid);
                        var order = await acti.OrderCreate(go, group, buyer.uid, fee);
                        if (order != null)
                        {
                            //将order存入es
                            var index_ladderorder = EsLadderOrderManager.GenObject(order);
                            await EsLadderOrderManager.AddOrUpdateAsync(index_ladderorder);
                            //再更新grouporder的go_price
                            go.go_price = order.order_price;
                            await acti.GroupOrderUpdateAsync(go);
                            return order;
                        }
                        return null;
                    }
                    catch (Exception ex)
                    {
                        throw new MDException(typeof(MdOrderBizHelper), ex);
                    }
                }
            }
        }
        #endregion
    }
}
