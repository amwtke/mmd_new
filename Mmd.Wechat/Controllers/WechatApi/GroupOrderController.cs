using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Redis;
using MD.Model.DB.Code;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.GroupOrder;
using MD.WeChat.Filters;
using MySql.Data.Entity;
using MD.Model.Index.MD;
using MD.Model.DB.Professional;

namespace MD.Wechat.Controllers.WechatApi.Parameters
{
    [RoutePrefix("api/grouporder")]
    [AccessFilter]
    public class GroupOrderController : ApiController
    {
        [HttpPost]
        [Route("getbyid2")]
        public async Task<HttpResponseMessage> getbyid2(GoParameter parameter)
        {
            if (parameter == null || parameter.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) ||
                parameter.uid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                var go = await EsGroupOrderManager.GetByIdAsync(parameter.goid);
                if (go == null)
                    return JsonResponseHelper.HttpRMtoJson($"没有取到goid:{parameter.goid}，的团订单信息！", HttpStatusCode.OK,
                        ECustomStatus.Fail);
                var g = await EsGroupManager.GetByGidAsync(Guid.Parse(go.gid));
                if (g == null || Guid.Parse(g.Id).Equals(Guid.Empty))
                    return JsonResponseHelper.HttpRMtoJson($"没有取到goid:{parameter.goid}，的团信息！", HttpStatusCode.OK,
                        ECustomStatus.Fail);
                var currentUser = await EsUserManager.GetByOpenIdAsync(parameter.openid);
                if (currentUser == null)
                    return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}，的用户信息！", HttpStatusCode.OK, ECustomStatus.Fail);

                string url = "";//分享链接，如果我是核销员，不管分享人，直接分享我的openid
                if (await repo.WoerCanWriteOff(Guid.Parse(g.mid), Guid.Parse(currentUser.Id)))
                {
                    url = MdWxSettingUpHelper.GenGoDetailUrl_fx(parameter.appid, parameter.goid, currentUser.openid);
                }
                else if (!string.IsNullOrEmpty(parameter.shareopenid))
                {
                    var shareuser = await EsUserManager.GetByOpenIdAsync(parameter.shareopenid);
                    if (shareuser != null)
                    {
                        if (await repo.WoerCanWriteOff(Guid.Parse(g.mid), Guid.Parse(shareuser.Id)))
                        {
                            url = MdWxSettingUpHelper.GenGoDetailUrl_fx(parameter.appid, parameter.goid, parameter.shareopenid);
                        }
                    }
                }
                else
                {
                    url = MdWxSettingUpHelper.GenGoDetailUrl(parameter.appid, parameter.goid);
                }

                //拼json
                float groupPrice = (float)g.group_price / 100;
                int person_quota = g.person_quota;
                string advertise_pic_url = g.advertise_pic_url;
                string title = g.title;
                int wtg = g.waytoget;
                int? user_left = go.user_left;
                var group_type = await AttHelper.GetValueAsync(Guid.Parse(g.Id), EAttTables.Group, EGroupAtt.group_type);//团类型(抽奖团，普通团)
                var group_luckystatus = await AttHelper.GetValueAsync(Guid.Parse(g.Id), EAttTables.Group, EGroupAtt.lucky_status);//团是否开奖
                var lucky_count = await AttHelper.GetValueAsync(Guid.Parse(g.Id), EAttTables.Group, EGroupAtt.lucky_count);//中奖人数
                var lucky_endTime = await AttHelper.GetValueAsync(Guid.Parse(g.Id), EAttTables.Group, EGroupAtt.lucky_endTime);
                //MDLogger.LogInfoAsync(typeof(GroupOrderController),$"分享链接：{url}");
                //剩余时间计算
                string timeLimit = "";
                int timeLimit_Seconds = 0;
                //如果我在团里面，返回我的oid
                Guid oid = Guid.Empty;
                int orderwaytoget = -1;//如果我在团里面，则返回我的订单waytoget
                DateTime expireDate = CommonHelper.FromUnixTime(go.expire_date.Value);
                if (expireDate > DateTime.Now)
                {
                    TimeSpan span = expireDate - DateTime.Now;
                    timeLimit = span.Hours + ":" + span.Minutes + ":" + span.Seconds;
                    timeLimit_Seconds = Convert.ToInt32(go.expire_date.Value - CommonHelper.GetUnixTimeNow());
                }
                else
                {
                    timeLimit = "团已经过期！";
                }

