using MD.Lib.DB.Redis;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Redis;
using MD.Model.DB.Code;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Activity;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.GroupOrder;
using MD.WeChat.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/laddergrouporder")]
    [AccessFilter]
    public class LadderGroupOrderController : ApiController
    {
        [HttpPost]
        [Route("getbyid")]
        public async Task<HttpResponseMessage> getbyid(GoParameter parameter)
        {
            if (parameter == null || parameter.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var go = await EsLadderGroupOrderManager.GetByIdAsync(parameter.goid);
            if (go == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到goid:{parameter.goid}的团订单信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            var g = await EsLadderGroupManager.GetByGidAsync(Guid.Parse(go.gid));
            if (g == null || g.PriceList == null || g.PriceList.Count == 0)
                return JsonResponseHelper.HttpRMtoJson($"没有取到goid:{parameter.goid}的团信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            var currentUser = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (currentUser == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到opeid:{parameter.openid}的用户信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            var p = await EsProductManager.GetByPidAsync(Guid.Parse(g.pid));
            if (p == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到goid:{parameter.goid}的商品信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            var index_order = await EsLadderOrderManager.GetOrderByBuyerAsync(Guid.Parse(go.gid), Guid.Parse(currentUser.Id));
            string fxUrl = MdWxSettingUpHelper.GenLadderGoDetailUrl(parameter.appid, parameter.goid);
            //是否过期
            bool isExp = g.end_time <= CommonHelper.GetUnixTimeNow();
            if (g.status != (int)EGroupStatus.已发布)
                isExp = true;
            #region 剩余时间计算
            string timeLimit = "";
            int timeLimit_Seconds = 0;
            DateTime expireDate = CommonHelper.FromUnixTime(go.expire_date);
            if (expireDate > DateTime.Now)
            {
                TimeSpan span = expireDate - DateTime.Now;
                timeLimit = span.Hours + ":" + span.Minutes + ":" + span.Seconds;
                timeLimit_Seconds = Convert.ToInt32(go.expire_date - CommonHelper.GetUnixTimeNow());
            }
            else
            {
                timeLimit = "团已经过期！";
            }
            #endregion
            #region 计算当前价并获取价格列表
            var nowUserCount = await EsLadderOrderManager.GetOrderCountByGoidAsync(Guid.Parse(go.Id));//当前【订单】参团的总人数
            List<object> PriceList = new List<object>();
            var nowPrice = 0.00;
            var isNowPrice = false;
            if (g.PriceList.Where(q => q.person_count == 1).FirstOrDefault() == null)
                g.PriceList.Add(new Model.Index.MD.LadderPrice { person_count = 1, group_price = g.origin_price });
            foreach (var pl in g.PriceList.OrderByDescending(q => q.person_count))
            {
                if (nowUserCount >= pl.person_count && isNowPrice == false)
                {
                    isNowPrice = true;
                    nowPrice = pl.group_price / 100.00;
                    PriceList.Add(new { person_count = pl.person_count, group_price = nowPrice, isNowPrice = true });
                    continue;
                }
                PriceList.Add(new { person_count = pl.person_count, group_price = pl.group_price / 100.00, isNowPrice = false });
            }
            #endregion
            #region 团成员列表
            string hxUrl = "";
            Guid oid = Guid.Empty; //如果我在团里面，返回我的oid
            int? ostatus = null;// 如果我在团里面，返回我的order状态
            var orders = await EsLadderOrderManager.GetByGoidAsnyc(parameter.goid, new List<int>() { (int)ELadderOrderStatus.已支付, (int)ELadderOrderStatus.拼团成功, (int)ELadderOrderStatus.已成团未提货 }, 1, 20);
            bool isMineIn = false;
            ArrayList userList = new ArrayList(orders.Item1);
            foreach (var o in orders.Item2)
            {
                if (Guid.Parse(o.Id).Equals(Guid.Empty) || Guid.Parse(o.buyer).Equals(Guid.Empty))
                    continue;
                var indexUser = await EsUserManager.GetByIdAsync(Guid.Parse(o.buyer));
                if (indexUser == null)
                    return JsonResponseHelper.HttpRMtoJson($"内部错误！buyer:{o.buyer}在es中不存在!", HttpStatusCode.OK, ECustomStatus.Fail);
                //判断自己是否在里面
                if (indexUser.openid.Equals(parameter.openid))
                {
                    isMineIn = true;
                    oid = Guid.Parse(o.Id);
                    ostatus = o.status;
                    hxUrl = MdWxSettingUpHelper.GenLadderWriteOffUrl(parameter.appid, Guid.Parse(o.Id));
                }
                //从redis中取出用户在微信中的信息
                var user = await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(indexUser.openid);
                if (user == null)
                    continue;
                userList.Add(new
                {
                    headpic = user.HeadImgUrl,
                    name = user.NickName,
                    openid = user.Openid
                });
            }
            #endregion
            #region 状态判断
            string banner = "";
            if (go.status == (int)ELadderGroupOrderStatus.拼团进行中)
            {
                if (go.leader.Equals(currentUser.Id)) //团长
                {
                    banner = "0"; //"开团成功";
                }
                else
                {
                    if (isMineIn)
                    {
                        banner = "2"; //"入团成功，我在里面";
                    }
                    else
                    {
                        banner = "1";//别人进来
                    }
                }
            }

            if (go.status == (int)ELadderGroupOrderStatus.拼团成功)
            {
                if (isMineIn)
                {
                    banner = "3"; //"拼团成功,我在里面";
                }
                else
                {
                    banner = "4";//别人进来
                }
            }
            #endregion
            var retObject =
                new
                {
                    gid = go.gid,
                    original_price = (float)go.price / 100,
                    advertise_pic_url = g.pic,
                    title = g.title,
                    standard = p.standard,
                    inventory = g.product_quotacount,
                    wtg = g.waytoget.Value,
                    timeLimit = timeLimit,
                    timeLimit_Seconds = timeLimit_Seconds,
                    userList = userList,
                    usercount = nowUserCount,
                    banner = banner,
                    maxUserCount = g.PriceList.Max(q => q.person_count),//最低价格人数(最大人数)
                    PriceList = PriceList,
                    nowGroupPrice = nowPrice,
                    fxUrl = fxUrl,
                    heUrl = hxUrl,
                    gostatus = go.status,
                    end_time = g.end_time,//提货截止日期
                    isExp= isExp,//是否过期

                    isMineIn = isMineIn,//我是否在此团
                    oid = oid,//如果我在里面，则返回我的oid
                    orderstatus = ostatus,//如果我在里面，则返回我的ostatus

                    isJoin = index_order != null,//我是否参加过该团
                    myselfgoid = index_order?.goid,//如果我参加过，返回我自己的goid
                    myselfoid= index_order?.Id//如果我参加过，返回我自己的oid
                };
            return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
        }
        [HttpPost]
        [Route("getptIng")]
        public async Task<HttpResponseMessage> getgrouporderbygid(ladderParameter parameter)
        {
            try
            {
                if (parameter == null || parameter.gid.Equals(Guid.Empty) || parameter.pageSize <= 0 || parameter.pageIndex <= 0)
                {
                    return JsonResponseHelper.HttpRMtoJson($"parameter error!gid:{parameter.gid},pageSize:{parameter.pageSize},pageIndex:{parameter.pageIndex}", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                //获取拼团进行中的团gidlist
                var gorderList = await EsLadderGroupOrderManager.GetGroupOrderAsync(parameter.gid, (int)ELadderGroupOrderStatus.拼团进行中, parameter.pageIndex, parameter.pageSize);
                int totalPage = MdWxSettingUpHelper.GetTotalPages(gorderList.Item1);
                //根据guidList查询该团信息
                List<object> obj = new List<object>();
                foreach (var grouporder in gorderList.Item2)
                {
                    var group = await EsLadderGroupManager.GetByGidAsync(Guid.Parse(grouporder.gid));//获取该团信息
                    var user = await EsUserManager.GetByIdAsync(Guid.Parse(grouporder.leader));//获取团长信息
                    UserInfoRedis userRedis = await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(user.openid);//获取团长头像
                    var userCount = await EsLadderOrderManager.GetOrderCountByGoidAsync(Guid.Parse(grouporder.Id));

                    //剩余时间计算
                    string timeLimit = "";
                    int timeLimit_Seconds = 0;
                    DateTime expireDate = CommonHelper.FromUnixTime(grouporder.expire_date);
                    if (expireDate > DateTime.Now)
                    {
                        TimeSpan span = expireDate - DateTime.Now;
                        timeLimit = span.Hours + ":" + span.Minutes + ":" + span.Seconds;
                        timeLimit_Seconds = Convert.ToInt32(grouporder.expire_date - CommonHelper.GetUnixTimeNow());
                    }
                    else
                    {
                        timeLimit = "团已经过期！";
                    }
                    obj.Add(
                        new
                        {
                            goid = grouporder.Id,
                            grouporder.gid,
                            user.name,
                            userRedis.HeadImgUrl,
                            usercount = userCount,//参团总人数
                            timeLimit = timeLimit,//结束剩余时间（小时：分：秒）
                            timeLimit_Seconds = timeLimit_Seconds,//结束剩余时间总秒数
                            nowGoPrice = grouporder.go_price/100.00,
                            maxUserCount = group.PriceList.Max(q => q.person_count),//最低价格人数(最大人数)
                        }
                    );
                }
                return JsonResponseHelper.HttpRMtoJson((new { totalPage = totalPage, glist = obj }), HttpStatusCode.OK, ECustomStatus.Success);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(WechatOrderController), ex);
                return JsonResponseHelper.HttpRMtoJson("ERROR", HttpStatusCode.OK, ECustomStatus.Fail);
            }
        }
    }
}