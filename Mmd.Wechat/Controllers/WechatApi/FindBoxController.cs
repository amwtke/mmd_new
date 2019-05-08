using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.MQ.MD;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB.Activity;
using MD.Model.DB.Code;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Activity;
using MD.WeChat.Filters;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/findbox")]
    [AccessFilter]
    public class FindBoxController : ApiController
    {
        [HttpPost]
        [Route("IsExistsBox")]
        public async Task<HttpResponseMessage> IsExistsBox(findboxParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"参数错误,appid:{parameter.appid},openid:{parameter.openid},", HttpStatusCode.OK, ECustomStatus.Fail);
            object retobj = "";
            int step = 0;//0:未打开，1:未核销，2：已核销
            //验证该用户是否属于该appid
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null || !user.wx_appid.Equals(parameter.appid))
            {
                return JsonResponseHelper.HttpRMtoJson($"用户与商家匹配失败,appid:{parameter.appid},openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var fxUrl = MdWxSettingUpHelper.GenFindBoxUrl(parameter.appid);
            var index_box = await EsAct_boxManager.GetByAppidAsync(parameter.appid, (int)EBoxStatus.已上线);
            #region 1:商家未设置宝箱
            if (index_box == null)
            {
                //说明该商家未设置宝箱
                retobj = new
                {
                    prize = "",
                    time = "",
                    state = "",
                    stock = 0,
                    step = 0,
                    bid = Guid.Empty,
                    fxUrl = fxUrl,
                    hxUrl = "",
                    glist = "",
                };
                return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
            }
            #endregion
            Guid bid = Guid.Parse(index_box.Id);
            //查看宝箱内是否有宝贝
            var boxtList = await EsAct_boxtreasureManager.GetBybidAsync(bid);
            #region 2:查看该用户是否已经领过了
            var UserTreasure = await EsAct_usertreasureManager.GetByOpenidAsync(bid, parameter.openid);
            if (UserTreasure != null)
            {
                //核销地址
                string url = MdWxSettingUpHelper.GenWriteOffFindBoxUrl(parameter.appid, UserTreasure.Id);
                var boxt = await EsAct_boxtreasureManager.GetBybtidAsync(Guid.Parse(UserTreasure.btid));//用户抢到的那条宝箱宝贝
                //库存
                int stock = boxtList.Sum(p => p.quota_count) <= 0 ? 0 : boxtList.Sum(p => p.quota_count);
                //该用户已经获取到宝箱了，直接返回二维码
                if (index_box.time_end <= CommonHelper.ToUnixTime(DateTime.Now))
                    step = -1;
                if (UserTreasure.status == (int)EUserTreasureStatus.未核销)
                    step = 1;
                else
                    step = 2;
                retobj = new
                {
                    prize = boxt.name,
                    time = UserTreasure.open_time,
                    state = boxt.description,
                    stock = stock,
                    step = step,
                    bid = bid,
                    hxUrl = url,
                    fxUrl = fxUrl,
                    glist = await GetZjList(bid),
                };
                return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
            }
            #endregion
            #region 3:商家有宝箱，但是未设置宝贝或宝贝被领完或宝箱已过期
            if (boxtList == null || boxtList.Sum(p => p.quota_count) <= 0 || index_box.time_end <= CommonHelper.ToUnixTime(DateTime.Now))
            {
                //说明该商家有宝箱，但是未设置宝贝或宝贝被领完
                retobj = new
                {
                    prize = "",
                    time = "",
                    state = "",
                    stock = 0,
                    step = 0,
                    hxUrl = "",
                    fxUrl = fxUrl,
                    bid = bid,
                    glist = await GetZjList(bid),
                };
                return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
            }
            #endregion
            // 该用户未打开过宝箱
            retobj = new
            {
                prize = "",
                time = "",
                state = "",
                stock = boxtList.Sum(p => p.quota_count),
                step = 0,
                bid = bid,
                hxUrl = "",
                fxUrl = fxUrl,
                glist = await GetZjList(bid),
            };
            return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("openBox")]
        public async Task<HttpResponseMessage> openBox(findboxParameter parameter)
        {
            //Guid bid, string appid, string openid
            try
            {
                if (parameter == null || parameter.bid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid))
                    return JsonResponseHelper.HttpRMtoJson($"参数错误,bid{parameter.bid},appid:{parameter.appid},openid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);
                var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
                if (user == null || !user.wx_appid.Equals(parameter.appid))
                    return JsonResponseHelper.HttpRMtoJson($"用户与商家不符合,appid{parameter.appid},openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
                var index_box = await EsAct_boxManager.GetByAppidAsync(parameter.appid, (int)EBoxStatus.已上线);
                //提示先关注
                //string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(parameter.appid);
                //UserInfoJson user1 = await Senparc.Weixin.MP.AdvancedAPIs.UserApi.InfoAsync(at, parameter.openid);
                //bool IsUserSub = user1?.subscribe == 1;
                //if (!IsUserSub)
                //    return JsonResponseHelper.HttpRMtoJson($"请先关注", HttpStatusCode.OK, ECustomStatus.UserNotSubscribe);

                if (index_box == null)
                    return JsonResponseHelper.HttpRMtoJson($"没有上线宝箱", HttpStatusCode.OK, ECustomStatus.Fail);
                var ESUserTreasure = await EsAct_usertreasureManager.GetByOpenidAsync(parameter.bid, parameter.openid);
                if (ESUserTreasure != null)
                    return JsonResponseHelper.HttpRMtoJson($"该宝箱已打开", HttpStatusCode.OK, ECustomStatus.Fail);
                var boxtreasureList = await EsAct_boxtreasureManager.GetBybidAsync(parameter.bid);
                if (boxtreasureList == null || boxtreasureList.Sum(p => p.quota_count) <= 0 || index_box.time_end <= CommonHelper.ToUnixTime(DateTime.Now))
                    return JsonResponseHelper.HttpRMtoJson($"很遗憾，宝箱已经被抢完了。", HttpStatusCode.OK, ECustomStatus.Fail);
                //根据索引获取该宝箱中的宝贝
                var userget = boxtreasureList[CommonHelper.GetRandomNumber(0, boxtreasureList.Count)];
                //存到act_usertreasure表中
                UserTreasure ut = new UserTreasure()
                {
                    utid = Guid.NewGuid(),
                    uid = Guid.Parse(user.Id),
                    mid = Guid.Parse(user.mid),
                    openid = user.openid,
                    btid = Guid.Parse(userget.Id),
                    bid = Guid.Parse(userget.bid),
                    status = (int)EUserTreasureStatus.未核销,
                    open_time = CommonHelper.GetUnixTimeNow(),
                };
                using (var acti = new ActivityRepository())
                {
                    bool b = await acti.AddOrUpdateUsertreaAsync(ut);
                    if (b)
                    {
                        //更新ES
                        var index_ut = await EsAct_usertreasureManager.GenObjAsync(ut.utid);
                        var flag = await EsAct_usertreasureManager.AddOrUpdateAsync(index_ut);
                        if (b && flag)
                        {
                            //核销地址
                            string url = MdWxSettingUpHelper.GenWriteOffFindBoxUrl(parameter.appid, ut.utid.ToString());
                            var fxUrl = MdWxSettingUpHelper.GenFindBoxUrl(parameter.appid);
                            var retobj = new
                            {
                                prize = userget.name,
                                time = ut.open_time,
                                state = userget.description,
                                stock = boxtreasureList.Sum(p => p.quota_count) <= 0 ? 0 : boxtreasureList.Sum(p => p.quota_count),
                                step = 1,
                                bid = parameter.bid,
                                fxUrl = fxUrl,
                                hxUrl = url,
                                glist = await GetZjList(parameter.bid),
                            };
                            //发送模板消息
                            var obj = MqWxTempMsgManager.GenActivityMessage(parameter.appid, parameter.openid, "恭喜您，发现了一个宝贝！", "阿里巴巴之神秘宝箱", userget.name, fxUrl);
                            MqWxTempMsgManager.SendMessage(obj);
                            return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
                        }
                    }
                }
                return JsonResponseHelper.HttpRMtoJson($"很遗憾，宝箱已经被抢完了。", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(FindBoxController), new Exception("openBox失败：" + ex.Message));
            }
        }

        /// <summary>
        /// 该宝箱中奖的前100个人
        /// </summary>
        /// <param name="bid">宝箱bid</param>
        /// <returns></returns>
        private async Task<object> GetZjList(Guid bid)
        {
            List<object> zjUserList = new List<object>();
            var index_usertList = await EsAct_usertreasureManager.GetBybidAsync(bid, 1, 100);
            if (index_usertList != null && index_usertList.Count > 0)
            {
                foreach (var usert in index_usertList)
                {
                    var boxt = await EsAct_boxtreasureManager.GetBybtidAsync(Guid.Parse(usert.btid));
                    var user1 = await EsUserManager.GetByIdAsync(Guid.Parse(usert.uid));
                    if (boxt != null && user1 != null)
                    {
                        zjUserList.Add(new
                        {
                            name = user1.name,
                            prize = boxt.name
                        });
                    }
                }
            }
            return zjUserList;
        }

        [HttpPost]
        [Route("hexiao")]
        public async Task<HttpResponseMessage> hexiao(findboxParameter parameter)
        {
            //Guid utid, string openid
            try
            {
                if (parameter == null || parameter.utid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid))
                {
                    return JsonResponseHelper.HttpRMtoJson($"parameter error!oid:{parameter.utid},openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                #region 核销逻辑

                using (var Activ = new ActivityRepository())
                {
                    //查询该订单是否存在
                    var ut = await Activ.GetUserTreasureByUtidAsync(parameter.utid);
                    if (ut == null)
                        return JsonResponseHelper.HttpRMtoJson("该用户宝贝不存在！", HttpStatusCode.OK, ECustomStatus.Fail);

                    var boxt = await EsAct_boxtreasureManager.GetBybtidAsync(ut.btid);//宝箱宝贝信息
                    var index_box = await EsAct_boxManager.GetBybidAsync(ut.bid);//宝箱信息
                    if (boxt.quota_count <= 0)
                        return JsonResponseHelper.HttpRMtoJson("该宝贝已经被领完了（库存不足）！", HttpStatusCode.OK, ECustomStatus.Fail);
                    //验证商品状态
                    if (ut.status != (int)EUserTreasureStatus.未核销)
                        return JsonResponseHelper.HttpRMtoJson(new
                        {
                            prize = boxt.name,
                            state = boxt.description,
                            endTime = index_box.time_end,
                            step = 2
                        }, HttpStatusCode.OK, ECustomStatus.Success);

                    var currentuser = await EsUserManager.GetByOpenIdAsync(parameter.openid);//获取当前用户信息
                    if (currentuser == null)
                        return JsonResponseHelper.HttpRMtoJson("核销员不存在！", HttpStatusCode.OK, ECustomStatus.Fail);
                    Guid currentUid = Guid.Parse(currentuser.Id);

                    //验证是否核销
                    if (!ut.writeoffer.Equals(Guid.Empty))
                    {
                        return JsonResponseHelper.HttpRMtoJson(new
                        {
                            prize = boxt.name,
                            state = boxt.description,
                            endTime = index_box.time_end,
                            step = 2
                        }, HttpStatusCode.OK, ECustomStatus.Success);
                    }

                    //验证商家与核销员
                    using (var repo = new BizRepository())
                    {
                        var mer = await repo.GetMerchantByMidAsync(ut.mid);
                        if (mer == null)
                            return JsonResponseHelper.HttpRMtoJson($"订单所在商家错误！mid:{ut.mid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        if (!mer.wx_appid.Equals(parameter.appid))
                            return JsonResponseHelper.HttpRMtoJson($"订单商家错误，appid不符！ut's appid:{mer.wx_appid},而appid:{parameter.appid}", HttpStatusCode.OK, ECustomStatus.Fail);

                        if (!await repo.WoerCanWriteOff(mer.mid, currentUid))
                        {
                            return JsonResponseHelper.HttpRMtoJson($"您无权核销此订单！编号：{ut.utid}", HttpStatusCode.OK, ECustomStatus.Fail);
                        }
                        var woer = await repo.WoerGetByUidAsync(currentUid);
                        //完成核销
                        ut.writeoffer = currentUid;
                        ut.writeofftime = CommonHelper.GetUnixTimeNow();
                        ut.status = (int)EUserTreasureStatus.已核销;
                        if (await Activ.AddOrUpdateUsertreaAsync(ut))
                        {
                            //更新ES
                            var index_ut = await EsAct_usertreasureManager.GenObjAsync(ut.utid);
                            var flag = await EsAct_usertreasureManager.AddOrUpdateAsync(index_ut);
                            if (flag)//扣库存
                            {
                                if (await Activ.UpdateQuota_countAsync(Guid.Parse(boxt.Id)))
                                {
                                    //更新EsAct_boxtreasure减库存
                                    var index_boxt = await EsAct_boxtreasureManager.GenObjectAsync(Guid.Parse(boxt.Id));
                                    await EsAct_boxtreasureManager.AddOrUpdateAsync(index_boxt);
                                }
                            }
                        }
                        return JsonResponseHelper.HttpRMtoJson(new
                        {
                            prize = boxt.name,
                            state = boxt.description,
                            endTime = index_box.time_end,
                            step = 1
                        }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResponseHelper.HttpRMtoJson("核销失败" + ex.Message, HttpStatusCode.InternalServerError,
                      ECustomStatus.Fail);
            }
        }

        [HttpPost]
        [Route("getboxtreasure")]
        public async Task<HttpResponseMessage> GetBoxTreasure(findboxParameter parameter)
        {
            if (parameter == null || parameter.bid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"参数错误,bid:{parameter?.bid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var index_box = await EsAct_boxManager.GetBybidAsync(parameter.bid);
            if (index_box == null || index_box.status != (int)EBoxStatus.已上线)
                return JsonResponseHelper.HttpRMtoJson($"未找到该宝箱bid:{parameter.bid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var boxT = await EsAct_boxtreasureManager.GetBybidAsync(parameter.bid);
            if (boxT == null)
                return JsonResponseHelper.HttpRMtoJson($"未找到该宝箱内的宝贝bid:{parameter.bid}", HttpStatusCode.OK, ECustomStatus.Fail);
            return JsonResponseHelper.HttpRMtoJson(boxT, HttpStatusCode.OK, ECustomStatus.Success);
        }
    }
}