                //团成员列表

                List<int> status = new List<int>() { (int)EOrderStatus.已成团未提货,(int)EOrderStatus.拼团成功, (int)EOrderStatus.已成团未发货,
                (int)EOrderStatus.已成团配货中,(int)EOrderStatus.已发货待收货, (int)EOrderStatus.已支付 };

                if (group_type != null && group_type == ((int)EGroupTypes.抽奖团).ToString() &&
                    group_luckystatus != null && group_luckystatus == ((int)EGroupLuckyStatus.已开奖).ToString() &&
                    go.status == (int)EGroupOrderStatus.拼团成功)
                {
                    status.Add((int)EOrderStatus.已退款);
                    status.Add((int)EOrderStatus.退款中);
                    status.Add((int)EOrderStatus.退款失败);
                }
                var orders = await EsOrderManager.SearchByGoidAsnyc2("", status, parameter.goid, 1, 10, false);
                if (orders.Item1 <= 0)
                {
                    //组团失败了，但是必须获取团成员列表
                    orders =
                        await
                            EsOrderManager.SearchByGoidAsnyc2("",
                                new List<int>() { (int)EOrderStatus.已退款, (int)EOrderStatus.退款中, (int)EOrderStatus.退款失败 },
                                parameter.goid, 1, 10, false);
                }
                //return JsonResponseHelper.HttpRMtoJson($"团订单:{go.Id}底下没有已支付的订单!", HttpStatusCode.OK,
                //        ECustomStatus.Fail);

