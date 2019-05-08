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
using MD.Lib.Exceptions.Pay;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using MD.Model.Index.MD;
using MD.Wechat.Controllers.PinTuanController.Group;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.Wechat.Controllers.WechatApi.Parameters.biz;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Group;
using MD.WeChat.Filters;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Write;
using MD.Model.DB;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/writeoffer")]
    [AccessFilter]
    public class WechatWriteOfferController : ApiController
    {
        /// <summary>
        /// 核销逻辑
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("writeofferorder")]
        public async Task<HttpResponseMessage> WriteOffOrder(WriteOfferParameter parameter)
        {
            try
            {
                if (parameter == null || parameter.oid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid))
                {
                    return JsonResponseHelper.HttpRMtoJson($"parameter error!oid:{parameter.oid},openid:{parameter.openid},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                #region 核销逻辑

                using (var repo = new BizRepository())
                {
                    //查询该订单是否存在
                    var order = await repo.OrderGetByOid(parameter.oid);
                    if (order == null)
                        return JsonResponseHelper.HttpRMtoJson("该订单不存在！", HttpStatusCode.OK, ECustomStatus.Fail);

                    //验证商品状态
                    if (order.status != (int)EOrderStatus.已成团未提货)
                        return JsonResponseHelper.HttpRMtoJson("不可重复核销！", HttpStatusCode.OK, ECustomStatus.Fail);

                    var currentuser = await repo.UserGetByOpenIdAsync(parameter.openid);//获取当前用户信息
                    if (currentuser == null)
                        return JsonResponseHelper.HttpRMtoJson("核销员不存在！", HttpStatusCode.OK, ECustomStatus.Fail);

                    //验证是否核销
                    if (!order.writeoffer.Equals(Guid.Empty))
                    {
                        var user = await repo.UserGetByUidAsync(order.writeoffer);//查询这个核销员的信息

                        if (order.writeoffer.Equals(currentuser.uid))
                        {
                            return JsonResponseHelper.HttpRMtoJson("您已经核销过该商品了！", HttpStatusCode.OK, ECustomStatus.Success);
                        }
                        else
                        {
                            return JsonResponseHelper.HttpRMtoJson($"商品已经被其他核销员核销！核销员：{user.name};核销时间为:{CommonHelper.FromUnixTime(order.writeoffday.Value)}",
                                HttpStatusCode.OK, ECustomStatus.Success);
                        }
                    }

                    //验证商家与核销员
                    var mer = await repo.GetMerchantByMidAsync(order.mid);
                    if (mer == null)
                        return JsonResponseHelper.HttpRMtoJson($"订单所在商家错误！mid:{order.mid}", HttpStatusCode.OK, ECustomStatus.Success);
                    if (!mer.wx_appid.Equals(parameter.appid))
                        return JsonResponseHelper.HttpRMtoJson($"订单商家错误，appid不符！order's appid:{mer.wx_appid},而appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Success);

                    if (!await repo.WoerCanWriteOff(mer.mid, currentuser.uid))
                    {
                        return JsonResponseHelper.HttpRMtoJson($"您无权核销此订单！编号：{order.o_no}", HttpStatusCode.OK, ECustomStatus.Success);
                    }
                    var woer = await repo.WoerGetByUidAsync(currentuser.uid);
                    //完成核销
                    order.writeoffer = currentuser.uid;
                    order.writeoffday = CommonHelper.GetUnixTimeNow();
                    order.status = (int)EOrderStatus.拼团成功;
                    order.extral_info = woer.woid.ToString();
                    order.default_writeoff_point = woer.woid;
                    await repo.OrderUpDateAsync(order);
                    return JsonResponseHelper.HttpRMtoJson($"核销成功！",
                        HttpStatusCode.OK, ECustomStatus.Success);

                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResponseHelper.HttpRMtoJson(ex.ToString(), HttpStatusCode.InternalServerError,
                      ECustomStatus.Fail);
            }
        }

        /// <summary>
        /// 返回订单核销信息
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("writeoffmessage")]
        public async Task<HttpResponseMessage> WriteOffMessage(WriteOfferParameter parameter)
        {
            if (parameter == null || parameter.oid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!oid:{parameter.oid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                var order = await repo.OrderGetByOid(parameter.oid);
                if (order == null)
                    return JsonResponseHelper.HttpRMtoJson("该订单不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                if (order.status != (int)EOrderStatus.拼团成功)
                    return JsonResponseHelper.HttpRMtoJson("该订单还未核销！", HttpStatusCode.OK, ECustomStatus.Fail);
                //先根据订单gid去查询团信息
                var group = await repo.GroupGetGroupById(order.gid);
                if (group == null)
                    return JsonResponseHelper.HttpRMtoJson("该团不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                //再根据团信息查询商品信息
                var product = await repo.GetProductByPidAsync(group.pid);
                if (product == null)
                    return JsonResponseHelper.HttpRMtoJson("该商品在ES中不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                //获取收货人信息
                var user_writeoff = await repo.UserWriteoffGetByMidAndUidAsync(product.mid, order.buyer);
                if (user_writeoff == null)
                    return JsonResponseHelper.HttpRMtoJson("该用户地址不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                //获取核销员信息
                var writeoffer = await repo.UserGetByUidAsync(order.writeoffer);
                if (writeoffer == null)
                    return JsonResponseHelper.HttpRMtoJson("核销员不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                //获取核销门店信息
                var writeoffpoint = await repo.GetWOPByWoidAsync(order.default_writeoff_point);
                if (writeoffpoint == null)
                    return JsonResponseHelper.HttpRMtoJson("核销点不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                //返回提货信息
                var retObject = new
                {
                    productname = product.name,
                    standard = product.standard,
                    p_no = product.p_no,
                    getname = user_writeoff.user_name,//收货人姓名
                    gettel = user_writeoff.cellphone,//收货人电话
                    order_price = (float)order.order_price / 100,//订单金额
                    orderno = order.o_no,//订单号
                    writeMD = writeoffpoint.address,//核销门店
                    writeoffer = writeoffer.name,//核销人
                    writetime = CommonHelper.FromUnixTime(order.writeoffday.Value).ToString()//核销时间
                };
                return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        /// <summary>
        /// 返回添加核销员扫码后的核销员信息
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getwriteoffermessage")]
        public async Task<HttpResponseMessage> GetWriteOfferMessage(WriteOfferParameter parameter)
        {
            if (parameter.woid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error！woid:{parameter.woid},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var mer = await repo.GetMerchantByAppidAsync(parameter.appid);
                if (mer == null)
                    return JsonResponseHelper.HttpRMtoJson($"error!mer is null,appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);

                var writeoffpoint = await repo.GetWOPByWoidAsync(parameter.woid);
                if (writeoffpoint == null)
                    return JsonResponseHelper.HttpRMtoJson($"门店不存在,woid:{parameter.woid}", HttpStatusCode.OK, ECustomStatus.Fail);

                var currentuser = await repo.UserGetByOpenIdAsync(parameter.openid);//获取当前用户信息
                if (currentuser == null)
                    return JsonResponseHelper.HttpRMtoJson("核销员不存在！", HttpStatusCode.OK, ECustomStatus.Fail);

                var woer = await repo.WoerGetByUidAsync(currentuser.uid);//查询当前用户是否是核销员

                return JsonResponseHelper.HttpRMtoJson(new { username = currentuser.name, mername = mer.name, pointname = writeoffpoint.name, realname = woer?.realname, phone = woer?.phone }, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }
        /// <summary>
        /// 添加核销员
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("addwriteoffer")]
        public async Task<HttpResponseMessage> addWriteOffer(WriteOfferParameter parameter)
        {
            #region 添加核销员

            if (parameter.woid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid) ||
                string.IsNullOrEmpty(parameter.realname) || string.IsNullOrEmpty(parameter.phone))
                return JsonResponseHelper.HttpRMtoJson($"添加核销员参数错误！bizid:{parameter.woid},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);

            using (var repo = new BizRepository())
            {
                var mer = await repo.GetMerchantByAppidAsync(parameter.appid);
                if (mer == null)
                    return JsonResponseHelper.HttpRMtoJson($"error!mer is null,appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);

                var writeoffpoint = await repo.GetWOPByWoidAsync(parameter.woid);
                if (writeoffpoint == null)
                    return JsonResponseHelper.HttpRMtoJson($"门店不存在,woid:{parameter.woid}", HttpStatusCode.OK, ECustomStatus.Fail);

                if (!writeoffpoint.mid.Equals(mer.mid))
                    return JsonResponseHelper.HttpRMtoJson($"门店与商家不符合！", HttpStatusCode.OK, ECustomStatus.Fail);

                var currentuser = await repo.UserGetByOpenIdAsync(parameter.openid);//获取当前用户信息
                if (currentuser == null)
                    return JsonResponseHelper.HttpRMtoJson("核销员不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                var woer = await repo.WoerGetByUidAsync(currentuser.uid);//查询当前用户是否是核销员

                //新加入
                if (woer == null || woer.uid.Equals(Guid.Empty))
                {
                    WriteOffer newOne = new WriteOffer()
                    {
                        is_valid = true,
                        mid = mer.mid,
                        openid = currentuser.openid,
                        timestamp = CommonHelper.GetUnixTimeNow(),
                        uid = currentuser.uid,
                        woid = parameter.woid,
                        phone = parameter.phone,
                        realname = parameter.realname
                    };

                    if (await repo.AddWoerAsync(newOne))
                    {
                        return JsonResponseHelper.HttpRMtoJson(new { isOk = true, Message = "添加成功" }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                }

                //已经是的了(更新valid与woid字段)
                if (woer.mid.Equals(mer.mid) && woer.uid.Equals(currentuser.uid))
                {
                    //重新生效(或者重新绑定到新的wop)
                    woer.is_valid = true;
                    woer.woid = parameter.woid;
                    woer.phone = parameter.phone;
                    woer.realname = parameter.realname;
                    bool result = await repo.UpdateWoerAsync(woer);
                    return JsonResponseHelper.HttpRMtoJson(new { isOk = true, Message = "添加成功" }, HttpStatusCode.OK, ECustomStatus.Success);
                }
                else
                {
                    var mermer = await repo.GetMerchantByMidAsync(woer.mid);
                    var wopwop = await repo.GetWOPByWoidAsync(woer.woid);
                    return JsonResponseHelper.HttpRMtoJson(new { isOk = false, Message = $"{currentuser.name}您好!您已经是：{mermer.name}下{wopwop.name} 的核销员了！" }, HttpStatusCode.OK, ECustomStatus.Fail);
                    //$"{currentuser.name}您好!您已经是：{mermer.name}下{wopwop.name} 的核销员了！不能同时成为两个店的核销员！", 
                }
            }
            #endregion
        }

        #region 阶梯团核销
        [HttpPost]
        [Route("writeofferladderorder")]
        public async Task<HttpResponseMessage> WriteOffLadderOrder(WriteOfferParameter parameter)
        {
            try
            {
                if (parameter == null || parameter.oid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid))
                {
                    return JsonResponseHelper.HttpRMtoJson($"parameter error!oid:{parameter.oid},openid:{parameter.openid},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                #region 核销逻辑
                using (var repo = new BizRepository())
                {
                    using (var acti = new ActivityRepository())
                    {
                        //查询该订单是否存在
                        var order = await acti.GetOrderByOidAsync(parameter.oid);
                        if (order == null)
                            return JsonResponseHelper.HttpRMtoJson("该订单不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                        var group = await acti.GetGroupByIdAsync(order.gid);
                        if (group == null)
                            return JsonResponseHelper.HttpRMtoJson($"该团不存在gid:{order.gid}！", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (group.product_quotacount <= 0)
                            return JsonResponseHelper.HttpRMtoJson($"库存不足，无法核销！", HttpStatusCode.OK, ECustomStatus.Fail);
                        var product = await EsProductManager.GetByPidAsync(group.pid);
                        if (product == null)
                            return JsonResponseHelper.HttpRMtoJson($"该商品不存在pid:{group.pid}！", HttpStatusCode.OK, ECustomStatus.Fail);
                        var usercount = await EsLadderOrderManager.GetOrderCountByGoidAsync(order.goid);
                        var grouporder = await EsLadderGroupOrderManager.GetByIdAsync(order.goid);
                        if (grouporder == null)
                            return JsonResponseHelper.HttpRMtoJson($"grouporder不存在goid:{order.goid}！", HttpStatusCode.OK, ECustomStatus.Fail);
                        var currentuser = await repo.UserGetByOpenIdAsync(parameter.openid);//获取当前用户信息
                        if (currentuser == null)
                            return JsonResponseHelper.HttpRMtoJson("核销员不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                        var retobj = new
                        {
                            pname = product.name,
                            standard = product.standard,
                            usercount = usercount,
                            go_price = grouporder.go_price / 100.00,
                            pic = group.pic
                        };
                        //验证是否核销
                        if (!order.writeoffer.Equals(Guid.Empty) || order.status != (int)ELadderOrderStatus.已成团未提货)
                        {
                            return JsonResponseHelper.HttpRMtoJson(new { isHxOk = 2, retobj }, HttpStatusCode.OK, ECustomStatus.Success);
                        }
                        //验证商家与核销员
                        var mer = await repo.GetMerchantByMidAsync(order.mid);
                        if (mer == null)
                            return JsonResponseHelper.HttpRMtoJson($"订单所在商家错误！mid:{order.mid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (!mer.wx_appid.Equals(parameter.appid))
                            return JsonResponseHelper.HttpRMtoJson($"订单商家错误，appid不符！order's appid:{mer.wx_appid},而appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (!await repo.WoerCanWriteOff(mer.mid, currentuser.uid))
                        {
                            return JsonResponseHelper.HttpRMtoJson($"您无权核销此订单！编号：{order.o_no}", HttpStatusCode.OK, ECustomStatus.Fail);
                        }
                        var woer = await repo.WoerGetByUidAsync(currentuser.uid);
                        //完成核销
                        order.writeoffer = currentuser.uid;
                        order.writeoffday = CommonHelper.GetUnixTimeNow();
                        order.status = (int)ELadderOrderStatus.拼团成功;
                        order.writeoff_point = woer.woid;
                        var flag3 = await acti.OrderUpDateAsync(order);
                        if (flag3)
                        {
                            //扣库存，
                            var flag1 = await acti.GroupUpdateQuotacount(group);
                            //做订单配额扣减
                            var flag2 = await repo.MerchantOrderConsumeAsnyc(group.mid, EBizType.DD.ToString(), 1, order.goid);
                            if (flag1 && flag2)
                                return JsonResponseHelper.HttpRMtoJson(new { isHxOk = 1, retobj }, HttpStatusCode.OK, ECustomStatus.Success);
                        }
                        return JsonResponseHelper.HttpRMtoJson("核销失败", HttpStatusCode.OK, ECustomStatus.Fail);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResponseHelper.HttpRMtoJson(ex.ToString(), HttpStatusCode.InternalServerError, ECustomStatus.Fail);
            }
        }
        [HttpPost]
        [Route("writeoffladderordermessage")]
        public async Task<HttpResponseMessage> WriteOffLadderOrderMessage(WriteOfferParameter parameter)
        {
            if (parameter == null || parameter.oid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!oid:{parameter.oid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                using (var acti = new ActivityRepository())
                {
                    var order = await acti.GetOrderByOidAsync(parameter.oid);
                    if (order == null)
                        return JsonResponseHelper.HttpRMtoJson("该订单不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                    if (order.status != (int)EOrderStatus.拼团成功)
                        return JsonResponseHelper.HttpRMtoJson("该订单还未核销！", HttpStatusCode.OK, ECustomStatus.Fail);
                    //先根据订单gid去查询团信息
                    var group = await acti.GetGroupByIdAsync(order.gid);
                    if (group == null)
                        return JsonResponseHelper.HttpRMtoJson("该团不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                    //再根据团信息查询商品信息
                    var product = await repo.GetProductByPidAsync(group.pid);
                    if (product == null)
                        return JsonResponseHelper.HttpRMtoJson("该商品在ES中不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                    //获取核销员信息
                    var writeoffer = await repo.UserGetByUidAsync(order.writeoffer);
                    if (writeoffer == null)
                        return JsonResponseHelper.HttpRMtoJson("核销员不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                    //获取核销门店信息
                    var writeoffpoint = await repo.GetWOPByWoidAsync(order.writeoff_point);
                    if (writeoffpoint == null)
                        return JsonResponseHelper.HttpRMtoJson("核销点不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                    //返回提货信息
                    var retObject = new
                    {
                        productname = product.name,
                        standard = product.standard,
                        p_no = product.p_no,
                        order_price = (float)order.order_price / 100,//订单金额
                        orderno = order.o_no,//订单号
                        writeMD = writeoffpoint.address,//核销门店
                        writeoffer = writeoffer.name,//核销人
                        writetime = CommonHelper.FromUnixTime(order.writeoffday.Value).ToString()//核销时间
                    };
                    return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
        }
        #endregion
    }
}
