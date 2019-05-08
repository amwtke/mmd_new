using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;
using MD.Model.DB.Code;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.Wechat.Controllers.WechatApi.Parameters.biz;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Activity;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Group;
using MD.WeChat.Filters;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/laddergroup")]
    [AccessFilter]
    public class LadderGroupController : ApiController
    {
        [HttpPost]
        [Route("getgroups")]
        public async Task<HttpResponseMessage> GetGroups(BaseParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid) || parameter.pageIndex <= 0)
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!mid:{parameter.mid},opid:{parameter.openid},pageindex:{parameter.pageIndex}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            int pageSize = MdWxSettingUpHelper.GetPageSize();

            var ret = await EsLadderGroupManager.GetBymidAsync(parameter.mid, new List<int>() { (int)ELadderGroupStatus.已发布 }, parameter.pageIndex, pageSize);
            if (ret == null)
            {
                return JsonResponseHelper.HttpRMtoJson($"没有取到mid:{parameter.mid}的团信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            //拼json
            List<object> groupList = new List<object>();
            int totalPage = MdWxSettingUpHelper.GetTotalPages(ret.Item1);
            var fxUrl = MdWxSettingUpHelper.GenLadderGroupListUrl(parameter.appid);
            foreach (var ig in ret.Item2)
            {
                Guid gid = Guid.Parse(ig.Id);
                //商品原价格
                var indexp = await EsProductManager.GetByPidAsync(Guid.Parse(ig.pid));
                if (indexp == null)
                {
                    return JsonResponseHelper.HttpRMtoJson($"没有取到pid:{ig.pid}的商品信息！", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                //已售
                var soldCount = ig.product_count - ig.product_quotacount >= 0 ? ig.product_count - ig.product_quotacount : 0;
                //总参团人数
                var ctCount = await EsLadderOrderManager.GetOrderCountByGidAsync(gid);
                //团最低价格
                var minPrice = ig.PriceList.Min(p => p.group_price);
                double nowTime = CommonHelper.GetUnixTimeNow();
                groupList.Add(
                    new
                    {
                        href = ig.pic,
                        ibaoyou = ig.waytoget,
                        isellout = ig.product_quotacount == 0,
                        isexp_time = nowTime > ig.end_time,
                        brief = ig.title,
                        price = ig.PriceList.Select(p => new { person_count = p.person_count, group_price = p.group_price / 100.00 }),
                        gid = ig.Id.ToString(),
                        create_time = ig.last_update_time,
                        productprice = (float)indexp.price / 100,
                        category = indexp.category,
                        inventory = ig.product_count,//库存(不变的)
                        soldCount = soldCount,//已售
                        ctCount = ctCount,//总参团人数
                        minPrice = minPrice/100.00,//最低价格
                    });
            }
            return JsonResponseHelper.HttpRMtoJson(new { fxUrl = fxUrl, totalPage = totalPage, glist = groupList }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getdetail")]
        public async Task<HttpResponseMessage> GetDetail(ladderParameter parameter)
        {
            if (parameter == null || parameter.gid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!gid:{parameter.gid},appid:{parameter.appid},openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var indexg = await EsLadderGroupManager.GetByGidAsync(parameter.gid);
            if (indexg == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到gid:{parameter.gid}的团信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            var indexp = await EsProductManager.GetByPidAsync(Guid.Parse(indexg.pid));
            if (indexp == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到pid:{indexg.pid}的商品信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            //该团的访问量
            var tuple = await EsBizLogStatistics.SearchBizViewAsnyc(ELogBizModuleType.GidView, parameter.gid, Guid.Empty);
            //图片列表
            List<string> imgList = new List<string>() { indexp.advertise_pic_1, indexp.advertise_pic_2, indexp.advertise_pic_3 };
            //已售
            var soldCount = indexg.product_count - indexg.product_quotacount >= 0 ? indexg.product_count - indexg.product_quotacount : 0;
            //html描述
            string html = HttpUtility.HtmlDecode(indexp.description).Replace("\"", "\'");
            //是否售罄 
            bool isSaledOut = indexg.product_quotacount <= 0;
            //是否过期
            bool isExp = indexg.end_time <= CommonHelper.GetUnixTimeNow();
            if (indexg.status != (int)EGroupStatus.已发布)
                isExp = true;
            //是否加入过该团
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            if (!user.wx_appid.Equals(parameter.appid))
                return JsonResponseHelper.HttpRMtoJson($"用户与商家匹配失败，opid:{parameter.openid},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var index_order = await EsLadderOrderManager.GetOrderByBuyerAsync(parameter.gid, Guid.Parse(user.Id));
            var fxUrl = MdWxSettingUpHelper.GenLadderGroupDetailUrl(parameter.appid, parameter.gid);
            double nowTime = CommonHelper.GetUnixTimeNow();
            //团价格
            if (indexg.PriceList.Where(q => q.person_count == 1).FirstOrDefault() == null)
                indexg.PriceList.Add(new Model.Index.MD.LadderPrice { person_count = 1, group_price = indexg.origin_price });

            var retObject =
                new
                {
                    imgList = imgList,
                    title = indexg.title,
                    description = indexg.description,
                    saled = soldCount,
                    inventory = indexg.product_quotacount,
                    originalPrice = (float)indexg.origin_price / 100,
                    groupPrice = indexg.PriceList.Select(p => new { person_count = p.person_count, group_price = p.group_price / 100.00 }).OrderByDescending(p=>p.person_count),
                    details = html,
                    getWay = indexg.waytoget,
                    soldOut = isSaledOut,
                    isExp= isExp,
                    isJoin = index_order != null,
                    goid = index_order?.goid,
                    thumbnail = indexg.pic,
                    pid = indexg.pid,
                    standard = indexp.standard,
                    fxUrl = fxUrl,
                    minPrice = indexg.PriceList.Min(p => p.group_price) / 100.00,
                    gidView = tuple.Item1,
                };
            EsBizLogStatistics.AddGidBizViewLog(parameter.gid.ToString(),user.openid,Guid.Parse(user.Id));
            return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("kt")]
        public async Task<HttpResponseMessage> kt(KtParameter parameter)
        {
            if (parameter == null || parameter.gid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!gid:{parameter.gid},openid:{parameter.openid},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                using (var acti = new ActivityRepository())
                {
                    try
                    {
                        //验证用户是否关注公众号
                        if (!await CheckUserSub(parameter.appid, parameter.openid, parameter.gid, "ladderkt"))
                        {
                            return JsonResponseHelper.HttpRMtoJson("UserNotSubscribe",
                                HttpStatusCode.OK, ECustomStatus.UserNotSubscribe);
                        }
                        var mer = await repo.GetMerchantByAppidAsync(parameter.appid);
                        if (mer == null || string.IsNullOrEmpty(mer.wx_appid) || string.IsNullOrEmpty(mer.wx_mch_id))
                        {
                            return JsonResponseHelper.HttpRMtoJson("mer is null!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                        }
                        //获取团信息
                        var group = await acti.GetGroupByIdAsync(parameter.gid);
                        if (group == null)
                            return JsonResponseHelper.HttpRMtoJson("es LadderGroup is null!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                        if (!group.mid.Equals(mer.mid))
                            return JsonResponseHelper.HttpRMtoJson("该团与商家不匹配!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);

                        var currUser = await EsUserManager.GetByOpenIdAsync(parameter.openid);
                        if (currUser == null)
                            return JsonResponseHelper.HttpRMtoJson($"当前用户不存在，请重试！", HttpStatusCode.OK, ECustomStatus.Fail);
                        var index_order = await EsLadderOrderManager.GetOrderByBuyerAsync(parameter.gid,Guid.Parse(currUser.Id));
                        if (index_order != null)
                            return JsonResponseHelper.HttpRMtoJson($"您已加入该团，无法再重新开团！", HttpStatusCode.OK, ECustomStatus.Fail);

                        //判断库存（支付成功后减少库存，这里不做锁定，只要有货就能开团）
                        //如果在支付后发现库存被抢光，则退款并提示。
                        if (group.product_quotacount <= 0)
                            return JsonResponseHelper.HttpRMtoJson("库存不足", HttpStatusCode.OK, ECustomStatus.Fail);
                        //获取开团价格

                        //生成订单
                        // 开团：1、产生一个GroupOrder，2、产生一个Order。状态是：GroupOrder：开团中，Order-未支付。
                        // 返回的Tuple中item1-grouporder的Guid,item2为order的Guid。
                        var ktResult =
                            await
                                MdOrderBizHelper.Ladder_KtProcessAsync(group, parameter.openid);

                        if (ktResult == null)
                        {
                            return JsonResponseHelper.HttpRMtoJson("开团失败!", HttpStatusCode.InternalServerError,
                                ECustomStatus.Fail);
                        }
                        string out_trade_no = ktResult.Item2.o_no;
                        return JsonResponseHelper.HttpRMtoJson(
                            new
                            {
                                appId = parameter.appid,
                                oid = ktResult.Item2.oid,
                                goid = ktResult.Item1.goid,
                                uid = ktResult.Item2.buyer
                            }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(WechatGroupController), ex);
                        if (ex.ToString().Length > 100)
                        {
                            return JsonResponseHelper.HttpRMtoJson("服务器异常，请刷新页面然后重试！", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                        }
                        return JsonResponseHelper.HttpRMtoJson(ex.ToString(), HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                }
            }
        }

        [HttpPost]
        [Route("ct")]
        public async Task<HttpResponseMessage> ct(CtParameter parameter)
        {
            if (parameter == null || parameter.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!goid:{parameter.goid},openid:{parameter.openid},appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                using (var acti = new ActivityRepository())
                {
                    try
                    {
                        //验证用户是否关注公众号
                        if (!await CheckUserSub(parameter.appid, parameter.openid, parameter.goid, "ladderct"))
                        {
                            return JsonResponseHelper.HttpRMtoJson("UserNotSubscribe",
                                HttpStatusCode.OK, ECustomStatus.UserNotSubscribe);
                        }

                        //获取商家信息
                        var mer = await repo.GetMerchantByAppidAsync(parameter.appid);
                        if (string.IsNullOrEmpty(mer?.wx_appid) || string.IsNullOrEmpty(mer.wx_mch_id))
                        {
                            return JsonResponseHelper.HttpRMtoJson("mer is null or other errors!",
                                HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                        }
                        //获取go信息
                        var go = await acti.GroupOrderGetAsync(parameter.goid);
                        if (go == null || go.status != (int)ELadderGroupOrderStatus.拼团进行中)
                            return JsonResponseHelper.HttpRMtoJson($"团状态不对,不能参团！", HttpStatusCode.InternalServerError, ECustomStatus.Fail);

                        //获取group信息
                        var group = await acti.GetGroupByIdAsync(go.gid);
                        if (group == null || group.status != (int)ELadderGroupStatus.已发布)
                            return JsonResponseHelper.HttpRMtoJson($"group is null or 状态不是 已发布!group status:{group?.status}", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                        //判断是否已经开或参过团
                        var currUser = await EsUserManager.GetByOpenIdAsync(parameter.openid);
                        if (currUser == null)
                            return JsonResponseHelper.HttpRMtoJson($"当前用户不存在，请重试！", HttpStatusCode.OK, ECustomStatus.Fail);
                        var index_order = await EsLadderOrderManager.GetOrderByBuyerAsync(group.gid,Guid.Parse(currUser.Id));
                        if (index_order != null)
                            return JsonResponseHelper.HttpRMtoJson($"您已开团，无法参团！", HttpStatusCode.OK, ECustomStatus.Fail);

                        //判断库存
                        if (group.product_quotacount <= 0)
                            return JsonResponseHelper.HttpRMtoJson("库存不足，无法参团。", HttpStatusCode.OK, ECustomStatus.Fail);
                        //生成订单
                        // 参团：1、判断grouporder的状态与参与人数，2、产生一个Order。order-未支付。
                        // 返回的Tuple中item1-grouporder的Guid,item2为order的Guid。
                        // 修改参团价格为group.group_price，不取parameter.fee  ###2016-8-31
                        var ctResult =
                            await
                                MdOrderBizHelper.Ladder_CtProcessAsync(go, group, parameter.openid);
                        if (ctResult == null)
                        {
                            return JsonResponseHelper.HttpRMtoJson("开团失败!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                        }
                        return JsonResponseHelper.HttpRMtoJson(
                            new
                            {
                                appId = parameter.appid,
                                oid = ctResult.oid,
                                goid = go.goid,
                                uid = ctResult.buyer
                            }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                    catch (Exception ex)
                    {
                        if (ex.ToString().Length > 100)
                        {
                            return JsonResponseHelper.HttpRMtoJson("服务器异常，请刷新页面然后重试！", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                        }
                        return JsonResponseHelper.HttpRMtoJson(ex.ToString(), HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                }
            }
        }

        [HttpPost]
        [Route("verifypay")]
        public async Task<HttpResponseMessage> verifypay(ladderParameter parameter)
        {
            if (parameter == null || parameter.oid.Equals(Guid.Empty)||parameter.goid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            var retOrder = await EsLadderOrderManager.GetByIdAsync(parameter.oid);
            var retGroupOrder = await EsLadderGroupOrderManager.GetByIdAsync(parameter.goid);
            if (retOrder != null && retGroupOrder != null)
            {
                return JsonResponseHelper.HttpRMtoJson(new { isOK = true }, HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson(new { isOK = false }, HttpStatusCode.OK, ECustomStatus.Success);
        }


        private async Task<bool> CheckUserSub(string appid, string openid, Guid goid, string type)
        {
            try
            {
                string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
                UserInfoJson user = await Senparc.Weixin.MP.AdvancedAPIs.UserApi.InfoAsync(at, openid);
                bool IsUserSub = user?.subscribe == 1;
                //bool IsUserSub = await RedisUserOp.IsExistOpenidAsync(openid);
                if (!IsUserSub)
                {
                    RedisUserOp.SaveTmpId(openid, goid.ToString(), type);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(WechatGroupController), new Exception("CheckUserSub获取微信用户信息失败,appid:" + appid + ",openid:" + openid + "," + ex));
                return false;
            }
        }
    }
}