                bool isMineIn = false;
                ArrayList userList = new ArrayList(orders.Item1);
                for (int i = 0; i < orders.Item2.Count; i++)
                {
                    var o = orders.Item2[i];
                    if (Guid.Parse(o.Id).Equals(Guid.Empty) || Guid.Parse(o.buyer).Equals(Guid.Empty))
                        continue;
                    //支付时间
                    string payTimeStr = CommonHelper.FromUnixTime(o.paytime.Value).ToString("yyyy-MM-dd HH:mm:ss");

                    var indexUser = await EsUserManager.GetByIdAsync(Guid.Parse(o.buyer));

                    if (indexUser == null)
                        return JsonResponseHelper.HttpRMtoJson($"内部错误！buyer:{o.buyer}在es中不存在!", HttpStatusCode.OK,
                            ECustomStatus.Fail);

                    //判断自己是否在里面
                    if (indexUser.openid.Equals(parameter.openid))
                    {
                        isMineIn = true;
                        oid = Guid.Parse(o.Id);
                        orderwaytoget = o.waytoget.Value;
                    }

                    //从redis中取出用户在微信中的信息
                    var user =
                            await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(indexUser.openid);

                    if (user == null)
                        continue;

                    userList.Add(new object());

                    int communityImgCount = 0;
                    int communityPraises = 0;
                    //社区照片总数和点赞总数
                    var community = await EsCommunityManager.GetCountAsync(Guid.Parse(currentUser.mid), Guid.Parse(user.Uid), (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
                    if (community != null)
                    {
                        communityImgCount = community.Item1;
                        communityPraises = community.Item2;
                    }

                    userList[i] =
                        new
                        {
                            uid = user.Uid,
                            headpic = user.HeadImgUrl,
                            name = user.NickName,
                            joinTime = payTimeStr,
                            openid = user.Openid,
                            communityImgCount = communityImgCount,
                            communityPraises = communityPraises
                        };
                }

                //banner
                string banner = "";
                if (go.status == (int)EGroupOrderStatus.拼团进行中)
                {
                    if (go.leader.Equals(parameter.uid.ToString())) //团长
                    {
                        banner = "0"; //"开团成功";
                    }
                    else
                    {
                        if (isMineIn)
                        {
                            banner = "2"; //"入团成功";
                        }
                        else
                        {
                            banner = "1";
                        }
                    }
                }

                if (go.status == (int)EGroupOrderStatus.拼团成功)
                {
                    if (isMineIn)
                    {
                        banner = "3";//"组团成功";
                    }
                    else
                    {
                        banner = "3";//"组团成功"

                        //团成功，查看自己是否已经退款，如果退款则显示参团失败，否则显示拼团成功
                        orders =
                        await
                            EsOrderManager.SearchByGoidAsnyc2("",
                                new List<int>() { (int)EOrderStatus.已退款, (int)EOrderStatus.退款中, (int)EOrderStatus.退款失败 },
                                parameter.goid, 1, 10, false);
                        if (orders != null && (group_type == null || group_type == ((int)EGroupTypes.普通团).ToString()))
                            foreach (var o in orders.Item2)
                            {
                                var indexUser = await EsUserManager.GetByIdAsync(Guid.Parse(o.buyer));

                                if (indexUser == null)
                                    return JsonResponseHelper.HttpRMtoJson($"内部错误！buyer:{o.buyer}在es中不存在!", HttpStatusCode.OK,
                                        ECustomStatus.Fail);

                                //判断自己是否在里面,在里面就显示参团成功
                                if (indexUser.openid.Equals(parameter.openid))
                                {
                                    banner = "4";//参团失败
                                    break;
                                }
                            }
                    }
                }

                if (go.status == (int)EGroupOrderStatus.拼团失败)
                    banner = "-1";//"组团失败";
                var retObject =
                    new
                    {
                        gid = go.gid,
                        original_price = (float)go.price.Value / 100,
                        groupPrice = groupPrice,
                        person_quota = person_quota,
                        advertise_pic_url = advertise_pic_url,
                        title = title,
                        wtg = wtg,//团的wtg
                        user_left = user_left,
                        timeLimit = timeLimit,
                        timeLimit_Seconds = timeLimit_Seconds,
                        userList = userList,
                        banner = banner,
                        url = url,
                        oid = oid,//如果我在里面，则返回我的oid
                        orderwaytoget = orderwaytoget,////如果我在团里面，则返回我的订单waytoget
                        group_type = group_type == null ? "0" : group_type,//团类型（0普通1抽奖）
                        group_luckystatus = group_luckystatus == null ? "0" : group_luckystatus,//是否开奖
                        lucky_count = lucky_count ?? "0",
                        lucky_endTime = lucky_endTime ?? "0",
                    };

                return
                    JsonResponseHelper.HttpRMtoJson(
                        retObject,
                        HttpStatusCode.OK,
                        ECustomStatus.Success);
            }
        }

