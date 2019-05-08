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
using MD.Lib.DB.Redis;
using MD.Model.Configuration.Redis;
using MD.Lib.ElasticSearch;
using MD.Lib.Weixin.Component;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using MD.Model.DB;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Model.Configuration.Aliyun;
using System.Drawing;
using MD.Lib.Util.Files;
using MD.CommonService.Utilities;
using System.IO;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/group")]
    [AccessFilter]
    public class WechatGroupController : ApiController
    {
        [HttpGet]
        [Route("GetGroupInfo")]
        public async Task<HttpResponseMessage> GetGroupInfo(Guid gid)
        {
            if (!gid.Equals(Guid.Empty))
            {
                var group = await EsGroupManager.GetByGidAsync(gid);
                var leader_price = await AttHelper.GetValueAsync(gid, EAttTables.Group.ToString(),
                    EGroupAtt.leader_price.ToString());
                return
                    JsonResponseHelper.HttpRMtoJson(
                        new { price = group.group_price, des = group.title, leader_price = leader_price ?? "0" },
                        HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson(null, HttpStatusCode.NotAcceptable, ECustomStatus.Fail);
        }

        [HttpGet]
        [Route("GetGroupOrderInfo")]
        public async Task<HttpResponseMessage> GetGroupOrderInfo(Guid goid)
        {
            if (!goid.Equals(Guid.Empty))
            {
                var go = await EsGroupOrderManager.GetByIdAsync(goid);
                if (go != null)
                {
                    var group = await EsGroupManager.GetByGidAsync(Guid.Parse(go.gid));
                    if (group != null)
                    {
                        return JsonResponseHelper.HttpRMtoJson(new { price = go.go_price, des = group.title, go.user_left }, HttpStatusCode.OK, ECustomStatus.Success);
                    }

                }
            }
            return JsonResponseHelper.HttpRMtoJson(null, HttpStatusCode.NotAcceptable, ECustomStatus.Fail);
        }

        [HttpGet]
        [Route("sync2es")]
        public async Task<HttpResponseMessage> Sync(Guid gid)
        {
            if (!gid.Equals(Guid.Empty))
            {
                using (var repo = new BizRepository())
                {
                    var group = await repo.GroupGetGroupById(gid);
                    if (group == null)
                        return JsonResponseHelper.HttpRMtoJson($"gid:{gid} group is not exits!", HttpStatusCode.NotAcceptable, ECustomStatus.Fail);

                    var index = await EsGroupManager.GenObject(gid);
                    var ret = await EsGroupManager.AddOrUpdateAsync(index);
                    return JsonResponseHelper.HttpRMtoJson(ret ? "true" : "false", HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
            return JsonResponseHelper.HttpRMtoJson($"gid:{gid} is empty!", HttpStatusCode.NotAcceptable, ECustomStatus.Fail);
        }

        [HttpPost]
        [Route("getgroups")]
        public async Task<HttpResponseMessage> getGroups(BaseParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || parameter.uid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!mid:{parameter.mid},opid:{parameter.openid},uid:{parameter.uid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            int pageSize = MdWxSettingUpHelper.GetPageSize();

            var ret = await EsGroupManager.GetByMidAsync(parameter.mid, new List<int>() { (int)EGroupStatus.已发布, (int)EGroupStatus.已结束 }, parameter.pageIndex, pageSize);
            if (ret == null)
            {
                return JsonResponseHelper.HttpRMtoJson($"没有取到mid:{parameter.mid}的团信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            //拼json
            List<object> groupList = new List<object>();
            int totalPage = MdWxSettingUpHelper.GetTotalPages(ret.Item1);

            foreach (var ig in ret.Item2)
            {
                Guid gid = Guid.Parse(ig.Id);
                //是否售罄 
                bool isSaledOut = ig.person_quota > ig.product_quota;
                if (ig.status != (int)EGroupStatus.已发布)
                    isSaledOut = true;
                //已售
                var soldCount = ig.product_setting_count - ig.product_quota >= 0
                   ? ig.product_setting_count - ig.product_quota
                   : 0;
                //团长价格
                var leader_price = await AttHelper.GetValueAsync(gid, EAttTables.Group.ToString(),
                    EGroupAtt.leader_price.ToString());

                string l_p = leader_price == null ? "0" : (Convert.ToDecimal(leader_price) / 100).ToString();
                //商品原价格
                var indexp = await EsProductManager.GetByPidAsync(Guid.Parse(ig.pid));
                if (indexp == null)
                {
                    return JsonResponseHelper.HttpRMtoJson($"没有取到pid:{ig.pid}的商品信息！", HttpStatusCode.OK,
                        ECustomStatus.Fail);
                }
                //团类型0：普通团，1：抽奖团
                var group_type = await AttHelper.GetValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.group_type.ToString());
                string g_t = group_type == null ? "0" : group_type;
                //抽奖团状态
                var luckystatus = await AttHelper.GetValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.lucky_status.ToString());
                string l_s = luckystatus == null ? "0" : luckystatus;
                groupList.Add(
                    new
                    {
                        href = ig.advertise_pic_url,
                        ibaoyou = ig.waytoget,
                        isellout = isSaledOut,
                        soldCount = soldCount,//已售
                        brief = ig.title,
                        nums = ig.person_quota,
                        price = (float)ig.group_price / 100,
                        gid = ig.Id.ToString(),
                        create_time = ig.last_update_time,
                        leader_price = l_p,
                        productprice = (float)indexp.price / 100,
                        category = indexp.category,
                        group_type = g_t,
                        lucky_status = l_s
                    });
            }
            //统计浏览量
            EsBizLogStatistics.AddMidBizViewLog(parameter.mid.ToString(), parameter.openid, parameter.uid);
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = groupList }, HttpStatusCode.OK,
                ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getdetail")]
        public async Task<HttpResponseMessage> getdetail(GroupParameter parameter)
        {
            if (parameter == null || parameter.gid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                Guid gid = parameter.gid;

                var indexg = await EsGroupManager.GetByGidAsync(gid);
                if (indexg == null)
                    return JsonResponseHelper.HttpRMtoJson($"没有取到gid:{parameter.gid}的团信息！", HttpStatusCode.OK,
                        ECustomStatus.Fail);

                var indexp = await EsProductManager.GetByPidAsync(Guid.Parse(indexg.pid));

                if (indexp == null)
                {
                    return JsonResponseHelper.HttpRMtoJson($"没有取到pid:{indexg.pid}的商品信息！", HttpStatusCode.OK,
                        ECustomStatus.Fail);
                }
                var indexUser = await EsUserManager.GetByOpenIdAsync(parameter.openid);
                if (indexUser == null)
                    return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户信息！", HttpStatusCode.OK, ECustomStatus.Fail);

                //拼json

                //图片列表
                List<string> imgList = new List<string>() {indexp.advertise_pic_1, indexp.advertise_pic_2,
                indexp.advertise_pic_3};
                //订单列表
                var soldCount = indexg.product_setting_count - indexg.product_quota >= 0
                    ? indexg.product_setting_count - indexg.product_quota
                    : 0;

                //库存数
                var inventory = indexg.product_quota;

                //html描述
                string html = HttpUtility.HtmlDecode(indexp.description).Replace("\"", "\'");

                //是否售罄 
                bool isSaledOut = indexg.person_quota > indexg.product_quota;
                if (indexg.status != (int)EGroupStatus.已发布)
                    isSaledOut = true;
                string url = "";//分享链接,如果我是核销员，不管分享人，直接分享我的openid
                if (await repo.WoerCanWriteOff(Guid.Parse(indexg.mid), Guid.Parse(indexUser.Id)))
                {
                    url = MdWxSettingUpHelper.GenGroupDetailUrl_fx(parameter.appid, gid, parameter.openid);
                }
                else if (!string.IsNullOrEmpty(parameter.shareopenid))
                {
                    var shareuser = await EsUserManager.GetByOpenIdAsync(parameter.shareopenid);
                    if (shareuser != null)
                    {
                        if (await repo.WoerCanWriteOff(Guid.Parse(indexg.mid), Guid.Parse(shareuser.Id)))
                        {
                            url = MdWxSettingUpHelper.GenGroupDetailUrl_fx(parameter.appid, gid, parameter.shareopenid);
                        }
                    }
                }
                else
                {
                    url = MdWxSettingUpHelper.GenGroupDetailUrl(parameter.appid, gid);
                }

                //团长价格
                string leader_price = await AttHelper.GetValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.leader_price.ToString());
                //该团的访问量
                var tuple = await EsBizLogStatistics.SearchBizViewAsnyc(ELogBizModuleType.GidView, parameter.gid, Guid.Empty, parameter.from, parameter.to);
                //团类型0：普通团，1：抽奖团
                var group_type = await AttHelper.GetValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.group_type.ToString());
                string g_t = group_type == null ? "0" : group_type;
                //抽奖团状态
                var luckystatus = await AttHelper.GetValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.lucky_status.ToString());
                string l_s = luckystatus == null ? "0" : luckystatus;//抽奖团状态0：未开奖，1已开奖
                                                                     //开奖时间
                var kj_Date = await AttHelper.GetValueAsync(gid, EAttTables.Group, EGroupAtt.lucky_endTime);
                var kj_timeLimit = kj_Date == null ? 0 : double.Parse(kj_Date);
                //开奖时间剩余总秒数
                int kj_timeLimit_Seconds = kj_timeLimit - CommonHelper.GetUnixTimeNow() <= 0 ? 0 : Convert.ToInt32(kj_timeLimit - CommonHelper.GetUnixTimeNow());
                //中奖人数
                var lucky_count = await AttHelper.GetValueAsync(gid, EAttTables.Group, EGroupAtt.lucky_count);

                var retObject =
                    new
                    {
                        imgList,
                        title = indexg.title,
                        brief = indexg.description,
                        saled = soldCount,
                        inventory = inventory,
                        originalPrice = (float)indexg.origin_price / 100,
                        groupPrice = (float)indexg.group_price / 100,
                        details = html,
                        getWay = indexg.waytoget,
                        soldOut = isSaledOut,
                        indexg.person_quota,
                        thumbnail = indexg.advertise_pic_url,
                        url,
                        leader_price = leader_price == null ? "0" : (Convert.ToDecimal(leader_price) / 100).ToString(),
                        pid = indexg.pid,
                        standard = indexp.standard,
                        gidView = tuple.Item1,
                        group_type = g_t,
                        lucky_status = l_s,
                        kj_timeLimit = kj_timeLimit,//开奖的时间戳
                        kj_timeLimit_Seconds = kj_timeLimit_Seconds,//开奖到现在的总秒数
                        lucky_count = lucky_count,
                        isshowpting = indexg.isshowpting == null ? true : indexg.isshowpting == 1
                    };

                EsBizLogStatistics.AddGidBizViewLog(parameter.gid.ToString(), parameter.openid, parameter.uid);

                return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        [HttpPost]
        [Route("getwops")]
        public async Task<HttpResponseMessage> getwopsBymid(BaseParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var wopList = await EsWriteOffPointManager.SearchAsnyc("", parameter.mid);
            List<IndexWriteOffPoint> list = wopList.Item2;
            if (list.Count <= 0)
            {
                var Object = new { wopList = wopList };
                return JsonResponseHelper.HttpRMtoJson(Object, HttpStatusCode.OK, ECustomStatus.Success);
            }
            var dicPreWrite = await EsOrderManager.GetOrderCountGroupByWoid(parameter.mid, (int)EOrderStatus.已成团未提货, 0, 0);
            string time = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
            DateTime start = Convert.ToDateTime(time);
            double timeStart = CommonHelper.ToUnixTime(start);
            double timeEnd = CommonHelper.ToUnixTime(start.AddDays(1));
            var dicWriteOff = await EsOrderManager.GetOrderCountGroupByWoid(parameter.mid, (int)EOrderStatus.拼团成功, timeStart, timeEnd);
            List<WriteOffPointJson> listView = new List<WriteOffPointJson>();//返回结果集合
            bool HasPosition = true;
            if (parameter.latitude == 0 || parameter.longitude == 0)
                HasPosition = false;
            foreach (var item in list)
            {
                WriteOffPointJson obj = new WriteOffPointJson()
                {
                    Id = item.Id,
                    address = item.address,
                    name = item.name,
                    tel = item.tel,
                };
                obj.PreWriteCount = dicPreWrite.ContainsKey(item.Id) ? dicPreWrite[item.Id] : 0;
                obj.WriteOffCount = dicWriteOff.ContainsKey(item.Id) ? dicWriteOff[item.Id] : 0;
                if (HasPosition && item.latitude != 0 && item.longitude != 0)
                    obj.distance = GetDistance(parameter.latitude, parameter.longitude, item.latitude, item.longitude);
                else obj.distance = 999999999;
                listView.Add(obj);
            }
            listView.Sort((o1, o2) => Convert.ToInt32(o1.distance - o2.distance));
            var tuple = Tuple.Create(wopList.Item1, listView);
            var retObject = new { wopList = tuple };
            return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
        }
        [HttpPost]
        [Route("getwops2")]
        public async Task<HttpResponseMessage> getwopsBymid(WopParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.gid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            //根据gid获取该团活动门店（woid）
            List<Guid> woidList = new List<Guid>();
            string woidstring = await AttHelper.GetValueAsync(parameter.gid, EAttTables.Group.ToString(), EGroupAtt.activity_point.ToString());
            if (!string.IsNullOrEmpty(woidstring))
            {
                foreach (var woid in woidstring.Split(','))
                {
                    if (!string.IsNullOrEmpty(woid))
                        woidList.Add(Guid.Parse(woid));
                }
            }
            var wopList = await EsWriteOffPointManager.SearchAsnyc("", parameter.mid, woidList);
            if (parameter.latitude == 0 || parameter.longitude == 0)
            {
                var retObject = new { wopList };
                return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
            }
            else
            {
                List<IndexWriteOffPoint> list = wopList.Item2;
                List<WriteOffPointJson> listView = new List<WriteOffPointJson>();//返回结果集合
                foreach (var item in list)
                {
                    WriteOffPointJson obj = new WriteOffPointJson()
                    {
                        Id = item.Id,
                        address = item.address,
                        name = item.name,
                        tel = item.tel
                    };
                    if (item.latitude != 0 && item.longitude != 0)
                        obj.distance = GetDistance(parameter.latitude, parameter.longitude, item.latitude, item.longitude);
                    else
                        obj.distance = 999999999;
                    listView.Add(obj);
                }
                listView.Sort((o1, o2) => Convert.ToInt32(o1.distance - o2.distance));
                var tuple = Tuple.Create(wopList.Item1, listView);
                var retObject = new { wopList = tuple };
                return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        #region 自提
        public class WriteOffPointJson
        {
            public string Id { get; set; }
            public string address { get; set; }
            public string name { get; set; }
            public string tel { get; set; }
            public double distance { get; set; }
            public int PreWriteCount { get; set; }
            public int WriteOffCount { get; set; }
        }

        private double GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double radLat1 = Rad(lat1);
            double radLng1 = Rad(lng1);
            double radLat2 = Rad(lat2);
            double radLng2 = Rad(lng2);
            double a = radLat1 - radLat2;
            double b = radLng1 - radLng2;
            double EARTH_RADIUS = 6378137;
            double result = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2))) * EARTH_RADIUS;
            return Math.Round(result, 2);
        }

        /// <summary>
        /// 经纬度转化成弧度
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private double Rad(double d)
        {
            return d * Math.PI / 180d;
        }
        #endregion

        [HttpPost]
        [Route("getwop")]
        public async Task<HttpResponseMessage> getwopBymid(WopParameter parameter)
        {
            if (parameter == null || parameter.wopid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var wop = await EsWriteOffPointManager.GetByIdAsync(parameter.wopid);
            var retObject = new { wop };

            return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("kt")]
        public async Task<HttpResponseMessage> kt(KtParameter parameter)
        {
            if (parameter == null || parameter.fee <= 0 || parameter.gid.Equals(Guid.Empty) ||
               string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid) || parameter.waytoget >= 2 || parameter.uid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!fee:{parameter.fee},gid:{parameter.gid},openid:{parameter.openid},appid:{parameter.appid},waytoget:{parameter.waytoget}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                try
                {
                    #region 判断商家、团状态是否正确，库存及是否有购买限制，用户ip
                    //验证用户是否关注公众号
                    if ((parameter.fee + parameter.post_price) < 500)
                    {
                        if (!await CheckUserSub(parameter.appid, parameter.openid, parameter.gid, "kt"))
                        {
                            return JsonResponseHelper.HttpRMtoJson("UserNotSubscribe",
                                HttpStatusCode.OK, ECustomStatus.UserNotSubscribe);
                        }
                    }
                    //获取商家信息
                    //WatchStopper ws = new WatchStopper(typeof(WechatGroupController),"KT计时！");
                    //ws.Restart("1-获取mer");
                    var mer = await repo.GetMerchantByAppidAsync(parameter.appid);
                    if (mer == null || string.IsNullOrEmpty(mer.wx_appid) || string.IsNullOrEmpty(mer.wx_mch_id) ||
                        string.IsNullOrEmpty(mer.wx_apikey))
                    {

                        return JsonResponseHelper.HttpRMtoJson("mer is null or other errors!",
                            HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                    //ws.Stop();                
                    //更新用户的取货信息
                    //ws.Restart("2-存uw！");
                    //ws.Stop();
                    //////

                    //获取团信息
                    //ws.Restart("3-获取团信息！");

                    var group = await repo.GroupGetGroupById(parameter.gid);
                    if (group == null)
                        return JsonResponseHelper.HttpRMtoJson("group is null or other errors!",
                            HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    //判断group的waytoget是否与参数匹配
                    if (group.waytoget != (int)EGroupWaytoget.自提或快递到家)//两者都是就不管
                    {
                        if (group.waytoget != parameter.waytoget)
                            return JsonResponseHelper.HttpRMtoJson("group waytoget errors!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }

                    //判断库存（支付成功后减少库存，这里不做锁定，只要有货就能开团）
                    //如果在支付后发现库存被抢光，则退款并提示。
                    //todo:这里会有性能问题，后期改成队列+服务形式。
                    if(group.person_quota > group.product_quota)
                        return JsonResponseHelper.HttpRMtoJson("库存不足，无法开团！",HttpStatusCode.OK, ECustomStatus.Fail);
                    string message = "";
                    if (!MdInventoryHelper.CanOpenGroup(group, parameter.openid, out message))
                        return JsonResponseHelper.HttpRMtoJson(message,
                            HttpStatusCode.OK, ECustomStatus.Fail);
                    //ws.Stop();
                    //准备预支付

                    string userIp = WXPayHelper.GetClientIp(Request);
                    if (string.IsNullOrEmpty(userIp))
                        throw new MDException(typeof(WechatApiController), "can't get user ip address!");
                    #endregion

                    User_WriteOff uw = new User_WriteOff();
                    UserPost up = new UserPost();
                    if (parameter.waytoget == (int)EWayToGet.自提)
                    {
                        if (string.IsNullOrEmpty(parameter.user_name) || string.IsNullOrEmpty(parameter.tel) || parameter.wopid.Equals(Guid.Empty))
                            return JsonResponseHelper.HttpRMtoJson($"parameter error!user_name:{parameter.user_name},tel:{parameter.tel},wopid:{parameter.wopid}", HttpStatusCode.OK, ECustomStatus.Fail);

                        uw.cellphone = parameter.tel;
                        uw.uid = parameter.uid;
                        uw.mid = mer.mid;
                        uw.is_default = true;
                        uw.woid = parameter.wopid;
                        uw.user_name = parameter.user_name;
                        await repo.UserWriteOffSaveOrUpdateAsnyc(uw);
                    }
                    else if (parameter.waytoget == (int)EWayToGet.物流)
                    {
                        if (parameter.upid.Equals(Guid.Empty))
                            return JsonResponseHelper.HttpRMtoJson($"parameter error!upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        up = await repo.GetUserPostByUpidAsync(parameter.upid);
                        if (up == null)
                            return JsonResponseHelper.HttpRMtoJson($"up is null!upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        //再根据up获取邮费,验证参数与邮费是否相等
                        var post_price = await EsLogisticsTemplateManager.GetFeeByCode(group.ltid, up.code);
                        if (post_price < 0)
                            return JsonResponseHelper.HttpRMtoJson($"该地址不在配送区域内", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (parameter.post_price != post_price)
                            return JsonResponseHelper.HttpRMtoJson($"parameter error pp:{parameter.post_price}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (up.is_default == false)
                            await repo.UpdateUserPostIs_DefaultAsync(parameter.uid, parameter.upid);//并且把这个地址设置为默认

                    }
                    //判断参数价格是否被修改过
                    if (parameter.fee != Convert.ToInt32(group.group_price) - Convert.ToInt32(group.leader_price))
                        return JsonResponseHelper.HttpRMtoJson($"价格输入错误", HttpStatusCode.OK, ECustomStatus.Fail);

                    //生成订单
                    // 开团：1、产生一个GroupOrder，2、产生一个Order。状态是：GroupOrder：开团中，Order-未支付。
                    // 返回的Tuple中item1-grouporder的Guid,item2为order的Guid。

                    //ws.Restart("4-生成订单！");
                    //修改开团价格为group.group_price-group.leader_price，不取parameter.fee  ###2016-8-31
                    var ktResult =
                        await
                            MdOrderBizHelper.KtProcessAsync(group, parameter.openid, uw, Convert.ToInt32(group.group_price) - Convert.ToInt32(group.leader_price), parameter.waytoget, up, parameter.post_price);

                    if (ktResult == null)
                    {
                        return JsonResponseHelper.HttpRMtoJson("开团失败!", HttpStatusCode.InternalServerError,
                            ECustomStatus.Fail);
                    }
                    #region 记录分销订单
                    try
                    {
                        if (group.Commission > 0 && !string.IsNullOrEmpty(parameter.shareopenid) && group.group_type == (int)EGroupTypes.普通团)
                        {
                            var shareUser = await EsUserManager.GetByOpenIdAsync(parameter.shareopenid);
                            if (shareUser != null && await repo.WoerCanWriteOff(mer.mid, Guid.Parse(shareUser.Id)))
                            {
                                var distribution = repo.GetDistributionByOid(ktResult.Item2.oid);//判断该订单是否有分销记录
                                if (distribution == null || distribution.oid.Equals(Guid.Empty))
                                {
                                    Distribution dis = new Distribution()
                                    {
                                        oid = ktResult.Item2.oid,
                                        mid = mer.mid,
                                        gid = group.gid,
                                        buyer = ktResult.Item2.buyer,
                                        sharer = Guid.Parse(shareUser.Id),
                                        commission = group.Commission,
                                        isptsucceed = 0,
                                        lastupdatetime = CommonHelper.GetUnixTimeNow(),
                                        sourcetype = (int)EDisSourcetype.订单佣金,
                                        last_commission = 0,
                                        finally_commission = 0
                                    };
                                    await repo.AddDistributionAsync(dis);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(WechatGroupController), new Exception($"开团增加分销数据错误,ex:{ex.Message}"));
                    }
                    #endregion

                    string out_trade_no = ktResult.Item2.o_no;
                    //ws.Stop();

                    //预支付
                    //ws.Restart("5-预支付");
                    var tuple =
                        await
                             WXPayHelper.GZHPay_GenPrePayCode(parameter.openid, parameter.appid, mer.wx_mch_id,
                                group.title, parameter.fee + parameter.post_price, out_trade_no,
                                mer.wx_apikey, userIp);

                    if (tuple == null)
                        throw new MDException(typeof(wxpayController), $"预支付函数调用失败！appid:{parameter.appid}");

                    //更新统计
                    await RedisMerchantStatisticsOp.AfterKtAsync(mer.mid.ToString());
                    if (parameter.longitude != 0 && parameter.latitude != 0)
                    {
                        EsBizLogStatistics.AddPayBizViewLog(parameter.gid.ToString(), parameter.openid, parameter.uid, mer.mid, parameter.longitude, parameter.latitude);
                    }

                    //ws.Stop();
                    return JsonResponseHelper.HttpRMtoJson(
                        new
                        {
                            appId = parameter.appid,
                            timeStamp = tuple.Item3,
                            nonceStr = tuple.Item4,
                            package = string.Format("prepay_id={0}", tuple.Item1),
                            paySign = tuple.Item2,
                            oid = ktResult.Item2.oid,
                            goid = ktResult.Item1.goid
                        }, HttpStatusCode.OK, ECustomStatus.Success

                        );
                }
                catch (PrePayDuplicatedException)
                {
                    return JsonResponseHelper.HttpRMtoJson("您的订单已经生效，请不要重复下单！", HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
                }
                catch (Exception ex)
                {
                    MDLogger.LogErrorAsync(typeof(WechatGroupController), ex);
                    if (ex.ToString().Length > 100)
                    {
                        return JsonResponseHelper.HttpRMtoJson("服务器异常，请刷新页面然后重试！", HttpStatusCode.InternalServerError,
ECustomStatus.Fail);
                    }
                    return JsonResponseHelper.HttpRMtoJson(ex.ToString(), HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
                }
            }
        }

        [HttpPost]
        [Route("verifypay")]
        public async Task<HttpResponseMessage> verifypay(VerifyParameter parameter)
        {
            if (parameter == null || parameter.oid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            var ret = await EsOrderManager.GetByIdAsync(parameter.oid);
            if (ret != null &&
                ((ret.status == (int)EOrderStatus.已支付 || ret.status == (int)EOrderStatus.已成团未提货 ||
                  ret.status == (int)EOrderStatus.已成团未发货)))
            {
                return JsonResponseHelper.HttpRMtoJson(new { isOK = true }, HttpStatusCode.OK, ECustomStatus.Success);
            }

            return JsonResponseHelper.HttpRMtoJson(new { isOK = false }, HttpStatusCode.OK, ECustomStatus.Success);
        }


        [HttpPost]
        [Route("ct")]
        public async Task<HttpResponseMessage> ct(CtParameter parameter)
        {
            if (parameter == null || parameter.fee <= 0 || parameter.goid.Equals(Guid.Empty) ||
              string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid) || parameter.waytoget >= 2 || parameter.uid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!fee:{parameter.fee},goid:{parameter.goid},openid:{parameter.openid},appid:{parameter.appid},waytoget:{parameter.waytoget}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                try
                {
                    #region 判断商家、团状态是否正确，库存及是否有购买限制，用户ip
                    //验证用户是否关注公众号
                    if ((parameter.fee + parameter.post_price) < 500)
                    {
                        if (!await CheckUserSub(parameter.appid, parameter.openid, parameter.goid, "ct"))
                        {
                            return JsonResponseHelper.HttpRMtoJson("UserNotSubscribe",
                                HttpStatusCode.OK, ECustomStatus.UserNotSubscribe);
                        }
                    }

                    //获取商家信息
                    var mer = await repo.GetMerchantByAppidAsync(parameter.appid);
                    if (string.IsNullOrEmpty(mer?.wx_appid) || string.IsNullOrEmpty(mer.wx_mch_id) || string.IsNullOrEmpty(mer.wx_apikey))
                    {
                        return JsonResponseHelper.HttpRMtoJson("mer is null or other errors!",
                            HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                    //获取go信息
                    var go = await repo.GroupOrderGet(parameter.goid);
                    if (go == null || go.status != (int)EGroupOrderStatus.拼团进行中 || go.user_left <= 0)
                        return JsonResponseHelper.HttpRMtoJson($"团状态不对或者人数已经为0,不能参团！",
                            HttpStatusCode.InternalServerError, ECustomStatus.Fail);

                    //获取group信息
                    var group = await repo.GroupGetGroupById(go.gid);
                    if (group == null || group.status != (int)EGroupStatus.已发布)
                        return JsonResponseHelper.HttpRMtoJson(
                            $"group is null or 状态不是 已发布!group status:{group?.status}",
                            HttpStatusCode.InternalServerError, ECustomStatus.Fail);

                    //判断库存（支付成功后减少库存，这里不做锁定，只要有货就能开团）
                    //如果在支付后发现库存被抢光，则退款并提示。
                    //todo:这里会有性能问题，后期改成队列+服务形式。
                    if (group.person_quota > group.product_quota)
                        return JsonResponseHelper.HttpRMtoJson("库存不足，无法参团！", HttpStatusCode.OK, ECustomStatus.Fail);
                    string message = "";
                    if (!MdInventoryHelper.CanOpenGroup(group, parameter.openid, out message))
                        return JsonResponseHelper.HttpRMtoJson(message,
                            HttpStatusCode.OK, ECustomStatus.Fail);

                    //准备预支付
                    //ip
                    string userIp = WXPayHelper.GetClientIp(Request);
                    if (string.IsNullOrEmpty(userIp))
                        throw new MDException(typeof(WechatApiController), "can't get user ip address!");
                    #endregion

                    User_WriteOff uw = new User_WriteOff();
                    UserPost up = new UserPost();
                    if (parameter.waytoget == (int)EWayToGet.自提)
                    {
                        if (string.IsNullOrEmpty(parameter.user_name) || string.IsNullOrEmpty(parameter.tel) || parameter.wopid.Equals(Guid.Empty))
                            return JsonResponseHelper.HttpRMtoJson($"parameter error!user_name:{parameter.user_name},tel:{parameter.tel},wopid:{parameter.wopid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        //更新用户的取货信息
                        uw.cellphone = parameter.tel;
                        uw.uid = parameter.uid;
                        uw.mid = mer.mid;
                        uw.is_default = true;
                        uw.woid = parameter.wopid;
                        uw.user_name = parameter.user_name;
                        await repo.UserWriteOffSaveOrUpdateAsnyc(uw);
                    }
                    else if (parameter.waytoget == (int)EWayToGet.物流)
                    {
                        if (parameter.upid.Equals(Guid.Empty))
                            return JsonResponseHelper.HttpRMtoJson($"parameter error!upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        up = await repo.GetUserPostByUpidAsync(parameter.upid);
                        if (up == null)
                            return JsonResponseHelper.HttpRMtoJson($"up is null!upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        //再根据up获取邮费,验证参数与邮费是否相等
                        var post_price = await EsLogisticsTemplateManager.GetFeeByCode(group.ltid, up.code);
                        if (post_price < 0)
                            return JsonResponseHelper.HttpRMtoJson($"该地址不在配送区域内", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (parameter.post_price != post_price)
                            return JsonResponseHelper.HttpRMtoJson($"parameter error pp:{parameter.post_price}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (up.is_default == false)
                            await repo.UpdateUserPostIs_DefaultAsync(parameter.uid, parameter.upid);//并且把这个地址设置为默认

                    }
                    //判断参数价格是否被修改过
                    //校验
                    if (parameter.fee != group.group_price.Value)
                        return JsonResponseHelper.HttpRMtoJson("价格异常！", HttpStatusCode.OK, ECustomStatus.Fail);
                    //生成订单
                    // 参团：1、判断grouporder的状态与参与人数，2、产生一个Order。order-未支付。
                    // 返回的Tuple中item1-grouporder的Guid,item2为order的Guid。
                    // 修改参团价格为group.group_price，不取parameter.fee  ###2016-8-31
                    var ctResult =
                    await
                        MdOrderBizHelper.CtProcessAsync(go, group, parameter.openid, uw, parameter.fee, parameter.waytoget, up, parameter.post_price);
                    if (ctResult == null)
                    {
                        return JsonResponseHelper.HttpRMtoJson("开团失败!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                    #region 记录分销订单
                    try
                    {
                        if (group.Commission > 0 && !string.IsNullOrEmpty(parameter.shareopenid) && group.group_type == (int)EGroupTypes.普通团)
                        {
                            var shareUser = await EsUserManager.GetByOpenIdAsync(parameter.shareopenid);
                            if (shareUser != null && await repo.WoerCanWriteOff(mer.mid, Guid.Parse(shareUser.Id)))
                            {
                                var distribution = repo.GetDistributionByOid(ctResult.oid);//判断该订单是否有分销记录
                                if (distribution == null || distribution.oid.Equals(Guid.Empty))
                                {
                                    Distribution dis = new Distribution()
                                    {
                                        oid = ctResult.oid,
                                        mid = mer.mid,
                                        gid = group.gid,
                                        buyer = ctResult.buyer,
                                        sharer = Guid.Parse(shareUser.Id),
                                        commission = group.Commission,
                                        isptsucceed = 0,
                                        lastupdatetime = CommonHelper.GetUnixTimeNow(),
                                        sourcetype = (int)EDisSourcetype.订单佣金,
                                        last_commission = 0,
                                        finally_commission = 0
                                    };
                                    await repo.AddDistributionAsync(dis);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(WechatGroupController), new Exception($"参团增加分销数据错误,ex:{ex.Message}"));
                    }
                    #endregion
                    string out_trade_no = ctResult.o_no;

                    //预支付
                    var tuple = await WXPayHelper.GZHPay_GenPrePayCode(parameter.openid, parameter.appid, mer.wx_mch_id, group.title, parameter.fee + parameter.post_price, out_trade_no,
                        mer.wx_apikey, userIp);

                    if (tuple == null)
                        throw new MDException(typeof(wxpayController), $"预支付函数调用失败！appid:{parameter.appid}");

                    //统计更新
                    await RedisMerchantStatisticsOp.AfterCtAsync(mer.mid.ToString());
                    if (parameter.longitude != 0 && parameter.latitude != 0)
                    {
                        EsBizLogStatistics.AddPayBizViewLog(group.gid.ToString(), parameter.openid, parameter.uid, mer.mid, parameter.longitude, parameter.latitude);
                    }
                    return JsonResponseHelper.HttpRMtoJson(
                        new
                        {
                            appId = parameter.appid,
                            timeStamp = tuple.Item3,
                            nonceStr = tuple.Item4,
                            package = string.Format("prepay_id={0}", tuple.Item1),
                            paySign = tuple.Item2,
                            oid = ctResult.oid,
                            goid = go.goid
                        }, HttpStatusCode.OK, ECustomStatus.Success

                        );
                }
                catch (PrePayDuplicatedException)
                {
                    return JsonResponseHelper.HttpRMtoJson("您的订单已经生效，请不要重复下单！", HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Length > 100)
                    {
                        return JsonResponseHelper.HttpRMtoJson("服务器异常，请刷新页面然后重试！", HttpStatusCode.InternalServerError,
ECustomStatus.Fail);
                    }
                    return JsonResponseHelper.HttpRMtoJson(ex.ToString(), HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
                }
            }
        }

        [HttpPost]
        [Route("luckykt")]
        public async Task<HttpResponseMessage> luckykt(KtParameter parameter)
        {
            if (parameter == null || parameter.fee <= 0 || parameter.gid.Equals(Guid.Empty) ||
               string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid) || parameter.waytoget >= 2 || parameter.uid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!fee:{parameter.fee},gid:{parameter.gid},openid:{parameter.openid},appid:{parameter.appid},waytoget:{parameter.waytoget}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                try
                {
                    #region 判断商家、团状态是否正确，库存及是否有购买限制，用户ip
                    //验证用户是否关注公众号
                    if ((parameter.fee + parameter.post_price) < 500)
                    {
                        if (!await CheckUserSub(parameter.appid, parameter.openid, parameter.gid, "kt"))
                        {
                            return JsonResponseHelper.HttpRMtoJson("UserNotSubscribe",
                                HttpStatusCode.OK, ECustomStatus.UserNotSubscribe);
                        }
                    }
                    //获取商家信息

                    //WatchStopper ws = new WatchStopper(typeof(WechatGroupController),"KT计时！");

                    //ws.Restart("1-获取mer");
                    var mer = await repo.GetMerchantByAppidAsync(parameter.appid);
                    if (mer == null || string.IsNullOrEmpty(mer.wx_appid) || string.IsNullOrEmpty(mer.wx_mch_id) || string.IsNullOrEmpty(mer.wx_apikey))
                    {
                        return JsonResponseHelper.HttpRMtoJson("mer is null or other errors!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                    //ws.Stop();
                    //////


                    //获取团信息
                    //ws.Restart("3-获取团信息！");

                    var group = await repo.GroupGetGroupById(parameter.gid);
                    if (group == null)
                        return JsonResponseHelper.HttpRMtoJson("group is null or other errors!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    //判断group的waytoget是否与参数匹配
                    if (group.waytoget != (int)EGroupWaytoget.自提或快递到家)//两者都是就不管
                    {
                        if (group.waytoget != parameter.waytoget)
                            return JsonResponseHelper.HttpRMtoJson("group waytoget errors!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                    //判断抽奖团是否已经结束
                    double nowTime = CommonHelper.GetUnixTimeNow();
                    if (group.lucky_count <= 0 || Convert.ToInt32(group.lucky_endTime) < nowTime || group.lucky_status != (int)EGroupLuckyStatus.待开奖)
                        return JsonResponseHelper.HttpRMtoJson("该团已过期，无法开团！", HttpStatusCode.InternalServerError, ECustomStatus.Fail);

                    //判断库存（支付成功后减少库存，这里不做锁定，只要有货就能开团）
                    //如果在支付后发现库存被抢光，则退款并提示。
                    //todo:这里会有性能问题，后期改成队列+服务形式。
                    if (group.person_quota > group.product_quota)
                        return JsonResponseHelper.HttpRMtoJson("库存不足，无法开团！", HttpStatusCode.OK, ECustomStatus.Fail);
                    string message = "";
                    if (!MdInventoryHelper.CanOpenGroup(group, parameter.openid, out message))
                        return JsonResponseHelper.HttpRMtoJson(message, HttpStatusCode.OK, ECustomStatus.Fail);
                    //ws.Stop();
                    //准备预支付

                    string userIp = WXPayHelper.GetClientIp(Request);
                    if (string.IsNullOrEmpty(userIp))
                        throw new MDException(typeof(WechatApiController), "can't get user ip address!");
                    #endregion


                    User_WriteOff uw = new User_WriteOff();
                    UserPost up = new UserPost();
                    if (parameter.waytoget == (int)EWayToGet.自提)
                    {
                        if (string.IsNullOrEmpty(parameter.user_name) || string.IsNullOrEmpty(parameter.tel) || parameter.wopid.Equals(Guid.Empty))
                            return JsonResponseHelper.HttpRMtoJson($"parameter error!user_name:{parameter.user_name},tel:{parameter.tel},wopid:{parameter.wopid}", HttpStatusCode.OK, ECustomStatus.Fail);

                        uw.cellphone = parameter.tel;
                        uw.uid = parameter.uid;
                        uw.mid = mer.mid;
                        uw.is_default = true;
                        uw.woid = parameter.wopid;
                        uw.user_name = parameter.user_name;
                        await repo.UserWriteOffSaveOrUpdateAsnyc(uw);
                    }
                    else if (parameter.waytoget == (int)EWayToGet.物流)
                    {
                        if (parameter.upid.Equals(Guid.Empty))
                            return JsonResponseHelper.HttpRMtoJson($"parameter error!upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        up = await repo.GetUserPostByUpidAsync(parameter.upid);
                        if (up == null)
                            return JsonResponseHelper.HttpRMtoJson($"up is null!upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        //再根据up获取邮费,验证参数与邮费是否相等
                        var post_price = await EsLogisticsTemplateManager.GetFeeByCode(group.ltid, up.code);
                        if (post_price < 0)
                            return JsonResponseHelper.HttpRMtoJson($"该地址不在配送区域内", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (parameter.post_price != post_price)
                            return JsonResponseHelper.HttpRMtoJson($"parameter error pp:{parameter.post_price}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (up.is_default == false)
                            await repo.UpdateUserPostIs_DefaultAsync(parameter.uid, parameter.upid);//并且把这个地址设置为默认

                    }
                    //判断参数价格是否被修改过
                    if (parameter.fee != Convert.ToInt32(group.group_price) - Convert.ToInt32(group.leader_price))
                        return JsonResponseHelper.HttpRMtoJson($"价格输入错误", HttpStatusCode.OK, ECustomStatus.Fail);


                    //生成订单
                    // 开团：1、产生一个GroupOrder，2、产生一个Order。状态是：GroupOrder：开团中，Order-未支付。
                    // 返回的Tuple中item1-grouporder的Guid,item2为order的Guid。

                    //ws.Restart("4-生成订单！");
                    //修改开团价格为group.group_price-group.leader_price，不取parameter.fee  ###2016-8-31
                    var ktResult =
                        await
                            MdOrderBizHelper.KtProcessAsync(group, parameter.openid, uw, Convert.ToInt32(group.group_price) - Convert.ToInt32(group.leader_price), parameter.waytoget, up, parameter.post_price);

                    if (ktResult == null)
                    {
                        return JsonResponseHelper.HttpRMtoJson("开团失败!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                    string out_trade_no = ktResult.Item2.o_no;
                    //ws.Stop();

                    //预支付
                    //ws.Restart("5-预支付");
                    var tuple =
                        await
                             WXPayHelper.GZHPay_GenPrePayCode(parameter.openid, parameter.appid, mer.wx_mch_id,
                                group.title, parameter.fee + parameter.post_price, out_trade_no,
                                mer.wx_apikey, userIp);

                    if (tuple == null)
                        throw new MDException(typeof(wxpayController), $"预支付函数调用失败！appid:{parameter.appid}");

                    //更新统计
                    await RedisMerchantStatisticsOp.AfterKtAsync(mer.mid.ToString());
                    if (parameter.longitude != 0 && parameter.latitude != 0)
                    {
                        EsBizLogStatistics.AddPayBizViewLog(parameter.gid.ToString(), parameter.openid, parameter.uid, mer.mid, parameter.longitude, parameter.latitude);
                    }
                    //ws.Stop();
                    return JsonResponseHelper.HttpRMtoJson(
                        new
                        {
                            appId = parameter.appid,
                            timeStamp = tuple.Item3,
                            nonceStr = tuple.Item4,
                            package = string.Format("prepay_id={0}", tuple.Item1),
                            paySign = tuple.Item2,
                            oid = ktResult.Item2.oid,
                            goid = ktResult.Item1.goid
                        }, HttpStatusCode.OK, ECustomStatus.Success

                        );
                }
                catch (PrePayDuplicatedException)
                {
                    return JsonResponseHelper.HttpRMtoJson("您的订单已经生效，请不要重复下单！", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
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

        [HttpPost]
        [Route("luckyct")]
        public async Task<HttpResponseMessage> luckyct(CtParameter parameter)
        {
            if (parameter == null || parameter.fee <= 0 || parameter.goid.Equals(Guid.Empty) ||
               string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid) || parameter.waytoget >= 2 || parameter.uid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!fee:{parameter.fee},goid:{parameter.goid},openid:{parameter.openid},appid:{parameter.appid},waytoget:{parameter.waytoget}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var repo = new BizRepository())
            {
                try
                {
                    #region 判断商家、团状态是否正确，库存及是否有购买限制，用户ip
                    //验证用户是否关注公众号
                    if ((parameter.fee + parameter.post_price) < 500)
                    {
                        if (!await CheckUserSub(parameter.appid, parameter.openid, parameter.goid, "ct"))
                        {
                            return JsonResponseHelper.HttpRMtoJson("UserNotSubscribe",
                                HttpStatusCode.OK, ECustomStatus.UserNotSubscribe);
                        }
                    }
                    //获取商家信息
                    var mer = await repo.GetMerchantByAppidAsync(parameter.appid);
                    if (string.IsNullOrEmpty(mer?.wx_appid) || string.IsNullOrEmpty(mer.wx_mch_id) || string.IsNullOrEmpty(mer.wx_apikey))
                    {
                        return JsonResponseHelper.HttpRMtoJson("mer is null or other errors!",
                            HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                    //获取go信息
                    var go = await repo.GroupOrderGet(parameter.goid);
                    if (go == null || go.status != (int)EGroupOrderStatus.拼团进行中 || go.user_left <= 0)
                        return JsonResponseHelper.HttpRMtoJson($"团状态不对或者人数已经为0,不能参团！",
                            HttpStatusCode.InternalServerError, ECustomStatus.Fail);

                    //获取group信息
                    var group = await repo.GroupGetGroupById(go.gid);
                    if (group == null || group.status != (int)EGroupStatus.已发布)
                        return JsonResponseHelper.HttpRMtoJson(
                            $"group is null or 状态不是 已发布!group status:{group?.status}",
                            HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    //判断group的waytoget是否与参数匹配
                    if (group.waytoget != (int)EGroupWaytoget.自提或快递到家)//两者都是就不管
                    {
                        if (group.waytoget != parameter.waytoget)
                            return JsonResponseHelper.HttpRMtoJson("group waytoget errors!", HttpStatusCode.InternalServerError, ECustomStatus.Fail);
                    }
                    //判断抽奖团是否已经结束
                    double nowTime = CommonHelper.GetUnixTimeNow();
                    if (group.lucky_count <= 0 || Convert.ToInt32(group.lucky_endTime) < nowTime || group.lucky_status != (int)EGroupLuckyStatus.待开奖)
                        return JsonResponseHelper.HttpRMtoJson("该团已过期，无法参团！", HttpStatusCode.InternalServerError, ECustomStatus.Fail);

                    //判断库存（支付成功后减少库存，这里不做锁定，只要有货就能开团）
                    //如果在支付后发现库存被抢光，则退款并提示。
                    //todo:这里会有性能问题，后期改成队列+服务形式。
                    if (group.person_quota > group.product_quota)
                        return JsonResponseHelper.HttpRMtoJson("库存不足，无法参团！", HttpStatusCode.OK, ECustomStatus.Fail);
                    string message = "";
                    if (!MdInventoryHelper.CanOpenGroup(group, parameter.openid,out message))
                        return JsonResponseHelper.HttpRMtoJson(message,
                            HttpStatusCode.OK, ECustomStatus.Fail);

                    //准备预支付
                    //校验
                    if (parameter.fee != group.group_price.Value)
                        return JsonResponseHelper.HttpRMtoJson("价格异常！",
                            HttpStatusCode.OK, ECustomStatus.Fail);
                    //ip
                    string userIp = WXPayHelper.GetClientIp(Request);
                    if (string.IsNullOrEmpty(userIp))
                        throw new MDException(typeof(WechatApiController), "can't get user ip address!");
                    #endregion

                    User_WriteOff uw = new User_WriteOff();
                    UserPost up = new UserPost();
                    if (parameter.waytoget == (int)EWayToGet.自提)
                    {
                        if (string.IsNullOrEmpty(parameter.user_name) || string.IsNullOrEmpty(parameter.tel) || parameter.wopid.Equals(Guid.Empty))
                            return JsonResponseHelper.HttpRMtoJson($"parameter error!user_name:{parameter.user_name},tel:{parameter.tel},wopid:{parameter.wopid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        //更新用户的取货信息
                        uw.cellphone = parameter.tel;
                        uw.uid = parameter.uid;
                        uw.mid = mer.mid;
                        uw.is_default = true;
                        uw.woid = parameter.wopid;
                        uw.user_name = parameter.user_name;
                        await repo.UserWriteOffSaveOrUpdateAsnyc(uw);
                    }
                    else if (parameter.waytoget == (int)EWayToGet.物流)
                    {
                        if (parameter.upid.Equals(Guid.Empty))
                            return JsonResponseHelper.HttpRMtoJson($"parameter error!upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        up = await repo.GetUserPostByUpidAsync(parameter.upid);
                        if (up == null)
                            return JsonResponseHelper.HttpRMtoJson($"up is null!upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        //再根据up获取邮费,验证参数与邮费是否相等
                        var post_price = await EsLogisticsTemplateManager.GetFeeByCode(group.ltid, up.code);
                        if (post_price < 0)
                            return JsonResponseHelper.HttpRMtoJson($"该地址不在配送区域内", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (parameter.post_price != post_price)
                            return JsonResponseHelper.HttpRMtoJson($"parameter error pp:{parameter.post_price}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (up.is_default == false)
                            await repo.UpdateUserPostIs_DefaultAsync(parameter.uid, parameter.upid);//并且把这个地址设置为默认
                    }

                    //生成订单
                    // 参团：1、判断grouporder的状态与参与人数，2、产生一个Order。order-未支付。
                    // 返回的Tuple中item1-grouporder的Guid,item2为order的Guid。
                    // 修改参团价格为group.group_price，不取parameter.fee  ###2016-8-31
                    var ctResult =
                          await
                        MdOrderBizHelper.CtProcessAsync(go, group, parameter.openid, uw, parameter.fee, parameter.waytoget, up, parameter.post_price);
                    if (ctResult == null)
                    {
                        return JsonResponseHelper.HttpRMtoJson("开团失败!", HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
                    }
                    string out_trade_no = ctResult.o_no;

                    //预支付
                    var tuple = await WXPayHelper.GZHPay_GenPrePayCode(parameter.openid, parameter.appid, mer.wx_mch_id, group.title, parameter.fee + parameter.post_price, out_trade_no,
                        mer.wx_apikey, userIp);

                    if (tuple == null)
                        throw new MDException(typeof(wxpayController), $"预支付函数调用失败！appid:{parameter.appid}");

                    //统计更新
                    await RedisMerchantStatisticsOp.AfterCtAsync(mer.mid.ToString());
                    if (parameter.longitude != 0 && parameter.latitude != 0)
                    {
                        EsBizLogStatistics.AddPayBizViewLog(group.gid.ToString(), parameter.openid, parameter.uid, mer.mid, parameter.longitude, parameter.latitude);
                    }
                    return JsonResponseHelper.HttpRMtoJson(
                        new
                        {
                            appId = parameter.appid,
                            timeStamp = tuple.Item3,
                            nonceStr = tuple.Item4,
                            package = string.Format("prepay_id={0}", tuple.Item1),
                            paySign = tuple.Item2,
                            oid = ctResult.oid,
                            goid = go.goid
                        }, HttpStatusCode.OK, ECustomStatus.Success

                        );
                }
                catch (PrePayDuplicatedException)
                {
                    return JsonResponseHelper.HttpRMtoJson("您的订单已经生效，请不要重复下单！", HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Length > 100)
                    {
                        return JsonResponseHelper.HttpRMtoJson("服务器异常，请刷新页面然后重试！", HttpStatusCode.InternalServerError,
ECustomStatus.Fail);
                    }
                    return JsonResponseHelper.HttpRMtoJson(ex.ToString(), HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
                }
            }
        }

        [HttpPost]
        [Route("groupshare")]
        public HttpResponseMessage GroupShare(ShareParameter parameter)
        {
            if (parameter == null || parameter.gid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || parameter.uid.Equals(Guid.Empty) || parameter.mid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!openid:{parameter.openid},gid:{parameter.gid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            if (Enum.IsDefined(typeof(ShareType), parameter.message))
            {
                EsBizLogStatistics.AddGroupShareViewLog(parameter.gid.ToString(), parameter.openid, parameter.uid, parameter.message.ToString(), parameter.mid.ToString());
                return JsonResponseHelper.HttpRMtoJson("Success", HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson($"parameter error!message:{parameter.message}", HttpStatusCode.OK, ECustomStatus.Fail);
        }

        /// <summary>
        /// 验证用户是否已关注公众号，true关注，false未关注
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="openid"></param>
        /// <param name="goid"></param>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取可分销的团
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getcommissiongroup")]
        public async Task<HttpResponseMessage> GetCommissionGroup(GroupParameter parameter)
        {
            if (parameter == null || parameter.pageIndex < 1 || parameter.mid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var mer = await repo.GetMerchantByMidAsync(parameter.mid);
                if (mer == null)
                    return JsonResponseHelper.HttpRMtoJson($"mer is null!", HttpStatusCode.OK, ECustomStatus.Fail);
                var iswriteoffer = await repo.WoerCanWriteOff(parameter.mid, parameter.uid);
                if (!iswriteoffer)
                    return JsonResponseHelper.HttpRMtoJson($"您不是核销员，无法查看!", HttpStatusCode.OK, ECustomStatus.Fail);
                int pageSize = MdWxSettingUpHelper.GetPageSize();
                List<object> retobj = new List<object>();
                var tuple = await EsGroupManager.GetFXGroupByMidAsync(parameter.mid, new List<int>() { (int)EGroupStatus.已发布 }, parameter.pageIndex, pageSize);
                if (tuple != null)
                {
                    int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
                    foreach (var group in tuple.Item2)
                    {
                        retobj.Add(new
                        {
                            group.advertise_pic_url,
                            group.title,
                            commission = group.commission / 100.00,
                            group.person_quota,
                            group_price = group.group_price / 100.00,
                            gid = group.Id,
                        });
                    }
                    return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
                }
                return JsonResponseHelper.HttpRMtoJson(null, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        [HttpPost]
        [Route("getGrouptuiguangimg")]
        public async Task<HttpResponseMessage> GetGroupTuiGuangImg(GroupParameter parameter)
        {
            if (parameter == null || parameter.uid.Equals(Guid.Empty) || parameter.gid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var group = await repo.GroupGetGroupById(parameter.gid);
                var mer = await repo.GetMerchantByMidAsync(group.mid);
                if (group == null || mer == null)
                    return JsonResponseHelper.HttpRMtoJson($"group or mer is null!", HttpStatusCode.OK, ECustomStatus.Fail);
                var writeoffer = repo.WoerCanWriteOff_TB(group.mid, parameter.uid);
                if (writeoffer == null)
                    return JsonResponseHelper.HttpRMtoJson($"writeoffer or mer is null!", HttpStatusCode.OK, ECustomStatus.Fail);
                var filename = GenImage(group, mer, writeoffer.openid);
                if (!string.IsNullOrEmpty(filename))
                    filename = filename.Replace("~", "");//去掉~符号
                return JsonResponseHelper.HttpRMtoJson(filename, HttpStatusCode.OK, ECustomStatus.Success);
            }

        }
        private string GenImage(Group group, Merchant mer, string shareopenid)
        {
            string tuiguangImg = "~/Content/MediaFiles/" + group.gid.ToString() + "/";
            string filePath = Server.GetMapPath(tuiguangImg);
            string filename = shareopenid + ".jpg";
            if (File.Exists(filePath + filename))
                return tuiguangImg + filename;
            Image img = Image.FromFile(Server.GetMapPath("~/Content/images/ad002.jpg"));
            //生成随机的背景图
            Bitmap backgroundimg = new Bitmap(img);
            if (group == null || mer == null)
                return "";
            Graphics g = Graphics.FromImage(backgroundimg);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            //加Logo
            Uri uriLogo = new Uri(mer.logo_url);
            WebRequest webRequest = WebRequest.Create(uriLogo);
            WebResponse webResponse = webRequest.GetResponse();
            Bitmap bitLogo = new Bitmap(webResponse.GetResponseStream());
            g.DrawImage(bitLogo, 25, 25, 110, 110);
            //加商家名称
            g.DrawString(mer.name, getFont(36), new SolidBrush(Color.Black), new PointF(145, 60));
            //加活动标题
            g.DrawString(group.title, getFont(36), new SolidBrush(Color.White), new RectangleF(new PointF(25, 170), new SizeF(700, 95)));
            //加活动主图片
            Uri uripic1 = new Uri(group.advertise_pic_url);
            WebRequest webRequest2 = WebRequest.Create(uripic1);
            WebResponse webResponse2 = webRequest2.GetResponse();
            Bitmap bitpic1 = new Bitmap(webResponse2.GetResponseStream());
            g.DrawImage(bitpic1, 35, 300, 680, 450);
            //加价格和介绍
            g.DrawString("原价：" + (group.origin_price.Value / 100.00).ToString("f2") + "元    拼团价：", getFont(32), new SolidBrush(Color.Black), new PointF(35, 755));
            g.DrawString("" + (group.group_price.Value / 100.00).ToString("f2") + "元", getFont(40), new SolidBrush(Color.Red), new PointF(435, 745));
            g.DrawString(group.description.Replace("\n\r", "  "), getFont(24), new SolidBrush(Color.Gray), new RectangleF(new PointF(35, 800), new SizeF(700, 125)));
            g.DrawLine(new Pen(Color.Black), new PointF(35, 780), new PointF(270, 780));
            //活动二维码
            string groupCode = MdWxSettingUpHelper.GenGroupDetailUrl_fx(mer.wx_appid, group.gid, shareopenid);
            var bitcode = ImageHelper.GenQr_Code(groupCode, 292, 292);
            g.DrawImage(bitcode, 230, 958, bitcode.Width, bitcode.Height);
            g.Dispose();

            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
            backgroundimg.Save(filePath + filename);
            return tuiguangImg + filename;
        }
        private Font getFont(int size)
        {
            FontFamily fm = new FontFamily("微软雅黑");
            Font font = new Font(fm, size, FontStyle.Regular, GraphicsUnit.Pixel);
            return font;
        }
    }
}
