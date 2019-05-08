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
using MD.Lib.DB.Redis;
using MD.Lib.Exceptions.Pay;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;
using MD.Lib.Weixin.Vector.Vectors;
using MD.Model.DB;
using MD.Model.DB.Professional;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using MD.Model.Configuration.Redis;
using MD.Model.Index.MD;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.Wechat.Controllers.WechatApi.Parameters.biz;
using MD.WeChat.Filters;


namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/noticeboard")]
    [AccessFilter]
    public class WechatNoticeBoardController : ApiController
    {
        /// <summary>
        /// 获取文章分类
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getcategory")]
        public async Task<HttpResponseMessage> getcategory(NoticeBoardParameter parameter)
        {
            using (var coderepo = new CodeRepository())
            {
                var list = await coderepo.GetNoticeCateListAsync();
                return JsonResponseHelper.HttpRMtoJson(list, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="parameter">pageIndex</param>
        /// <returns></returns>
        [HttpPost]
        [Route("getlist")]
        public async Task<HttpResponseMessage> getlist(NoticeBoardParameter parameter)
        {
            if (parameter == null || parameter.pageIndex <= 0 || parameter.mid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            int pageSize = MdWxSettingUpHelper.GetPageSize();

            var ret = await EsNoticeBoardManager.GetNoticeBoardAsync(parameter.mid, parameter.category, parameter.pageIndex, pageSize);
            if (ret == null)
            {
                return JsonResponseHelper.HttpRMtoJson($"没有取到资讯信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            //拼json
            List<object> noticelist = new List<object>();
            int totalPage = MdWxSettingUpHelper.GetTotalPages(ret.Item1);

            foreach (var ig in ret.Item2)
            {
                noticelist.Add(
                    new
                    {
                        nid = ig.Id,
                        thumb_pic = ig.thumb_pic,
                        title = ig.title,
                        tag_1 = ig.tag_1,
                        tag_2 = ig.tag_2,
                        tag_3 = ig.tag_3,
                        hits_count = ig.hits_count,
                        transmit_count = ig.transmit_count,
                        source = ig.source
                    });
            }
            return JsonResponseHelper.HttpRMtoJson((new { totalPage = totalPage, glist = noticelist }), HttpStatusCode.OK,
                ECustomStatus.Success);
        }
        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="parameter">nid</param>
        /// <returns></returns>
        [HttpPost]
        [Route("getdetail")]
        public async Task<HttpResponseMessage> getdetail(NoticeBoardParameter parameter)
        {
            if (parameter == null || parameter.nid.Equals(Guid.Empty) || parameter.readerSize <= 0 || string.IsNullOrEmpty(parameter.appid) || string.IsNullOrEmpty(parameter.openid)
                || parameter.CommentPageIndex < 0 || parameter.CommentPageSize < 0 || parameter.praisesPageIndex < 0 || parameter.praisesPageSize < 0)
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"user is null openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentUid = Guid.Parse(user.Id);
            Guid currentMid = Guid.Parse(user.mid);
            var index = await EsNoticeBoardManager.GetNoticeBoardAsync(parameter.nid);
            if (index == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到nid:{parameter.nid}的文章信息！", HttpStatusCode.OK,
                    ECustomStatus.Fail);

            //html描述
            string html = HttpUtility.HtmlDecode(index.description).Replace("\"", "\'");
            #region  看过人的用户头像数组
            List<string> userImgList = new List<string>();
            using (var rep = new BizRepository())
            {
                List<string> openidList = await rep.GetNoticeReaderAsync(parameter.nid, parameter.readerSize);//现根据nid在NoticeReader取到前N个用户
                //再根据用户openid取Radis到头像
                foreach (string openud in openidList)
                {
                    UserInfoRedis userRedis =
                     await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(openud);
                    if (!string.IsNullOrEmpty(userRedis.HeadImgUrl))
                        userImgList.Add(userRedis.HeadImgUrl);//增加到头像数组中
                }

            }
            #endregion
            #region 评论的列表
            var Tuplecomment = await EsCommentManager.GetCommentByTopic_IdAsync(currentMid, parameter.nid, parameter.CommentPageIndex, parameter.CommentPageSize);
            List<object> commentList = new List<object>();
            int commentTotalPage = GetTotalPages(Tuplecomment.Item1, parameter.CommentPageSize);
            foreach (var comment in Tuplecomment.Item2)
            {
                commentList.Add(new
                {
                    commentid = comment.Id,
                    comment.content,
                    comment.from_uid,
                    comment.timestamp,
                    com_replys = comment.Com_Replys,
                    comment.dic_nickname,
                    comment.dic_headerpic
                });
            }
            #endregion
            #region 点赞人列表
            var communityBiz = await EsCommunityBizManager.GetCommunityBizListAsync(currentMid, Guid.Empty, Guid.Empty, parameter.nid, (int)EComBizType.NoticBoardFavour, parameter.praisesPageIndex, parameter.praisesPageSize);
            List<object> praisesList = new List<object>();
            int praisesTotalPage = GetTotalPages(communityBiz.Item1, parameter.praisesPageSize);
            foreach (var praises in communityBiz.Item2)
            {
                var praisesUser = await RedisUserOp.GetByUidAsnyc(Guid.Parse(praises.from_id));
                praisesList.Add(new
                {
                    praisesUser.Uid,
                    praisesUser.NickName,
                    praisesUser.HeadImgUrl
                });
            }
            #endregion
            bool isPraises = false;
            var praisesBiz = await EsCommunityBizManager.GetCommunityBizAsync(Guid.Empty, currentUid, parameter.nid, (int)EComBizType.NoticBoardFavour);
            if (praisesBiz != null)
                isPraises = true;
            //分享到朋友圈的回调地址
            string sharecallback = MdWxSettingUpHelper.GenNoticeDetailUrl(parameter.appid, parameter.nid);
            //拼json
            var retObject =
                new
                {
                    nid = index.Id,
                    thumb_pic = index.thumb_pic,
                    title = index.title,
                    source = index.source,
                    tag_1 = index.tag_1,
                    tag_2 = index.tag_2,
                    tag_3 = index.tag_3,
                    description = html,
                    timestamp = CommonHelper.FromUnixTime(index.timestamp).ToString(),
                    hits_count = index.hits_count,
                    transmit_count = index.transmit_count,
                    praise_count = index.praise_count,
                    userImaList = userImgList,
                    sharecallbackUrl = sharecallback,
                    commentList,
                    commentTotalPage,
                    praisesList,
                    praisesTotalPage,
                    isPraises
                };

            return JsonResponseHelper.HttpRMtoJson(retObject, HttpStatusCode.OK, ECustomStatus.Success);
        }
        /// <summary>
        /// 点击量自动+1
        /// </summary>
        /// <param name="parameter">nid</param>
        /// <returns></returns>
        [HttpPost]
        [Route("hitscount")]
        public async Task<HttpResponseMessage> hits_count(NoticeBoardParameter parameter)
        {
            try
            {
                if (parameter == null || parameter.nid.Equals(Guid.Empty)
                   || parameter.uid.Equals(Guid.Empty))
                {
                    return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                var user = await EsUserManager.GetByIdAsync(parameter.uid);
                if (user == null)
                    return JsonResponseHelper.HttpRMtoJson($"uid;{parameter.uid}的用户找不到！", HttpStatusCode.OK, ECustomStatus.Fail);
                using (var rep = new BizRepository())
                {
                    NoticeReader reader = new NoticeReader
                    {
                        nid = parameter.nid,
                        uid = parameter.uid,
                        openid = user.openid,
                        timestamp = CommonHelper.GetUnixTimeNow(),
                        comment = parameter.comment,
                        extend_1 = parameter.extend_1,
                        extend_2 = parameter.extend_2,
                        extend_3 = parameter.extend_3
                    };

                    bool flag = await rep.UpdateNoticeBoardhitsAsync(parameter.nid, reader);
                    if (flag)
                    {
                        //更新es
                        var index = await EsNoticeBoardManager.GenObjectAsync(parameter.nid);//从数据库中重新取，然后返回新的IndexNoticeBoard
                        if (index != null)
                        {
                            if (!EsNoticeBoardManager.AddOrUpdate(index))
                                return JsonResponseHelper.HttpRMtoJson("更新ES失败", HttpStatusCode.OK, ECustomStatus.Success);
                        }
                        return JsonResponseHelper.HttpRMtoJson(new { hits_count = index.hits_count, transmin_count = index.transmit_count },
                            HttpStatusCode.OK, ECustomStatus.Success);
                    }
                    return JsonResponseHelper.HttpRMtoJson(new { isOK = false }, HttpStatusCode.OK, ECustomStatus.Fail);
                }
            }
            catch (Exception)
            {
                return JsonResponseHelper.HttpRMtoJson("更新失败", HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
            }
        }
        /// <summary>
        /// 转发量自动+1
        /// </summary>
        /// <param name="parameter">nid</param>
        /// <returns></returns>
        [HttpPost]
        [Route("transmitcount")]
        public async Task<HttpResponseMessage> transmit_count(NoticeBoardParameter parameter)
        {
            try
            {
                if (parameter == null || parameter.nid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty))
                {
                    return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
                }
                var user = await EsUserManager.GetByIdAsync(parameter.uid);
                if (user == null)
                    return JsonResponseHelper.HttpRMtoJson($"uid;{parameter.uid}的用户找不到！", HttpStatusCode.OK, ECustomStatus.Fail);
                using (var rep = new BizRepository())
                {
                    bool flag = await rep.UpdateNoticeBoardtransmitAsync(parameter.nid);
                    if (flag)
                    {
                        //更新es
                        var index = await EsNoticeBoardManager.GenObjectAsync(parameter.nid);//从数据库中重新取，然后返回新的IndexNoticeBoard
                        if (index != null)
                        {
                            if (!EsNoticeBoardManager.AddOrUpdate(index))
                                return JsonResponseHelper.HttpRMtoJson("更新ES失败", HttpStatusCode.OK, ECustomStatus.Success);
                        }

                        //种草的时间线
                        TimeLineVectorHelper.ZhongChao(parameter.nid, parameter.uid);

                        return JsonResponseHelper.HttpRMtoJson(new { hits_count = index.hits_count, transmin_count = index.transmit_count },
                            HttpStatusCode.OK, ECustomStatus.Success);
                    }
                    return JsonResponseHelper.HttpRMtoJson(new { isOK = false }, HttpStatusCode.OK, ECustomStatus.Fail);
                }
            }
            catch (Exception)
            {
                return JsonResponseHelper.HttpRMtoJson("更新失败", HttpStatusCode.InternalServerError,
                        ECustomStatus.Fail);
            }
        }

        /// <summary>
        /// 获取点击量最高的前N篇文章列表
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getlistorderbyhit")]
        public async Task<HttpResponseMessage> getlistorderbyhit(NoticeBoardParameter parameter)
        {
            if (parameter == null || parameter.pageSize <= 0 || parameter.mid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            var ret = await EsNoticeBoardManager.GetNoticeBoardSortHisAsync(parameter.mid, parameter.pageSize);
            if (ret == null)
            {
                return JsonResponseHelper.HttpRMtoJson($"没有取到资讯信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            return JsonResponseHelper.HttpRMtoJson((new { glist = ret }), HttpStatusCode.OK,
                ECustomStatus.Success);
        }

        /// <summary>
        /// 获取点击量最高的前N篇文章列表
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getlistorderbytop")]
        public async Task<HttpResponseMessage> getlistorderbytop(NoticeBoardParameter parameter)
        {
            if (parameter == null || parameter.pageSize <= 0 || parameter.mid.Equals(Guid.Empty))
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }

            var ret = await EsNoticeBoardManager.GetNoticeBoardList(parameter.mid, parameter.pageSize, "extend_1");
            if (ret == null)
            {
                return JsonResponseHelper.HttpRMtoJson($"没有取到资讯信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            return JsonResponseHelper.HttpRMtoJson((new { glist = ret }), HttpStatusCode.OK,
                ECustomStatus.Success);
        }
        private int GetTotalPages(int totalCount, int pageSize)
        {
            if (totalCount <= 0)
                return 0;
            if (totalCount % pageSize == 0)
                return totalCount / pageSize;
            return totalCount / pageSize + 1;
        }
    }
}