        [HttpPost]
        [Route("getbyid")]
        public async Task<HttpResponseMessage> getbyid(GoParameter parameter)
        {
            if (parameter == null || parameter.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) ||
                parameter.uid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            var go = await EsGroupOrderManager.GetByIdAsync(parameter.goid);
            if (go == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到goid:{parameter.goid}，的团订单信息！", HttpStatusCode.OK,
                    ECustomStatus.Fail);
            var g = await EsGroupManager.GetByGidAsync(Guid.Parse(go.gid));
            if (g == null || Guid.Parse(g.Id).Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"没有取到goid:{parameter.goid}，的团信息！", HttpStatusCode.OK,
                    ECustomStatus.Fail);

            //拼json
            float groupPrice = (float)g.group_price / 100;
            int person_quota = g.person_quota;
            string advertise_pic_url = g.advertise_pic_url;
            string title = g.title;
            int wtg = g.waytoget;
            int? user_left = go.user_left;
            string url = MdWxSettingUpHelper.GenGoDetailUrl_fx(parameter.appid, parameter.goid, parameter.openid);
            //MDLogger.LogInfoAsync(typeof(GroupOrderController),$"分享链接：{url}");
            //剩余时间计算
            string timeLimit = "";
            DateTime expireDate = CommonHelper.FromUnixTime(go.expire_date.Value);
            if (expireDate > DateTime.Now)
            {
                TimeSpan span = expireDate - DateTime.Now;
                timeLimit = span.Hours + ":" + span.Minutes + ":" + span.Seconds;
            }
            else
            {
                timeLimit = "团已经过期！";
            }

            //团成员列表

            List<int> status = new List<int>() { (int)EOrderStatus.拼团成功, (int)EOrderStatus.已成团未发货, (int)EOrderStatus.已成团未提货,
                (int)EOrderStatus.已成团配货中,(int)EOrderStatus.已发货待收货, (int)EOrderStatus.已支付 };
            var orders = await EsOrderManager.SearchByGoidAsnyc2("", status, parameter.goid, 1, 10, false);

            if (orders.Item1 <= 0)
            {
                //组团失败了，但是必须获取团成员列表
                orders =
                    await
                        EsOrderManager.SearchByGoidAsnyc2("",
                            new List<int>() { (int)EOrderStatus.已退款, (int)EOrderStatus.退款中, (int)EOrderStatus.退款失败 },
                            parameter.goid, 1, 10, false);
            }
            //return JsonResponseHelper.HttpRMtoJson($"团订单:{go.Id}底下没有已支付的订单!", HttpStatusCode.OK,
            //        ECustomStatus.Fail);

            bool isMineIn = false;
            ArrayList userList = new ArrayList(orders.Item1);
            for (int i = 0; i < orders.Item2.Count; i++)
            {
                var o = orders.Item2[i];
                if (Guid.Parse(o.Id).Equals(Guid.Empty) || Guid.Parse(o.buyer).Equals(Guid.Empty))
                    continue;
                //支付时间
                string payTimeStr = CommonHelper.FromUnixTime(o.paytime.Value).ToString("yyyy-MM-dd HH:mm:ss");

                var indexUser = await EsUserManager.GetByIdAsync(Guid.Parse(o.buyer));

                if (indexUser == null)
                    return JsonResponseHelper.HttpRMtoJson($"内部错误！buyer:{o.buyer}在es中不存在!", HttpStatusCode.OK,
                        ECustomStatus.Fail);

                //判断自己是否在里面
                if (indexUser.openid.Equals(parameter.openid))
                    isMineIn = true;

                //从redis中取出用户在微信中的信息
                var user =
                        await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(indexUser.openid);

                if (user == null)
                    continue;

                userList.Add(new object());

                userList[i] =
                    new
                    {
                        headpic = user.HeadImgUrl,
                        name = user.NickName,
                        joinTime = payTimeStr,
                        openid = user.Openid
                    };
            }

            //banner
            string banner = "";
            if (go.status == (int)EGroupOrderStatus.拼团进行中)
            {
                if (go.leader.Equals(parameter.uid.ToString())) //团长
                {
                    banner = "0"; //"开团成功";
                }
                else
                {
                    if (isMineIn)
                    {
                        banner = "2"; //"入团成功";
                    }
                    else
                    {
                        banner = "1";
                    }
                }
            }

            if (go.status == (int)EGroupOrderStatus.拼团成功)
            {
                if (isMineIn)
                {
                    banner = "3";//"组团成功";
                }
                else
                {
                    banner = "3";//"组团成功"

                    //团成功，查看自己是否已经退款，如果退款则显示参团失败，否则显示拼团成功
                    orders =
                    await
                        EsOrderManager.SearchByGoidAsnyc2("",
                            new List<int>() { (int)EOrderStatus.已退款, (int)EOrderStatus.退款中, (int)EOrderStatus.退款失败 },
                            parameter.goid, 1, 10, false);
                    foreach (var o in orders.Item2)
                    {
                        var indexUser = await EsUserManager.GetByIdAsync(Guid.Parse(o.buyer));

                        if (indexUser == null)
                            return JsonResponseHelper.HttpRMtoJson($"内部错误！buyer:{o.buyer}在es中不存在!", HttpStatusCode.OK,
                                ECustomStatus.Fail);

                        //判断自己是否在里面,在里面就显示参团成功
                        if (indexUser.openid.Equals(parameter.openid))
                        {
                            banner = "4";//参团失败
                            break;
                        }
                    }
                }
            }

            if (go.status == (int)EGroupOrderStatus.拼团失败)
                banner = "-1";//"组团失败";

            var retObject =
                new
                {
                    gid = go.gid,
                    original_price = (float)go.price.Value / 100,
                    groupPrice = groupPrice,
                    person_quota = person_quota,
                    advertise_pic_url = advertise_pic_url,
                    title = title,
                    wtg = wtg,
                    user_left = user_left,
                    timeLimit = timeLimit,
                    userList = userList,
                    banner = banner,
                    url = url
                };

            return
                JsonResponseHelper.HttpRMtoJson(
                    retObject,
                    HttpStatusCode.OK,
                    ECustomStatus.Success);
        }

