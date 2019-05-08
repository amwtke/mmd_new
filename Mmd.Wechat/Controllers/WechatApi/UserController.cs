using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using MD.Lib.DB.Redis;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.WeChat.Filters;
using MD.Lib.DB.Repositorys;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.User;
using MD.Lib.DB.Redis.MD;
using MD.Model.DB;
using MD.Lib.Weixin.Component;
using MD.Lib.Log;
using MD.Model.DB.Code;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB.Professional;
using MD.Model.Index.MD;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/user")]
    [AccessFilter]
    public class UserController : ApiController
    {
        [HttpPost]
        [Route("getinfo")]
        public async Task<HttpResponseMessage> getinfo(BaseParameter postParameter)
        {
            if (postParameter.uid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,uid is empty!", HttpStatusCode.OK, ECustomStatus.Fail);

            var user = await EsUserManager.GetByIdAsync(postParameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"uid;{postParameter.uid}的用户找不到！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid mid = Guid.Parse(user.mid);
            UserInfoRedis userRedis =
                await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(user.openid);
            if (string.IsNullOrEmpty(userRedis.Uid) || !userRedis.Uid.Equals(postParameter.uid.ToString()))
            {
                return JsonResponseHelper.HttpRMtoJson($"openid:{user.openid}的用户在r中找不到！", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var reop = new BizRepository())
            {
                using (var codereop = new CodeRepository())
                {
                    int age = 0;
                    if (user.age > 0)
                    {
                        DateTime birthday = CommonHelper.FromUnixTime(user.age);
                        if (birthday < DateTime.Now)
                        {
                            age = DateTime.Now.Year - birthday.Year;
                        }
                    }
                    var iswriteoffer = await reop.WoerCanWriteOff(mid, postParameter.uid);//是否核销员
                    int communityImgCount = 0;
                    int communityPraises = 0;
                    //社区照片总数和点赞总数
                    var community = await EsCommunityManager.GetCountAsync(mid, postParameter.uid, (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
                    if (community != null)
                    {
                        communityImgCount = community.Item1;
                        communityPraises = community.Item2;
                    }
                    //我的通知
                    long myMessageCount = 0;
                    var tupleMessage = await EsCommunityBizManager.GetByAggBizTypeAsync(mid, postParameter.uid, 0, null);
                    if (tupleMessage != null && tupleMessage.Item1 > 0)
                    {
                        myMessageCount = tupleMessage.Item2.Sum(p => p.DocCount);
                    }
                    //我关注的人数
                    int myGzCounts = 0;
                    var tuple1 = await EsCommunityBizManager.GetCommunityBizListAsync(Guid.Empty, Guid.Empty, postParameter.uid, Guid.Empty, (int)EComBizType.Subscribe, 1, 1);
                    if (tuple1 != null)
                        myGzCounts = tuple1.Item1;
                    //我的粉丝人数
                    int myFansCounts = 0;
                    var tuple2 = await EsCommunityBizManager.GetCommunityBizListAsync(Guid.Empty, postParameter.uid, Guid.Empty, Guid.Empty, (int)EComBizType.Subscribe, 1, 1);
                    if (tuple2 != null)
                        myFansCounts = tuple2.Item1;
                    var retobj =
                            new
                            {
                                headpic = userRedis.HeadImgUrl,
                                name = userRedis.NickName,
                                age,
                                age_timestamp = user.age,
                                skin = await codereop.GetCodeSkin(user.skin),
                                skincode = user.skin,
                                qmCount = await RedisVectorOp.GetQMCount(postParameter.uid),
                                iswriteoffer,
                                communityImgCount,
                                communityPraises,
                                myGzCounts,
                                myFansCounts,
                                myMessageCount
                            };
                    return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
        }

        [HttpPost]
        [Route("getinfobyopenid")]
        public async Task<HttpResponseMessage> getinfoByopenid(BaseParameter postParameter)
        {
            if (string.IsNullOrEmpty(postParameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,openid is empty!", HttpStatusCode.OK, ECustomStatus.Fail);

            var user = await EsUserManager.GetByOpenIdAsync(postParameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"uid;{postParameter.uid}的用户找不到！", HttpStatusCode.OK, ECustomStatus.Fail);

            UserInfoRedis userRedis =
                await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(user.openid);
            if (string.IsNullOrEmpty(userRedis.Uid) || !userRedis.Uid.Equals(user.Id))
            {
                return JsonResponseHelper.HttpRMtoJson($"openid:{user.openid}的用户在r中找不到！", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            var retobj =
                    new
                    {
                        headpic = userRedis.HeadImgUrl,
                        name = userRedis.NickName,
                        uid = user.Id
                    };
            return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getallskin")]
        public async Task<HttpResponseMessage> GetAllSkin(BaseParameter parameter)
        {
            using (var codebiz = new CodeRepository())
            {
                var skinobj = await codebiz.GetAllCodeSkinAsync();//肤质字典
                return JsonResponseHelper.HttpRMtoJson(skinobj.ToList(), HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        [HttpPost]
        [Route("setuserageandskin")]
        public async Task<HttpResponseMessage> SetUserAgeAndSkin(UserParameter parameter)
        {
            if (parameter == null || parameter.uid.Equals(Guid.Empty) || parameter.age <= 0 || parameter.skinCode < 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error,uid:{parameter.uid},age:{parameter.age}.skinCode:{parameter.skinCode}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByIdAsync(parameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"Es user is null,uid:{parameter.uid}!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var u = await repo.UpdateUserAgeAndSkinAsync(parameter.uid, parameter.age, parameter.skinCode);
                if (u != null)
                {
                    await EsUserManager.AddOrUpdateAsync(EsUserManager.GenObject(u));
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { isOk = true }, HttpStatusCode.OK, ECustomStatus.Success);
        }
        [HttpPost]
        [Route("getmypostlist")]
        public async Task<HttpResponseMessage> GetMyPostList(UserParameter parameter)
        {
            if (parameter == null || parameter.uid.Equals(Guid.Empty) || parameter.gid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,uid:{parameter.uid},gid:{parameter.gid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByIdAsync(parameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"Es user is null,uid:{parameter.uid}!", HttpStatusCode.OK, ECustomStatus.Fail);
            var group = await EsGroupManager.GetByGidAsync(parameter.gid);
            if (group == null)
                return JsonResponseHelper.HttpRMtoJson($"Es group is null,gid:{parameter.gid}!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                List<object> retobj = new List<object>();
                var postlist = await repo.GetUserPostByUidAsync(parameter.uid, false);
                foreach (var up in postlist)
                {
                    var post_price = await EsLogisticsTemplateManager.GetFeeByCode(Guid.Parse(group.ltid), up.code);
                    retobj.Add(new
                    {
                        up.upid,
                        up.uid,
                        up.name,
                        up.cellphone,
                        up.province,
                        up.city,
                        up.district,
                        up.code,
                        up.address,
                        up.is_default,
                        post_price = post_price > 0 ? post_price / 100.00 : post_price
                    });
                }
                return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }
        [HttpPost]
        [Route("getmypostdetail")]
        public async Task<HttpResponseMessage> GetMyPostDetail(UserParameter parameter)
        {
            if (parameter == null || parameter.upid.Equals(Guid.Empty) || parameter.gid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,upid:{parameter.upid},gid:{parameter.gid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var group = await EsGroupManager.GetByGidAsync(parameter.gid);
            if (group == null)
                return JsonResponseHelper.HttpRMtoJson($"Es group is null,gid:{parameter.gid}!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var up = await repo.GetUserPostByUpidAsync(parameter.upid);
                if (up != null)
                {
                    var post_price = await EsLogisticsTemplateManager.GetFeeByCode(Guid.Parse(group.ltid), up.code);
                    var retobj = new
                    {
                        up.upid,
                        up.uid,
                        up.name,
                        up.cellphone,
                        up.province,
                        provinceCode = up.code.Substring(0, 3),
                        up.city,
                        cityCode = up.code.Substring(0, 6),
                        up.district,
                        districtCode = up.code,
                        up.address,
                        up.is_default,
                        post_price = post_price > 0 ? post_price / 100.00 : post_price
                    };
                    return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
                }
                return JsonResponseHelper.HttpRMtoJson(up, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        [HttpPost]
        [Route("addorupdatemypost")]
        public async Task<HttpResponseMessage> AddorEditMyPost(UserParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.name) || string.IsNullOrEmpty(parameter.cellphone) ||
                string.IsNullOrEmpty(parameter.province) || string.IsNullOrEmpty(parameter.city) || string.IsNullOrEmpty(parameter.district) ||
                string.IsNullOrEmpty(parameter.districtcode) || parameter.districtcode.Length != 9 || string.IsNullOrEmpty(parameter.address))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,openid:{parameter.openid},name:{parameter.name},cellphone:{parameter.cellphone},districtcode:{parameter.districtcode},address:{parameter.address},is_default:{parameter.is_default},province:{parameter.province},city:{parameter.city},district:{parameter.district}",
                    HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"Es user is null,openid:{parameter.openid}!", HttpStatusCode.OK, ECustomStatus.Fail);
            var currentUid = Guid.Parse(user.Id);
            UserPost userpost = new UserPost();
            using (var reop = new BizRepository())
            {
                if (parameter.upid.Equals(Guid.Empty))
                {
                    userpost.upid = Guid.NewGuid();
                    userpost.createtime = CommonHelper.GetUnixTimeNow();
                }
                else
                {
                    var up = await reop.GetUserPostByUpidAsync(parameter.upid);
                    if (up == null)
                        return JsonResponseHelper.HttpRMtoJson($"up is null,upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
                    userpost.upid = up.upid;
                    userpost.createtime = up.createtime;
                }
                userpost.uid = currentUid;
                userpost.name = parameter.name;
                userpost.cellphone = parameter.cellphone;
                userpost.province = parameter.province;
                userpost.city = parameter.city;
                userpost.district = parameter.district;
                userpost.code = parameter.districtcode;
                userpost.address = parameter.address;
                userpost.is_default = parameter.is_default;

                if (await reop.AddOrUpdateUserPostAsync(userpost))
                    return JsonResponseHelper.HttpRMtoJson(new { isOk = true, upid = userpost.upid }, HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson(new { isOk = false }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("delmypost")]
        public async Task<HttpResponseMessage> DelMyPost(UserParameter parameter)
        {
            if (parameter == null || parameter.upid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,upid:{parameter.upid}", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                if (await repo.DelUserPostAsync(parameter.upid))
                    return JsonResponseHelper.HttpRMtoJson(new { isOk = true }, HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson(new { isOk = false }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getusersubscribe")]
        public async Task<HttpResponseMessage> GetUserSubscribe(UserParameter parameter)
        {
            try
            {
                if (parameter == null || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid))
                    return JsonResponseHelper.HttpRMtoJson($"parameter error", HttpStatusCode.OK, ECustomStatus.Fail);
                string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(parameter.appid);
                var user = await Senparc.Weixin.MP.AdvancedAPIs.UserApi.InfoAsync(at, parameter.openid);
                bool IsUserSub = user?.subscribe == 1;
                return JsonResponseHelper.HttpRMtoJson(new { isOk = IsUserSub }, HttpStatusCode.OK, ECustomStatus.Success);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(WechatGroupController), new Exception("CheckUserSub获取微信用户信息失败,appid:" + parameter.appid + ",openid:" + parameter.openid + "," + ex));
                return JsonResponseHelper.HttpRMtoJson(new { isOk = false }, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        /// <summary>
        /// 我的核销记录
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getmywriteofflog")]
        public async Task<HttpResponseMessage> GetMyWriteOffLog(UserParameter parameter)
        {
            if (parameter == null || parameter.uid.Equals(Guid.Empty) || parameter.pageIndex <= 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByIdAsync(parameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"uid;{parameter.uid}的用户找不到！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentMid = Guid.Parse(user.mid);
            int pageSize = MdWxSettingUpHelper.GetPageSize();
            string begindate = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd");
            string enddate = DateTime.Now.ToString("yyyy-MM-dd");
            string qdate = begindate + " - " + enddate;
            var tuple = await EsOrderManager.SearchAsnyc2(null, parameter.uid, qdate, "", currentMid, (int)EWayToGet.自提, (int)EOrderStatus.拼团成功, parameter.pageIndex, pageSize);
            List<object> retobj = new List<object>();
            using (var repo = new BizRepository())
            {
                if (tuple != null)
                {
                    int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
                    foreach (var order in tuple.Item2)
                    {
                        var group = await EsGroupManager.GetByGidAsync(Guid.Parse(order.gid));
                        if (group == null)
                            continue;
                        string buyname = order.name;
                        string buycellphone = order.cellphone;
                        if (string.IsNullOrEmpty(buyname) || string.IsNullOrEmpty(buycellphone))
                        {
                            var user_wo = await repo.UserWriteoffGetByMidAndUidAsync(currentMid, Guid.Parse(order.buyer));
                            if (user_wo != null)
                            {
                                buyname = user_wo.user_name;
                                buycellphone = user_wo.cellphone;
                            }
                        }
                        retobj.Add(new
                        {
                            writeoffday=(int)order.writeoffday,
                            group.title,
                            order.o_no,
                            buyname,
                            buycellphone
                        });
                    }
                    return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, olist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
            return JsonResponseHelper.HttpRMtoJson(null, HttpStatusCode.OK, ECustomStatus.Success);
        }

        #region 核销员列表
        /// <summary>
        /// 核销员列表
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        //[HttpPost]
        //[Route("getwriteofflist")]
        //public async Task<HttpResponseMessage> GetWriteOffList(UserParameter parameter)
        //{
        //    if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.pageIndex <= 0 || parameter.pageSize <= 0)
        //        return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
        //    using (var repo = new BizRepository())
        //    {
        //        var tuple = await repo.GetWOercomByMidAsync(parameter.mid, parameter.pageIndex, parameter.pageSize);
        //        var list = tuple.Item2;
        //        var count = tuple.Item1;
        //        List<object> listRes = new List<object>();
        //        using (var codebiz = new CodeRepository())
        //        {
        //            foreach (var item in list)
        //            {
        //                var user = await RedisUserOp.GetByUidAsnyc(item.uid);
        //                if (user != null)
        //                {
        //                    int age = 0;
        //                    if (item.age > 0)
        //                    {
        //                        DateTime birthday = CommonHelper.FromUnixTime(Convert.ToDouble(item.age));
        //                        if (birthday < DateTime.Now)
        //                            age = DateTime.Now.Year - birthday.Year;
        //                    }
        //                    string skin = "";
        //                    if (item.skin != null)
        //                        skin = await codebiz.GetCodeSkin(item.skin.Value);
        //                    int imgPraisesCount = 0;//照片被赞次数
        //                    int imgCount = 0;//总照片数
        //                    var tupleCommunity = await EsCommunityManager.GetCountAsync(parameter.mid, item.uid, (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
        //                    if (tupleCommunity != null)
        //                    {
        //                        imgCount = tupleCommunity.Item1;
        //                        imgPraisesCount = tupleCommunity.Item2;
        //                    }
        //                    listRes.Add(new
        //                    {
        //                        uid = item.uid,
        //                        name = item.nickName,
        //                        headerpic = user.HeadImgUrl,
        //                        woerName = item.woname,
        //                        age = age,
        //                        skin = skin,
        //                        imgCount,
        //                        imgPraisesCount
        //                    });
        //                }
        //            }
        //            var totalPage = Math.Ceiling(count/1.0/parameter.pageSize);
        //            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = listRes }, HttpStatusCode.OK, ECustomStatus.Success);
        //        }
        //    }
        //} 
        #endregion

        /// <summary>
        /// 核销员列表
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getwriteofflist")]
        public async Task<HttpResponseMessage> GetWriteOffList(UserParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.pageIndex <= 0 || parameter.pageSize <= 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var tuple = await repo.GetWOercomByMidAsync(parameter.mid, 1, 1000);
                var list = tuple.Item2;
                List<string> listUid = list.Select(w => w.uid.ToString()).ToList();
                var tuple4 = await EsCommunityBizManager.GetByAggUidAsync(parameter.mid, listUid, (int)EComBizType.Favour, parameter.pageIndex, parameter.pageSize);
                var count = tuple4.Item1;
                var listKeyItem = tuple4.Item2;
                List<WriteOfferObject> listRes = new List<WriteOfferObject>();
                using (var codebiz = new CodeRepository())
                {
                    foreach (var item in listKeyItem)
                    {
                        Guid uid = Guid.Parse(item.Key);
                        var writeOffer = list.Where(w => w.uid == uid).FirstOrDefault();
                        var user = await RedisUserOp.GetByUidAsnyc(uid);
                        if (writeOffer != null && user != null)
                        {
                            int age = 0;
                            if (writeOffer.age > 0)
                            {
                                DateTime birthday = CommonHelper.FromUnixTime(Convert.ToDouble(writeOffer.age));
                                if (birthday < DateTime.Now)
                                    age = DateTime.Now.Year - birthday.Year;
                            }
                            string skin = "";
                            if (writeOffer.skin != null)
                                skin = await codebiz.GetCodeSkin(writeOffer.skin.Value);
                            int imgPraisesCount = 0;//照片被赞次数
                            int imgCount = 0;//总照片数
                            var tupleCommunity = await EsCommunityManager.GetCountAsync(parameter.mid, uid, (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
                            if (tupleCommunity != null)
                            {
                                imgCount = tupleCommunity.Item1;
                                imgPraisesCount = tupleCommunity.Item2;
                            }
                            listRes.Add(new WriteOfferObject()
                            {
                                uid = uid,
                                name = user.NickName,
                                headerpic = user.HeadImgUrl,
                                woerName = writeOffer.woname,
                                age = age,
                                skin = skin,
                                imgCount = imgCount,
                                imgPraisesCount = imgPraisesCount
                            });
                        }
                    }
                    //var listObj = listRes.OrderByDescending(t => t.imgPraisesCount).ToList();
                    var totalPage = Math.Ceiling(count / 1.0 / parameter.pageSize);
                    return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = listRes }, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
        }

        private class WriteOfferObject
        {
            public Guid uid { get; set; }
            public string name { get; set; }
            public string headerpic { get; set; }
            public string woerName { get; set; }
            public int age { get; set; }
            public string skin { get; set; }
            public int imgCount { get; set; }
            public int imgPraisesCount { get; set; }
        }

    }
}
