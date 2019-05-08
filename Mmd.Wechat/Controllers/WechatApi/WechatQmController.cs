using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Redis.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.GroupOrder;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Qm;
using MD.WeChat.Filters;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.DB.Repositorys;
using MD.Model.DB.Professional;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/qm")]
    [AccessFilter]
    public class WechatQmController : ApiController
    {
        [HttpPost]
        [Route("getlist")]
        public async Task<HttpResponseMessage> getlist(QmParameter parameter)
        {
            if (parameter == null || parameter.uid.Equals(Guid.Empty) || parameter.pageIndex<=0)
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            var uid = parameter.uid;
            try
            {
                using (var codebiz = new CodeRepository())
                {
                    var codeSkinList = await codebiz.GetAllCodeSkinAsync();//肤质字典
                    var retsult = await RedisVectorOp.GetPTCGTop2Asnyc(uid, parameter.pageIndex);
                    if (retsult.Item2.Count > 0)
                    {
                        var redis = new RedisManager2<WeChatRedisConfig>();
                        List<object> _list = new List<object>();
                        foreach (var v in retsult.Item2)
                        {
                            var redisValue = await redis.GetObjectFromRedisHash<UserInfoRedis>(v.Key);
                            var indexuser = await EsUserManager.GetByOpenIdAsync(v.Key);
                            if (indexuser == null) continue;
                            //计算好友年龄
                            int age = 0;
                            if (indexuser.age > 0)
                            {
                                DateTime birthday = CommonHelper.FromUnixTime(indexuser.age);
                                if (birthday < DateTime.Now)
                                {
                                    age = DateTime.Now.Year - birthday.Year;
                                }
                            }
                            var commnuity = await EsCommunityManager.GetCountAsync(Guid.Parse(indexuser.mid), Guid.Parse(indexuser.Id), (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
                            int imgPraisesCount = 0;//照片被赞次数
                            int imgCount = 0;//总照片数
                            if (commnuity != null)
                            {
                                imgCount = commnuity.Item1;
                                imgPraisesCount = commnuity.Item2;
                            }
                            _list.Add(new
                            {
                                head_pic = redisValue.HeadImgUrl,
                                uuid = redisValue.Uid,
                                nick_name = redisValue.NickName,
                                qmd = v.Value,
                                age = age,
                                skin = codeSkinList.ContainsKey(indexuser.skin) ? codeSkinList[indexuser.skin] : "",//肤质 
                                imgCount,
                                imgPraisesCount
                            });
                        }
                        int totalPage = retsult.Item1;

                        //计算总页数
                        if (totalPage % 10 == 0)
                            totalPage = totalPage / 10;
                        else
                            totalPage = (totalPage / 10) + 1;

                        return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, _list = _list },
                            HttpStatusCode.OK,
                            ECustomStatus.Success);
                    }
                }

                return
                    JsonResponseHelper.HttpRMtoJson("没有值",
                        HttpStatusCode.OK,
                        ECustomStatus.Success);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(WechatQmController),ex);
                return
                    JsonResponseHelper.HttpRMtoJson("error",
                        HttpStatusCode.OK,
                        ECustomStatus.Fail);
            }
        }
    }
}