        [HttpPost]
        [Route("geturl")]
        public HttpResponseMessage getGodetailUrl(GoParameter parameter)
        {
            if (parameter == null || parameter.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            string url = MdWxSettingUpHelper.GenGoDetailUrl(parameter.appid, parameter.goid);
            return
                JsonResponseHelper.HttpRMtoJson(
                    url,
                    HttpStatusCode.OK,
                    ECustomStatus.Success);
        }

        /// <summary>
        /// 根据gid获取开团进行中的前pageSize个订单(正在拼)
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getgrouporderbygid")]
        public async Task<HttpResponseMessage> getgrouporderbygid(GoParameter parameter)
        {
            try
            {
                if (parameter == null || parameter.gid.Equals(Guid.Empty) || parameter.pageSize <= 0 || parameter.pageIndex <= 0)
                {
                    return JsonResponseHelper.HttpRMtoJson($"parameter error!gid:{parameter.gid},pageSize:{parameter.pageSize},pageIndex:{parameter.pageIndex}", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                //获取拼团进行中的团gidlist
                var gorderList = EsGroupOrderManager.GetByGid(parameter.gid, (int)EGroupOrderStatus.拼团进行中, parameter.pageIndex, parameter.pageSize, 1);
                int totalPage = MdWxSettingUpHelper.GetTotalPages(gorderList.Item1);
                //根据guidList查询该团信息
                List<object> obj = new List<object>();
                foreach (var grouporder in gorderList.Item2)
                {
                    var group = await EsGroupManager.GetByGidAsync(Guid.Parse(grouporder.gid));//获取该团信息
                    var user = await EsUserManager.GetByIdAsync(Guid.Parse(grouporder.leader));//获取团长信息
                    UserInfoRedis userRedis = await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(user.openid);//获取团长头像

                    //剩余时间计算
                    string timeLimit = "";
                    int timeLimit_Seconds = 0;
                    DateTime expireDate = CommonHelper.FromUnixTime(grouporder.expire_date.Value);
                    if (expireDate > DateTime.Now)
                    {
                        TimeSpan span = expireDate - DateTime.Now;
                        timeLimit = span.Hours + ":" + span.Minutes + ":" + span.Seconds;
                        timeLimit_Seconds = Convert.ToInt32(grouporder.expire_date.Value - CommonHelper.GetUnixTimeNow());
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
                            group.title,
                            user.name,
                            userRedis.HeadImgUrl,
                            userRedis.Uid,//团长的uid
                            grouporder.user_left,//参团人数
                            group.person_quota,//参团总人数
                            timeLimit = timeLimit,//结束剩余时间（小时：分：秒）
                            timeLimit_Seconds = timeLimit_Seconds//结束剩余时间总秒数

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
