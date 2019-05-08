using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Redis.MD;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Vector;
using MD.Lib.Weixin.Vector.Vectors;
using MD.Model.Configuration.Redis;
using MD.Model.DB.Professional;
using MD.Model.Redis.RedisObjects.Vector;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.WeChat.Filters;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/tl")]
    [AccessFilter]
    public class WechatTimeLineController : ApiController
    {
        [HttpPost]
        [Route("getlist")]
        public async Task<HttpResponseMessage> getlist(BaseParameter parameter)
        {
            if (parameter == null || parameter.pageIndex <= 0 || parameter.uid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            try
            {
                int pageSize = MdWxSettingUpHelper.GetPageSize();
                var tuple = await TimeLineVectorHelper.GetTopAsnyc(parameter.uid, parameter.pageIndex);
                List<object> _list = new List<object>();

                if (tuple.Item2?.Count > 0)
                {
                    var redis = new RedisManager2<WeChatRedisConfig>();
                    foreach (var v in tuple.Item2)
                    {
                        //从redis中取出vector
                        var vectorRedis = await redis.GetObjectFromRedisHash<VectorRedis>(v.Key);
                        if (string.IsNullOrEmpty(vectorRedis.value))
                            continue;

                        //序列化成vector db对象
                        Vector vv = RedisCommonHelper.StringToObject<Vector>(vectorRedis.value);
                        var view = VectorProcessorManager.Parse(vv);
                        if (view == null)
                            continue;
                        //user头像
                        var userRedis = await RedisUserOp.GetByUidAsnyc(Guid.Parse(view.Owner));

                        //type的判断
                        string picUrl = "";
                        string bizComment = "";
                        string bizid = "";
                        string type = "";
                        if (view.Type.Equals(ETimelineType.CJPT.ToString()) || view.Type.Equals(ETimelineType.KT.ToString()))
                        {
                            var goid = Guid.Parse(view.Objects[0]);
                            var goindex = await EsGroupOrderManager.GetByIdAsync(goid);
                            if (goindex != null)
                            {
                                var g = await EsGroupManager.GetByGidAsync(Guid.Parse(goindex.gid));
                                if (g != null)
                                {
                                    picUrl = g.advertise_pic_url;
                                    bizComment = g.title;
                                }
                                bizid = goid.ToString();
                                type = "0";
                            }
                        }
                        else if (view.Type.Equals(ETimelineType.YDWZ.ToString()))
                        {
                            var nid = Guid.Parse(view.Objects[0]);
                            var n = await EsNoticeBoardManager.GetByidAsync(nid);
                            if (n != null)
                            {
                                picUrl = n.thumb_pic;
                                bizComment = n.title;
                                bizid = nid.ToString();
                                type = "1";
                            }
                        }


                        //时间
                        StringBuilder timesb = new StringBuilder();
                        TimeSpan ts = DateTime.Now - CommonHelper.FromUnixTime(view.Timestamp);
                        if (ts.Days > 0)
                            timesb.Append(ts.Days + "天 ");
                        if (ts.Hours > 0)
                            timesb.Append(ts.Hours + "小时 ");
                        if (ts.Minutes > 0)
                            timesb.Append(ts.Minutes + "分钟 ");

                        _list.Add(new
                        {
                            nickname = userRedis.NickName,
                            head_pic = userRedis.HeadImgUrl,
                            comment = view.Contents[0],
                            picUrl,
                            bizComment,
                            bizid,
                            type,
                            time = string.IsNullOrEmpty(timesb.ToString()) ? "刚刚" : timesb.ToString()
                        });
                    }
                }

                //拼json
                int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);


                return JsonResponseHelper.HttpRMtoJson((new { totalPage = totalPage, list = _list }), HttpStatusCode.OK,
                    ECustomStatus.Success);
            }
            catch (Exception ex)
            {
                return JsonResponseHelper.HttpRMtoJson("error"+ex.Message, HttpStatusCode.OK,
    ECustomStatus.Fail);
            }
        }
    }
}
