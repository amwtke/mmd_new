using System;
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
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Redis;
using MD.Model.DB.Code;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Order;
using MD.WeChat.Filters;
using MD.Model.DB.Professional;
using System.IO;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;
using MD.Lib.DB.Redis.MD;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/order")]
    [AccessFilter]
    public class WechatOrderController : ApiController
    {
        [HttpPost]
        [Route("getorders")]
        public async Task<HttpResponseMessage> GetOrders(OrderParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty) || parameter.pageIndex <= 0)
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            int pageSize = MdWxSettingUpHelper.GetPageSize();



            var orders =
                await
                    EsOrderManager.SearchAsnyc2("", parameter.mid, parameter.uid, parameter.waytoget, null, parameter.pageIndex,
                        pageSize);
            if (orders == null)
            {
                return JsonResponseHelper.HttpRMtoJson($"没有取到mid:{parameter.mid}，的订单信息！", HttpStatusCode.OK,
                    ECustomStatus.Fail);
            }

            List<object> retList = new List<object>();

            foreach (var o in orders.Item2)
            {
                if (string.IsNullOrEmpty(o.gid) || Guid.Parse(o.gid).Equals(Guid.Empty))
                    continue;
                var g = await EsGroupManager.GetByGidAsync(Guid.Parse(o.gid));
                if (g == null)
                    continue;
                var go = await EsGroupOrderManager.GetByIdAsync(Guid.Parse(o.goid));
                if (go == null)
                    continue;

                string joinType = go.leader.Equals(parameter.uid.ToString()) ? "kt" : "ct";

                Guid gid = Guid.Parse(g.Id);
                string group_type = await AttHelper.GetValueAsync(gid, EAttTables.Group, EGroupAtt.group_type);
                string group_luckystatus = await AttHelper.GetValueAsync(gid, EAttTables.Group, EGroupAtt.lucky_status);//抽奖团团状态是否开奖
                string o_luckystatus = await AttHelper.GetValueAsync(gid, EAttTables.Order, EOrderAtt.luckyStatus);

                //抽奖团的剩余时间计算
                //string luckytimeLimit = "";
                //int luckytimeLimit_Seconds = 0;
                //double timelimit = Convert.ToDouble(await AttHelper.GetValueAsync(gid, EAttTables.Group, EGroupAtt.lucky_endTime));
                //DateTime expireDate = CommonHelper.FromUnixTime(timelimit);
                //if (expireDate > DateTime.Now)
                //{
                //    TimeSpan span = expireDate - DateTime.Now;
                //    luckytimeLimit = span.Hours + ":" + span.Minutes + ":" + span.Seconds;
                //    luckytimeLimit_Seconds = Convert.ToInt32(timelimit - CommonHelper.GetUnixTimeNow());
                //}
                //else
                //{
                //    luckytimeLimit = "团已经过期！";
                //}
                var data =
                    new
                    {
                        oid = o.Id,
                        pic = g.advertise_pic_url,
                        title = g.title,
                        wtg = o.waytoget,
                        person_quota = g.person_quota,
                        price = (float)g.group_price / 100,
                        oStatus = o.status,
                        payTime = o.paytime,
                        goid = go.Id,
                        goStatus = go.status,
                        joinType = joinType,
                        o.post_number,
                        group_type = group_type == null ? "0" : group_type,//0：普通团1：抽奖团
                        group_lucky_status = group_luckystatus == null ? "0" : group_luckystatus,//团是否开奖
                        order_lucky_status = o_luckystatus == null ? "0" : o_luckystatus,//我是否中奖
                        //lucky_timeLimit = luckytimeLimit,
                        //lucky_timeLimit_Seconds = luckytimeLimit_Seconds

                    };
                retList.Add(data);
            }

            return
                JsonResponseHelper.HttpRMtoJson(
                    new { totalPage = MdWxSettingUpHelper.GetTotalPages(orders.Item1), olist = retList },
                    HttpStatusCode.OK,
                    ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getbyid")]
        public async Task<HttpResponseMessage> getbyid(OrderParameter parameter)
        {
            if (parameter == null || parameter.oid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid) || parameter.mid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                using (var repoaddress = new AddressRepository())
                {
                    var order = await repo.OrderGetByOid(parameter.oid);
                    if (order == null || !order.oid.Equals(parameter.oid))
                    {
                        return JsonResponseHelper.HttpRMtoJson($"订单数据错误!订单号:{parameter.oid}", HttpStatusCode.OK,
                            ECustomStatus.Fail);
                    }

                    var group = await EsGroupManager.GetByGidAsync(order.gid);
                    if (group == null || !group.Id.Equals(order.gid.ToString()))
                    {
                        return JsonResponseHelper.HttpRMtoJson($"订单数据错误!团不存在！gid:{order.gid}", HttpStatusCode.OK,
                            ECustomStatus.Fail);
                    }
                    //团状态，普通团或抽奖团
                    Guid gid = Guid.Parse(group.Id);
                    var group_type = await AttHelper.GetValueAsync(gid, EAttTables.Group, EGroupAtt.group_type);
                    //是否开奖
                    var luckygroup_status = await AttHelper.GetValueAsync(gid, EAttTables.Group, EGroupAtt.lucky_status);
                    //该订单是否中奖
                    var luckyorder_status = await AttHelper.GetValueAsync(order.oid, EAttTables.Order, EOrderAtt.luckyStatus);
                    //核销地址
                    string url = MdWxSettingUpHelper.GenWriteOffOrderUrl(parameter.appid, parameter.oid);

                    //参团还是开团
                    var go = await EsGroupOrderManager.GetByIdAsync(order.goid);
                    if (go == null)
                        return JsonResponseHelper.HttpRMtoJson($"订单数据错误!go不存在:goid：{order.goid}",
                            HttpStatusCode.OK, ECustomStatus.Fail);

                    string joinType = go.leader.Equals(parameter.uid.ToString()) ? "kt" : "ct";

                    //提货地点与提货人

                    string name = order.name;
                    string cellphone = order.cellphone;
                    string wopAddress = "";
                    string wopName = "";
                    if (order.waytoget == (int)EWayToGet.自提)
                    {
                        //快递到家上线后，自提的name和cellphone一并存到了order，之前的数据还是从这里面取
                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(cellphone))
                        {
                            var user_wo = await repo.UserWriteoffGetByMidAndUidAsync(parameter.mid, parameter.uid);
                            if (user_wo == null)
                                return
                                    JsonResponseHelper.HttpRMtoJson(
                                        $"订单数据错误!user_writeoff不存在:mid:{parameter.mid};oid:{parameter.uid}", HttpStatusCode.OK,
                                        ECustomStatus.Fail);
                            name = user_wo.user_name;
                            cellphone = user_wo.cellphone;
                        }
                        var wop = await EsWriteOffPointManager.GetByIdAsync(order.default_writeoff_point);
                        if (wop == null || !wop.Id.Equals(order.default_writeoff_point.ToString()))
                        {
                            return JsonResponseHelper.HttpRMtoJson($"订单数据错误!Wop不存在:{order.default_writeoff_point}",
                                HttpStatusCode.OK, ECustomStatus.Fail);
                        }
                        wopAddress = wop.address;
                        wopName = wop.name;
                    }
                    string companyName = "";
                    if (!string.IsNullOrEmpty(order.post_company))
                        companyName = await repoaddress.GetCompanyNameByCode(order.post_company);
                    var ret =
                        new
                        {
                            wtg = order.waytoget,
                            oStatus = order.status,
                            otn = order.o_no,
                            upid = order.upid.Equals(Guid.Empty) ? "" : order.upid.ToString(),
                            price = (float)order.actual_pay.Value / 100,
                            wopAddress,//核销门店地址
                            wopName,//核销门店
                            name,//收货人姓名
                            cellphone,//收货人电话
                            order.postaddress,//邮寄地址
                            post_company = order.post_company,//快递公司代码
                            post_number = order.post_number,//运单号
                            post_companyName = companyName,
                            payTime = CommonHelper.FromUnixTime(order.paytime.Value).ToString("yyyy-MM-dd HH:mm:ss"),
                            url,
                            goid = order.goid,
                            gid = order.gid,
                            pid = group.pid,
                            group_title = group.title,
                            group_pic1 = group.advertise_pic_url,
                            group_price = group.group_price / 100.00,
                            group_person_quota = group.person_quota,
                            joinType = joinType,
                            goStatus = go.status,//组团状态0:拼团进行中,1拼团成功,2拼团失败,3开团中
                            group_type = group_type == null ? "0" : group_type,//团状态0:普通团1抽奖团
                            luckygroup_status = luckygroup_status == null ? "0" : luckygroup_status,//是否开奖0未开1已开
                            luckyorder_status = luckyorder_status == null ? "0" : luckyorder_status//是否中奖0未中1已中
                        };
                    return
                        JsonResponseHelper.HttpRMtoJson(
                            ret,
                            HttpStatusCode.OK,
                            ECustomStatus.Success);
                }
            }
        }

        /// <summary>
        /// 宣传图，获取最新的一条订单。
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("getAdorders")]
        public async Task<HttpResponseMessage> getAdorders()
        {
            var orders = await EsOrderManager.Top10("", new List<int>() { (int)EOrderStatus.拼团成功, (int)EOrderStatus.已支付, (int)EOrderStatus.已成团未提货, (int)EOrderStatus.已成团未发货 });
            if (orders.Count > 0)
            {
                var order = orders[0];
                var group = await EsGroupManager.GetByGidAsync(Guid.Parse(order.gid));
                if (group != null)
                {
                    var user = await EsUserManager.GetByIdAsync(Guid.Parse(order.buyer));
                    //var redisUser =
                    //    await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(user.openid);
                    if (user != null)
                    {
                        var ret = new
                        {
                            pic = group.advertise_pic_url,
                            username = user.name
                        };
                        return JsonResponseHelper.HttpRMtoJson(
                        ret,
                        HttpStatusCode.OK,
                        ECustomStatus.Success);
                    }
                }
            }
            return
                    JsonResponseHelper.HttpRMtoJson(
                        "",
                        HttpStatusCode.OK,
                        ECustomStatus.Fail);
        }

        /// <summary>
        /// 中奖名单
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getluckyorders")]
        public async Task<HttpResponseMessage> getLuckyOrders(OrderParameter parameter)
        {
            if (parameter == null || parameter.gid.Equals(Guid.Empty) || parameter.pageIndex <= 0 || parameter.mid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,gid:{parameter.gid},mid:{parameter.mid},pageIndex:{parameter.pageIndex}!", HttpStatusCode.OK, ECustomStatus.Fail);
            var group = await EsGroupManager.GetByGidAsync(parameter.gid);
            if (group == null)
                return JsonResponseHelper.HttpRMtoJson($"该团不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
            string groupType = await AttHelper.GetValueAsync(parameter.gid, EAttTables.Group, EGroupAtt.group_type);
            if (groupType != ((int)EGroupTypes.抽奖团).ToString())
                return JsonResponseHelper.HttpRMtoJson($"团状态错误，不是抽奖团。grouptype:{groupType}！", HttpStatusCode.OK, ECustomStatus.Fail);
            string g_luckystatus = await AttHelper.GetValueAsync(parameter.gid, EAttTables.Group, EGroupAtt.lucky_status);
            if (g_luckystatus != ((int)EGroupLuckyStatus.已开奖).ToString())
                return JsonResponseHelper.HttpRMtoJson($"该团还未开奖。g_luckystatus:{g_luckystatus}！", HttpStatusCode.OK, ECustomStatus.Fail);
            string lucky_endtime = await AttHelper.GetValueAsync(parameter.gid, EAttTables.Group, EGroupAtt.lucky_endTime);
            int pageSize = MdWxSettingUpHelper.GetPageSize();
            var Tuple = await EsOrderManager.GetByGidAsync(parameter.gid, new List<int>() { (int)EOrderStatus.已成团未提货, (int)EOrderStatus.已成团未发货, (int)EOrderStatus.已成团配货中, (int)EOrderStatus.已发货待收货, (int)EOrderStatus.拼团成功 }, parameter.pageIndex, pageSize);
            if (Tuple.Item1 >= 0)
            {
                List<object> zjUserList = new List<object>();
                var RedisManager2 = new RedisManager2<WeChatRedisConfig>();
                foreach (var o in Tuple.Item2)
                {
                    //查询是否中奖
                    var orderlucky_status = await AttHelper.GetValueAsync(Guid.Parse(o.Id), EAttTables.Order, EOrderAtt.luckyStatus);
                    if (string.IsNullOrEmpty(orderlucky_status) || orderlucky_status == "0")
                        continue;
                    Guid buyer = Guid.Parse(o.buyer);//购买人uid
                    var indexuser = await EsUserManager.GetByIdAsync(buyer);
                    var redisUser = await RedisManager2.GetObjectFromRedisHash<UserInfoRedis>(indexuser.openid);
                    var user = new
                    {
                        HeadImgUrl = redisUser.HeadImgUrl,//购买人头像
                        buyername = indexuser.name,//购买人昵称
                        o_no = o.o_no
                    };
                    zjUserList.Add(user);
                }
                var tupgo = await EsGroupOrderManager.GetByGidAsync(parameter.gid, EGroupOrderStatus.拼团成功, 1, 1);
                var data =
                 new
                 {
                     pic = group.advertise_pic_url,
                     title = group.title,
                     wtg = group.waytoget,
                     person_quota = group.person_quota,
                     price = (float)group.group_price / 100,
                     group_type = groupType,
                     group_luckystatus = g_luckystatus,//抽奖团状态0：未开奖，1已开奖
                     cyrs = tupgo.Item1 * group.person_quota,//参与抽奖人数(拼团成功的总人数)
                     zyrs = Tuple.Item1,//中奖人数
                     kjdate = lucky_endtime,//开奖时间
                     zjUserList = zjUserList
                 };
                return JsonResponseHelper.HttpRMtoJson(new { totalPage = MdWxSettingUpHelper.GetTotalPages(Tuple.Item1), olist = data },
                   HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson("暂无订单", HttpStatusCode.OK, ECustomStatus.Success);
        }


        [HttpPost]
        [Route("receiptgoods")]
        public async Task<HttpResponseMessage> Receiptgoods(OrderParameter parameter)
        {
            if (parameter == null || parameter.oid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var order = await EsOrderManager.GetByIdAsync(parameter.oid);
            if (order == null)
                return JsonResponseHelper.HttpRMtoJson($"esorder is null oid:{parameter.oid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"esuser is null opid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            if (!order.buyer.Equals(user.Id))
                return JsonResponseHelper.HttpRMtoJson($"用户与订单不对应，uid:{user.Id},obuyer:{order.buyer}", HttpStatusCode.OK, ECustomStatus.Fail);
            if (order.status != (int)EOrderStatus.已发货待收货)
                return JsonResponseHelper.HttpRMtoJson($"当前状态无法确认收货：{((EOrderStatus)order.status).ToString()}", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                bool flag = await repo.UpdateOrderStatusByOid(parameter.oid, (int)EOrderStatus.拼团成功);//更新数据库，并更新es
                return JsonResponseHelper.HttpRMtoJson(new { isOk = flag }, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        /// <summary>
        /// 获取正在拼团的人
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getptingusers")]
        public async Task<HttpResponseMessage> GetPTIngUsers(OrderParameter parameter)
        {
            if (parameter == null || parameter.gid.Equals(Guid.Empty) || parameter.pageIndex < 0 || parameter.pageSize < 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error", HttpStatusCode.OK, ECustomStatus.Fail);
            var group = await EsGroupManager.GetByGidAsync(parameter.gid);
            if (group == null)
                return JsonResponseHelper.HttpRMtoJson($"es group is null", HttpStatusCode.OK, ECustomStatus.Fail);
            int totalPage = 0;
            List<object> retObj = new List<object>();
            var listStatus = new List<int>() { (int)EOrderStatus.已支付, (int)EOrderStatus.已成团未提货, (int)EOrderStatus.已成团未发货 };
            var tupleOrder = await EsOrderManager.GetByGidAsync(parameter.gid, listStatus, parameter.pageIndex, parameter.pageSize);
            if (tupleOrder != null)
            {
                totalPage = MdWxSettingUpHelper.GetTotalPages(tupleOrder.Item1, parameter.pageSize);
                foreach (var order in tupleOrder.Item2)
                {
                    Guid buyer = Guid.Parse(order.buyer);
                    var redisBuyer = await RedisUserOp.GetByUidAsnyc(buyer);
                    if (redisBuyer == null)
                        continue;
                    var isKt = false;
                    var grouporder = await EsGroupOrderManager.GetByIdAsync(Guid.Parse(order.goid));
                    if (grouporder == null)
                        continue;
                    if (grouporder.leader.Equals(order.buyer))
                        isKt = true;
                    int imgPraisesCount = 0;//照片被赞次数
                    int imgCount = 0;//总照片数
                    var tupleCommunity = await EsCommunityManager.GetCountAsync(Guid.Parse(order.mid), buyer, (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
                    if (tupleCommunity != null)
                    {
                        imgCount = tupleCommunity.Item1;
                        imgPraisesCount = tupleCommunity.Item2;
                    }
                    retObj.Add(new
                    {
                        uid=buyer,
                        redisBuyer.NickName,
                        redisBuyer.HeadImgUrl,
                        isKt= isKt,
                        imgCount,
                        imgPraisesCount,
                        time=GetTimeString(order.paytime.Value)
                    });
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = retObj }, HttpStatusCode.OK, ECustomStatus.Success);

        }

        /// <summary>
        /// 物流查询，暂时不用
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getlogisticsinfo")]
        public async Task<HttpResponseMessage> GetLogisticsInfo(OrderParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.companyCode) || string.IsNullOrEmpty(parameter.number))
                return JsonResponseHelper.HttpRMtoJson($"parameter is null ccode:{parameter.companyCode},num:{parameter.number}", HttpStatusCode.OK, ECustomStatus.Fail);
            string ApiKey = ConfigurationManager.AppSettings["PostApiKey"];
            string apiurl = @"http://api.kuaidi100.com/api?id=" + ApiKey + "&com=" + parameter.companyCode + "&nu=" + parameter.number + "&show=0&muti=1&order=desc";
            WebRequest request = WebRequest.Create(apiurl);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            Encoding encode = Encoding.UTF8;
            StreamReader reader = new StreamReader(stream, encode);
            string detail = reader.ReadToEnd();
            string aaa = "{\"message\":\"ok\",\"status\":\"1\",\"state\":\"3\",\"data\":[{ \"time\":\"2012-07-07 13:35:14\",\"context\":\"客户已签收\"}," +
                         " { \"time\":\"2012-07-07 09:10:10\",\"context\":\"离开[北京\"}," +
                         " { \"time\":\"2012-07-06 19:46:38\",\"context\":\"到达\"}," +
                          "{ \"time\":\"2012-07-06 15:22:32\",\"context\":\"离开\"}," +
                          "{ \"time\":\"2012-07-06 15:05:00\",\"context\":\"到达\"}," +
                          "{ \"time\":\"2012-07-06 13:37:52\",\"context\":\"离开\"}," +
                          "{ \"time\":\"2012-07-06 12:54:41\",\"context\":\"到达\"}," +
                          "{ \"time\":\"2012-07-06 11:11:03\",\"context\":\"离开[北]\"}," +
                          "{ \"time\":\"2012-07-06 10:43:21\",\"context\":\"到\"}," +
                          "{ \"time\":\"2012-07-05 21:18:53\",\"context\":\"离开\"}," +
                          "{ \"time\":\"2012-07-05 20:07:27\",\"context\":\"已取件\"}" +
                        "]}";
            var bbb = JsonConvert.DeserializeObject<LogisticsPartialInfo>(aaa);
            return JsonResponseHelper.HttpRMtoJson(new { data = bbb }, HttpStatusCode.OK, ECustomStatus.Success);
        }
        private class LogisticsPartialInfo
        {
            /// <summary>
            /// {"message":"身份key认证失败！","status":"0"}
            /// </summary>
            public string message { get; set; }
            /// <summary>
            /// 快递单当前的状态 ：0：在途，即货物处于运输过程中；
            /// 1：揽件，货物已由快递公司揽收并且产生了第一条跟踪信息；
            /// 2：疑难，货物寄送过程出了问题；
            /// 3：签收，收件人已签收；
            /// 4：退签，即货物由于用户拒签、超区等原因退回，而且发件人已经签收；
            /// 5：派件，即快递正在进行同城派件；
            /// 6：退回，货物正处于退回发件人的途中；
            /// </summary>
            public string state { get; set; }
            /// <summary>
            /// 查询结果状态： 0：物流单暂无结果， 1：查询成功， 2：接口出现异常，
            /// </summary>
            public string status { get; set; }

            public List<LogisticsMessage> data { get; set; }
        }
        private class LogisticsMessage
        {
            /// <summary>
            /// 每条跟踪信息的时间
            /// </summary>
            public string time { get; set; }
            /// <summary>
            /// 每条跟综信息的描述
            /// </summary>
            public string context { get; set; }
        }

        private string GetTimeString(double timestamp)
        {
            TimeSpan ts = DateTime.Now - CommonHelper.FromUnixTime(timestamp);
            if (ts.Days > 0)
                return ts.Days + "天前";
            if (ts.Hours > 0)
                return ts.Hours + "小时前";
            if (ts.Minutes > 0)
                return ts.Minutes + "分钟前";
            return "刚刚";
        }
    }
}

