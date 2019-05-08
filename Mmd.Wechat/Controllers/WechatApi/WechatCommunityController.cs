using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Aliyun;
using MD.Model.DB.Professional;
using MD.Model.Index;
using MD.Model.Index.MD;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Community;
using MD.WeChat.Filters;
using Nest;
using Senparc.Weixin.MP.AdvancedAPIs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/community")]
    [AccessFilter]
    public class WechatCommunityController : ApiController
    {
        /// <summary>
        /// 上传社区背景图片
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("uploadbackimg")]
        public async Task<HttpResponseMessage> uploadBackImg(CommunityParameter parameter)
        {
            if (parameter == null || parameter.uid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.backImg))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByIdAsync(parameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到uid:{parameter.uid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(user.wx_appid);
            string fileName = CommonHelper.GetUnixTimeNow().ToString();
            var str = MediaApi.Get2(at, parameter.backImg.Trim());
            var isOk = false;
            if (str != null && str.Length > 0)
            {
                var path = OssPicPathManager<OssPicBucketConfig>.UploadUserCommunityPic(parameter.uid, fileName.ToString(), str);
                if (!string.IsNullOrEmpty(path))
                {
                    isOk = true;
                    using (var reop = new BizRepository())
                    {
                        var dbuser = await reop.UpdateUserBackImgAsync(parameter.uid, path);
                        if (dbuser != null)
                        {
                            await EsUserManager.AddOrUpdateAsync(EsUserManager.GenObject(dbuser));
                            return JsonResponseHelper.HttpRMtoJson(new { isOk = isOk, path }, HttpStatusCode.OK, ECustomStatus.Success);
                        }
                    }
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { isOk = false, path = "" }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 增加社区说说
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("addorupdatecommunity")]
        public async Task<HttpResponseMessage> addCommunity(CommunityParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.openid) || parameter.mid.Equals(Guid.Empty) || parameter.imgs.Length <= 0 || parameter.topictype < 0
                || parameter.flag < 1)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentUid = Guid.Parse(user.Id);
            var redisUser = await RedisUserOp.GetByUidAsnyc(currentUid);
            if (redisUser == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到uid:{currentUid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            if (!user.mid.Equals(parameter.mid.ToString()))
                return JsonResponseHelper.HttpRMtoJson("用户与商家不对应！", HttpStatusCode.OK, ECustomStatus.Fail);
            //判断是否被禁言
            var blackUser = await EsBlacklistManager.GetBlacklistAsync(currentUid, (int)EBlacklistType.美美社区发帖);
            if (blackUser != null)
            {
                var nowTime = CommonHelper.GetUnixTimeNow();
                if (blackUser.opentimestamp > nowTime)
                {
                    var blackDays =Convert.ToInt64((blackUser.opentimestamp - blackUser.createtime) / 60 / 60 / 24);
                    return JsonResponseHelper.HttpRMtoJson($"您已被管理员禁言{blackDays}天！", HttpStatusCode.OK, ECustomStatus.Fail);
                }
            }
            using (var repo = new BizRepository())
            {
                Community community = new Community();
                community.cid = Guid.NewGuid();
                string fileName = community.cid.ToString(); //图片名称及文章cid_index;
                string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(user.wx_appid);
                string ImgPath = string.Empty;
                for (int i = 0; i < parameter.imgs.Length; i++)
                {
                    if (string.IsNullOrEmpty(parameter.imgs[i].Trim()))
                        continue;
                    var str = MediaApi.Get2(at, parameter.imgs[i].Trim());
                    if (str != null && str.Length > 0)
                    {
                        var path = OssPicPathManager<OssPicBucketConfig>.UploadUserCommunityPic(currentUid, fileName.ToString() + "_" + i, str);
                        if (!string.IsNullOrEmpty(path))
                            ImgPath += path + "|";
                    }
                }
                community.mid = parameter.mid;
                community.uid = currentUid;
                community.title = null;//社区无标题
                community.hits = 0;
                community.praises = 0;
                community.transmits = 0;
                community.topic_type = parameter.topictype;
                community.flag = parameter.flag;
                community.status = (int)ECommunityStatus.已发布;
                community.content = parameter.content;
                community.imgs = ImgPath?.Trim('|');
                var tempdbCommunity = await repo.SaveOrUpdateCommunityAsync(community);//保存到数据库
                if (tempdbCommunity != null)
                {
                    var indexComm = EsCommunityManager.GenObject(tempdbCommunity);
                    if (indexComm != null)
                    {
                        indexComm.KeyWords = redisUser.NickName + "," + community.content;
                        var flag = await EsCommunityManager.AddOrUpdateAsync(indexComm);
                        if (flag)
                        {
                            Thread.Sleep(100);
                            var tempcommunity = await EsCommunityManager.GetByIdAsync(community.cid);
                            int times = 0;
                            while (tempcommunity == null && times <= 6)
                            {
                                times++;
                                Thread.Sleep(times * 100);
                                tempcommunity = await EsCommunityManager.GetByIdAsync(community.cid);
                            }
                            if (tempcommunity == null)
                                MDLogger.LogErrorAsync(typeof(WechatCommunityController), new Exception($"Community存es失败，times:{times}"));
                        }
                        return JsonResponseHelper.HttpRMtoJson(new { isOk = true }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                }
                return JsonResponseHelper.HttpRMtoJson(new { isOk = false }, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        /// <summary>
        /// 删除社区说说
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("delcommunity")]
        public async Task<HttpResponseMessage> delCommunity(CommunityParameter parameter)
        {
            if (parameter == null || parameter.cid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentUid = Guid.Parse(user.Id);
            var community = await EsCommunityManager.GetByIdAsync(parameter.cid);
            if (community == null)
                return JsonResponseHelper.HttpRMtoJson($"null cid:{parameter.cid}", HttpStatusCode.OK, ECustomStatus.Fail);
            if (!community.uid.Equals(user.Id))
                return JsonResponseHelper.HttpRMtoJson($"没有权限删！", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var flag = false;
                var dbcommunity = await repo.DelCommunityAsync(parameter.cid, (int)ECommunityStatus.已删除);
                if (dbcommunity != null)
                {
                    var tempindex = EsCommunityManager.GenObject(dbcommunity);
                    flag = await EsCommunityManager.AddOrUpdateAsync(tempindex);
                    if (flag)
                    {
                        Thread.Sleep(100);
                        var tempcommunity = await EsCommunityManager.GetByIdAsync(parameter.cid);
                        int times = 0;
                        while (tempcommunity.status == (int)ECommunityStatus.已发布 && times <= 6)//如果还是已发布，则表示修改还未生效
                        {
                            times++;
                            Thread.Sleep(times * 100);
                            tempcommunity = await EsCommunityManager.GetByIdAsync(parameter.cid);
                        }
                        if (tempcommunity.status == (int)ECommunityStatus.已发布)
                            MDLogger.LogErrorAsync(typeof(WechatCommunityController), new Exception($"Community删除该状态失败，times:{times}"));
                        await EsCommunityBizManager.DelBizsAsync(currentUid, Guid.Empty, dbcommunity.cid, (int)EComBizType.Favour);//删除点赞日志
                    }
                }
                return JsonResponseHelper.HttpRMtoJson(new { isOk = flag }, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        /// <summary>
        /// 获取社区说说列表
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getlist")]
        public async Task<HttpResponseMessage> GetList(CommunityParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || parameter.pageIndex <= 0 || parameter.topictype < 0
                || parameter.CommentPageIndex <= 0 || parameter.CommentPageSize < 0 || parameter.praisesPageIndex < 0 || parameter.praisesPageSize < 0
                || string.IsNullOrEmpty(parameter.strSort))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
                if (user == null)
                    return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
                Guid currentUid = Guid.Parse(user.Id);
                if (!user.mid.Equals(parameter.mid.ToString()))
                    return JsonResponseHelper.HttpRMtoJson("用户与商家不对应！", HttpStatusCode.OK, ECustomStatus.Fail);
                int pageSize = MdWxSettingUpHelper.GetPageSize();
                List<object> retobj = new List<object>();
                var tuple = await EsCommunityManager.GetListAsync(parameter.mid, Guid.Empty, parameter.topictype, parameter.pageIndex, pageSize, (int)ECommunityStatus.已发布, "", parameter.flag, parameter.strSort);
                int totalPage = GetTotalPages(tuple.Item1, pageSize);
                foreach (var community in tuple.Item2)
                {
                    #region 发说说人的属性
                    Guid communityUid = Guid.Parse(community.uid);
                    Guid cid = Guid.Parse(community.Id);
                    var rediscommunityUser = await RedisUserOp.GetByUidAsnyc(communityUid);
                    var isWriteOffer = await repo.WoerCanWriteOff(parameter.mid, communityUid);
                    bool isMySelf = false;
                    if (communityUid.Equals(currentUid))
                        isMySelf = true;
                    #endregion
                    #region 当前用户的属性
                    bool isPraises = false;
                    var praisesBiz = await EsCommunityBizManager.GetCommunityBizAsync(communityUid, currentUid, cid, (int)EComBizType.Favour);
                    if (praisesBiz != null)
                        isPraises = true;
                    bool isAttention = false;
                    var attentionBiz = await EsCommunityBizManager.GetCommunityBizAsync(communityUid, currentUid, Guid.Empty, (int)EComBizType.Subscribe);
                    if (attentionBiz != null)
                        isAttention = true;
                    #endregion
                    #region 该条说说的评论列表
                    var Tuplecomment = await EsCommentManager.GetCommentByTopic_IdAsync(Guid.Empty, cid, parameter.CommentPageIndex, parameter.CommentPageSize);
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
                    var communityBiz = await EsCommunityBizManager.GetCommunityBizListAsync(Guid.Empty, communityUid, Guid.Empty, cid, (int)EComBizType.Favour, parameter.praisesPageIndex, parameter.praisesPageSize);
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
                    retobj.Add(new
                    {
                        isMySelf = isMySelf,
                        nickName = rediscommunityUser.NickName,
                        headerPic = rediscommunityUser.HeadImgUrl,
                        uid = community.uid,
                        isPraises = isPraises,//我是否赞过该说说
                        isWriteOffer = isWriteOffer,//是否美妆达人
                        isAttention = isAttention,//是否关注
                        cid = community.Id,
                        community.title,
                        community.content,
                        community.createtime,
                        community.lastupdatetime,
                        community.imgs,
                        community.praises,
                        community.hits,
                        community.transmits,
                        community.flag,//标签
                        commentTotalPage,//评论总页数
                        commentList,//评论列表
                        commentTotalCount = Tuplecomment.Item1,//评论总行数
                        praisesList,//点赞人列表
                        praisesTotalPage//点赞人总页数
                    });
                }
                return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        /// <summary>
        /// 获取社区说说详情
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getdetail")]
        public async Task<HttpResponseMessage> GetDetail(CommunityParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.openid) || parameter.cid.Equals(Guid.Empty) || parameter.mid.Equals(Guid.Empty) || parameter.CommentPageIndex <= 0 || parameter.CommentPageSize < 0
                || parameter.praisesPageIndex < 0 || parameter.praisesPageSize < 0 || string.IsNullOrEmpty(parameter.appid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
                if (user == null)
                    return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
                Guid currentUid = Guid.Parse(user.Id);
                var community = await EsCommunityManager.GetByIdAsync(parameter.cid);
                if (community == null)
                    return JsonResponseHelper.HttpRMtoJson($"community is null cid:{parameter.cid}", HttpStatusCode.OK, ECustomStatus.Fail);
                #region 发说说人的属性
                Guid communityUid = Guid.Parse(community.uid);
                var communityUser = await RedisUserOp.GetByUidAsnyc(communityUid);
                var isWriteOffer = await repo.WoerCanWriteOff(parameter.mid, communityUid);
                var isMySelf = communityUid.Equals(currentUid);
                #endregion
                #region 当前用户的属性
                bool isPraises = false;
                var praisesBiz = await EsCommunityBizManager.GetCommunityBizAsync(communityUid, currentUid, parameter.cid, (int)EComBizType.Favour);
                if (praisesBiz != null)
                    isPraises = true;
                bool isAttention = false;
                var attentionBiz = await EsCommunityBizManager.GetCommunityBizAsync(communityUid, currentUid, Guid.Empty, (int)EComBizType.Subscribe);
                if (attentionBiz != null)
                    isAttention = true;
                #endregion
                #region 该条说说的评论列表
                var Tuplecomment = await EsCommentManager.GetCommentByTopic_IdAsync(Guid.Empty, parameter.cid, parameter.CommentPageIndex, parameter.CommentPageSize);
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
                var communityBiz = await EsCommunityBizManager.GetCommunityBizListAsync(Guid.Empty, communityUid, Guid.Empty, parameter.cid, (int)EComBizType.Favour, parameter.praisesPageIndex, parameter.praisesPageSize);
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

                string fxUrl = MdWxSettingUpHelper.GenCommunityDetailUrl(parameter.appid, parameter.cid);
                var retobj = new
                {
                    isMySelf = isMySelf,//发文章的人是不是我自己
                    nickName = communityUser.NickName,
                    headerPic = communityUser.HeadImgUrl,
                    uid = community.uid,
                    isPraises = isPraises,//我是否赞过该说说
                    isWriteOffer = isWriteOffer,//是否美妆达人
                    isAttention = isAttention,//是否关注
                    cid = community.Id,
                    community.title,
                    community.content,
                    community.createtime,
                    community.lastupdatetime,
                    time = GetTimeString(community.createtime),
                    community.imgs,
                    community.praises,
                    community.hits,
                    community.transmits,
                    community.flag,
                    commentTotalPage,//评论总页数
                    commentList,//评论列表
                    commentTotalCount = Tuplecomment.Item1,//评论总行数
                    praisesList,//点赞人列表
                    praisesTotalPage,//点赞人总页数
                    fxUrl
                };
                return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }

        /// <summary>
        /// 个人中心
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getmycenter")]
        public async Task<HttpResponseMessage> GetMyCenter(CommunityParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty) || parameter.to_uid.Equals(Guid.Empty) || parameter.pageIndex < 0 || parameter.pageSize < 0
                || string.IsNullOrEmpty(parameter.appid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByIdAsync(parameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到uid:{parameter.uid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentUid = Guid.Parse(user.Id);
            var to_user = await EsUserManager.GetByIdAsync(parameter.to_uid);
            if (to_user == null)
                return JsonResponseHelper.HttpRMtoJson($"to_user is null:to_uid:{parameter.to_uid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var redisto_User = await RedisUserOp.GetByUidAsnyc(parameter.to_uid);
            if (redisto_User == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到redisto_User:{parameter.to_uid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            #region to_uid发表的说说
            var tupleCommunity = await EsCommunityManager.GetListAsync(parameter.mid, parameter.to_uid, (int)ECommunityTopicType.MMSQ, parameter.pageIndex, parameter.pageSize, (int)ECommunityStatus.已发布, "");
            int imgTotalCount = 0;
            var TupleCommCount = await EsCommunityManager.GetCountAsync(parameter.mid, parameter.to_uid, (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
            if (TupleCommCount != null)
                imgTotalCount = TupleCommCount.Item1;
            List<object> communityList = new List<object>();
            int totalPage = GetTotalPages(tupleCommunity.Item1, parameter.pageSize);
            foreach (var community in tupleCommunity.Item2)
            {
                communityList.Add(new
                {
                    community.Id,
                    community.imgs
                });
            }
            #endregion
            #region to_uid的属性
            //肤质
            string to_user_Skin = "";
            using (var coderepo = new CodeRepository())
            {
                to_user_Skin = await coderepo.GetCodeSkin(to_user.skin);
            }
            //是否核销员
            bool isWriteOffer = false;
            using (var reop = new BizRepository())
            {
                isWriteOffer = await reop.WoerCanWriteOff(parameter.mid, parameter.to_uid);
            }
            //好友数
            int myqmCount = await RedisVectorOp.GetQMCount(parameter.to_uid);
            //年龄
            int age = 0;
            if (to_user.age > 0)
            {
                DateTime birthday = CommonHelper.FromUnixTime(to_user.age);
                if (birthday < DateTime.Now)
                {
                    age = DateTime.Now.Year - birthday.Year;
                }
            }
            //是否我自己看我自己的个人中心
            bool isMySelf = false;
            if (user.Id.Equals(to_user.Id))
                isMySelf = true;
            //to_user关注别人的数量
            int AttentionCount = 0;
            var tuple1 = await EsCommunityBizManager.GetCommunityBizListAsync(Guid.Empty, Guid.Empty, parameter.to_uid, Guid.Empty, (int)EComBizType.Subscribe, 1, 1);
            if (tuple1 != null)
                AttentionCount = tuple1.Item1;
            //to_user的粉丝数量
            int fansCount = 0;
            var tuple2 = await EsCommunityBizManager.GetCommunityBizListAsync(Guid.Empty, parameter.to_uid, Guid.Empty, Guid.Empty, (int)EComBizType.Subscribe, 1, 1);
            if (tuple2 != null)
                fansCount = tuple2.Item1;
            //to_user照片被赞次数
            long imgFavourCount = 0;
            //to_user照片被赞总排名
            long imgFavourSort = 0;
            var tuple4 = await EsCommunityBizManager.GetByAggUidAsync(parameter.mid, to_user.Id, (int)EComBizType.Favour);
            if (tuple4 != null)
            {
                imgFavourCount = tuple4.Item1;
                imgFavourSort = tuple4.Item2;
            }

            #endregion
            #region 当前用户的属性
            bool isAttention = false;
            var attentionBiz = await EsCommunityBizManager.GetCommunityBizAsync(parameter.to_uid, currentUid, Guid.Empty, (int)EComBizType.Subscribe);
            if (attentionBiz != null)
                isAttention = true;
            #endregion

            string fxUrl = MdWxSettingUpHelper.GenCommunityMyCenterUrl(parameter.appid, parameter.to_uid);
            var retobj = new
            {
                totalPage = totalPage,
                communityList = communityList,
                imgTotalCount = imgTotalCount,
                redisto_User.NickName,
                redisto_User.HeadImgUrl,
                age = age,
                skin = to_user_Skin,
                backImg = to_user.backimg,
                isWriteOffer = isWriteOffer,
                isAttention = isAttention,//当前用户是否关注to_user
                myqmCount = myqmCount,//to_user好友人数
                isMySelf = isMySelf,//是否我自己
                AttentionCount = AttentionCount,//to_user关注别人的数量
                fansCount = fansCount,//to_user的粉丝数量
                imgFavourCount = imgFavourCount,//to_user照片被赞次数
                imgFavourSort = imgFavourSort,//to_user照片被赞总排名
                fxUrl
            };
            return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
        }

        #region CommunityBiz
        /// <summary>
        /// 我的排行榜
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getmytop")]
        public async Task<HttpResponseMessage> GetMyTop(CommunityParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await RedisUserOp.GetByUidAsnyc(parameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到uid:{parameter.uid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            //先计算我的排名
            //总榜排名
            int mySumRank = 0;
            var tuple1 = await EsCommunityBizManager.GetByAggUidAsync(parameter.mid, user.Uid, (int)EComBizType.Favour);
            if (tuple1 != null)
                mySumRank = tuple1.Item2;
            //上月排名
            var lastMonthBegin = (int)CommonHelper.ToUnixTime(DateTime.Now.AddMonths(-1).AddDays(1 - DateTime.Now.Day));  //上个月起始时间
            var lastMonthEnd = (int)CommonHelper.ToUnixTime(DateTime.Now.AddDays(-DateTime.Now.Day));//上个月截至时间:
            int myLastmonthRank = 0;
            var tuple2 = await EsCommunityBizManager.GetByAggUidAsync(parameter.mid, user.Uid, (int)EComBizType.Favour, lastMonthBegin, lastMonthEnd);
            if (tuple2 != null)
                myLastmonthRank = tuple2.Item2;
            //本月排名
            int myNowmonthRank = 0;
            var nowMonthBegin = (int)CommonHelper.ToUnixTime(DateTime.Now.AddDays(1 - DateTime.Now.Day));
            var nowMonthEnd = (int)CommonHelper.GetUnixTimeNow();
            var tuple3 = await EsCommunityBizManager.GetByAggUidAsync(parameter.mid, user.Uid, (int)EComBizType.Favour, nowMonthBegin, nowMonthEnd);
            if (tuple3 != null)
                myNowmonthRank = tuple3.Item2;
            var retobj = new
            {
                mySumRank,
                myLastmonthRank,
                myNowmonthRank,
                user.HeadImgUrl,
                user.NickName
            };
            return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 点赞排行榜
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("gettoppraises")]
        public async Task<HttpResponseMessage> getTopPraises(CommunityParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.bizType > 2 || parameter.bizType < 1
                || parameter.pageIndex < 0 || parameter.pageSize < 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            // parameter.bizType:1本月排榜，2：总排行榜
            int? nowMonthBegin = null, nowMonthEnd = null;
            if (parameter.bizType == 1)
            {
                nowMonthBegin = (int)CommonHelper.ToUnixTime(DateTime.Now.AddDays(1 - DateTime.Now.Day));
                nowMonthEnd = (int)CommonHelper.GetUnixTimeNow();
            }
            List<object> retObj = new List<object>();
            int totalPage = 0;
            var tupleTop = await EsCommunityBizManager.GetByAggUidAsync(parameter.mid, null, (int)EComBizType.Favour, parameter.pageIndex, parameter.pageSize, nowMonthBegin, nowMonthEnd);
            if (tupleTop != null && tupleTop.Item1 > 0)
            {
                totalPage = GetTotalPages(tupleTop.Item1, parameter.pageSize);
                foreach (var item in tupleTop.Item2)
                {
                    Guid uid = Guid.Parse(item.Key);
                    var redisUser = await RedisUserOp.GetByUidAsnyc(uid);
                    retObj.Add(new
                    {
                        uid,
                        redisUser.HeadImgUrl,
                        redisUser.NickName,
                        PraiseCount = item.DocCount
                    });
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = retObj }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 增加一条CommunityBiz
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("addcommunitybiz")]
        public async Task<HttpResponseMessage> addCommunityBiz(CommunityParameter parameter)
        {
            if (parameter == null || parameter.cid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || parameter.to_uid.Equals(Guid.Empty) || parameter.bizType < 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentUid = Guid.Parse(user.Id);
            var to_user = await EsUserManager.GetByIdAsync(parameter.to_uid);
            if (to_user == null)
                return JsonResponseHelper.HttpRMtoJson($"to_user is null:to_uid:{parameter.to_uid}", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid to_UserGuid = Guid.Parse(to_user.Id);
            var redisUser = await RedisUserOp.GetByUidAsnyc(currentUid);
            if (redisUser == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到currentUid:{currentUid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            IndexCommunityBiz biz = new IndexCommunityBiz()
            {
                Id = Guid.NewGuid().ToString(),
                mid = to_user.mid,
                from_id = user.Id,//点赞人或点关注的人
                uid = parameter.to_uid.ToString(),//被点赞人或被关注的人(发文章的人)
                biztype = parameter.bizType,//点赞1关注2
                timestamp = (int)CommonHelper.GetUnixTimeNow(),
                isread = 0
            };
            //存数据逻辑判断
            switch (parameter.bizType)
            {
                case (int)EComBizType.Favour:
                    var community = await EsCommunityManager.GetByIdAsync(parameter.cid);
                    if (community == null)
                        return JsonResponseHelper.HttpRMtoJson($"community is null:cid:{parameter.cid}", HttpStatusCode.OK, ECustomStatus.Fail);
                    biz.bizid = community.Id;//点赞为文章id
                    break;
                case (int)EComBizType.Subscribe:
                    if (to_user.Id.Equals(user.Id))
                        return JsonResponseHelper.HttpRMtoJson("自己不能关注自己", HttpStatusCode.OK, ECustomStatus.Fail);
                    var isSubscribe = await EsCommunityBizManager.GetCommunityBizAsync(to_UserGuid, currentUid, to_UserGuid, (int)EComBizType.Subscribe);
                    if (isSubscribe != null)
                        return JsonResponseHelper.HttpRMtoJson("已关注", HttpStatusCode.OK, ECustomStatus.Fail);
                    biz.bizid = to_user.Id;//关注为关注人id
                    biz.extralid = parameter.cid.ToString();
                    break;
                case (int)EComBizType.NoticBoardFavour:
                    var noticboard = await EsNoticeBoardManager.GetByidAsync(parameter.cid);
                    if (noticboard == null)
                        return JsonResponseHelper.HttpRMtoJson($"noticboard is null:cid:{parameter.cid}", HttpStatusCode.OK, ECustomStatus.Fail);
                    biz.bizid = noticboard.Id;
                    biz.mid = user.mid;
                    break;
                default:
                    return JsonResponseHelper.HttpRMtoJson($"no bizType:bizType:{parameter.bizType}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var flag = await EsCommunityBizManager.AddBizAsync(biz);
            if (flag)
            {
                using (var repo = new BizRepository())
                {
                    if (parameter.bizType == (int)EComBizType.Favour)//如果是点赞,对该说说的点赞总数加1
                    {
                        var dbtempcomm = await repo.UpdateCommunityPraisesAsync(parameter.cid);
                        if (dbtempcomm != null)
                        {
                            var tempindex = EsCommunityManager.GenObject(dbtempcomm);
                            await EsCommunityManager.AddOrUpdateAsync(tempindex);
                        }
                    }
                    if (parameter.bizType == (int)EComBizType.NoticBoardFavour)//如果是对发现美点赞，对该文章点赞总数加1
                    {
                        var dbnoticboard = await repo.UpdateNoticeBoardPraise_countAsync(parameter.cid);
                        if (dbnoticboard != null)
                        {
                            var indexnotic = await EsNoticeBoardManager.GenObjectAsync(dbnoticboard.nid);
                            await EsNoticeBoardManager.AddOrUpdateAsync(indexnotic);
                        }
                    }
                }
            }
            var retobj = new
            {
                NickName = redisUser.NickName,
                HeadImgUrl = redisUser.HeadImgUrl,
                Id = biz.Id,
                from_id = biz.from_id,
                uid = biz.uid
            };
            return JsonResponseHelper.HttpRMtoJson(new { isOk = flag, retobj }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 删除CommunityBiz一条数据
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("delcommunitybiz")]
        public async Task<HttpResponseMessage> delCommunityBiz(CommunityParameter parameter)
        {
            if (parameter == null || parameter.uid.Equals(Guid.Empty) || parameter.to_uid.Equals(Guid.Empty) || parameter.cid.Equals(Guid.Empty) || parameter.bizType < 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByIdAsync(parameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到uid:{parameter.uid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            var to_user = await EsUserManager.GetByIdAsync(parameter.to_uid);
            if (to_user == null)
                return JsonResponseHelper.HttpRMtoJson($"to_user is null:to_uid:{parameter.to_uid}", HttpStatusCode.OK, ECustomStatus.Fail);
            // parameter.bizType,1:取消点赞cid:此文章的ID 2:取消关注cid:to_uid
            Guid bizId = Guid.Empty;
            if (parameter.bizType == (int)EComBizType.Favour)
                bizId = parameter.cid;
            else if (parameter.bizType == (int)EComBizType.Subscribe)
                bizId = parameter.to_uid;
            var flag = await EsCommunityBizManager.DelBizAsync(parameter.to_uid, parameter.uid, bizId, parameter.bizType);
            return JsonResponseHelper.HttpRMtoJson(new { isOk = flag }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 获取我的关注人的说说
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getmysubscribelist")]
        public async Task<HttpResponseMessage> GetMySubscribelist(CommunityParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.openid) || parameter.pageIndex < 0 || parameter.pageSize < 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentUid = Guid.Parse(user.Id);
            //获取我关注的人的uidList
            var tupleUserList = await EsCommunityBizManager.GetUidsByFrom_IdAsync(user.Id, (int)EComBizType.Subscribe, parameter.pageIndex, parameter.pageSize);
            List<object> retObj = new List<object>();
            int totalPage = 0;
            if (tupleUserList != null && tupleUserList.Item1 > 0)
            {
                //根据我关注人的uidList获取我关注人的说说
                var uidList = tupleUserList.Item2;
                var commnuityList = await EsCommunityManager.GetListAsync(uidList, (int)ECommunityTopicType.MMSQ, parameter.pageIndex, parameter.pageSize, (int)ECommunityStatus.已发布);
                totalPage = GetTotalPages(commnuityList.Item1, parameter.pageSize);
                if (commnuityList != null && commnuityList.Item1 > 0)
                {
                    foreach (var commnuity in commnuityList.Item2)
                    {
                        Guid commnuityUid = Guid.Parse(commnuity.uid);
                        var commnuityUser = await RedisUserOp.GetByUidAsnyc(commnuityUid);
                        if (commnuityUser == null) continue;
                        var commnuityUserGZCount = await EsCommunityBizManager.GetCommunityBizListAsync(Guid.Empty, commnuityUid, Guid.Empty, Guid.Empty, (int)EComBizType.Subscribe, 1, 1);
                        retObj.Add(new
                        {
                            commnuityUser.HeadImgUrl,
                            commnuityUser.NickName,
                            commnuity.uid,
                            firstImg = commnuity.imgs?.FirstOrDefault(),
                            ImgCount = commnuity.imgs == null ? 0 : commnuity.imgs.Count,
                            praises = commnuity.praises,//这篇文章的点赞总数
                            UserGZCount = commnuityUserGZCount.Item1//他的关注人数
                        });
                    }
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = retObj }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 获取我关注的或关注我的人的列表
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getmygzorfansuserlist")]
        public async Task<HttpResponseMessage> GetMyGZOrFansUerList(CommunityParameter parameter)
        {
            //parameter.bizType用来区分1:我的关注2 :我的粉丝
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.uid.Equals(Guid.Empty) || parameter.bizType > 2 || parameter.pageIndex < 0 || parameter.pageSize < 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByIdAsync(parameter.uid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到uid:{parameter.uid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            List<object> retobj = new List<object>();
            //获取我关注人的uidList
            Tuple<int, List<string>> tupleUserList = null;
            if (parameter.bizType == 1) //我的关注uidList
                tupleUserList = await EsCommunityBizManager.GetUidsByFrom_IdAsync(user.Id, (int)EComBizType.Subscribe, parameter.pageIndex, parameter.pageSize);
            else if (parameter.bizType == 2)//关注我的uidList
                tupleUserList = await EsCommunityBizManager.GetFrom_idsByUidAsync(user.Id, (int)EComBizType.Subscribe, parameter.pageIndex, parameter.pageSize);
            if (tupleUserList != null && tupleUserList.Item1 > 0)
            {
                //根据uidList计算出这些人的总照片数和被赞次数
                foreach (var uid in tupleUserList.Item2)
                {
                    Guid gzUid = Guid.Parse(uid);
                    var redisUser = await RedisUserOp.GetByUidAsnyc(gzUid);
                    if (user == null) continue;
                    var commnuity = await EsCommunityManager.GetCountAsync(parameter.mid, gzUid, (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
                    int imgPraisesCount = 0;//照片被赞次数
                    int imgCount = 0;//总照片数
                    if (commnuity != null)
                    {
                        imgCount = commnuity.Item1;
                        imgPraisesCount = commnuity.Item2;
                    }
                    bool isAttention = false; //是否关注
                    switch (parameter.bizType)//parameter.bizType用来区分1:我的关注2 :我的粉丝
                    {
                        case 1:
                            isAttention = true;
                            break;
                        case 2:
                            var tempBiz = await EsCommunityBizManager.GetCommunityBizAsync(gzUid, parameter.uid, Guid.Empty, (int)EComBizType.Subscribe);
                            if (tempBiz != null)
                                isAttention = true;
                            break;
                        default:
                            break;
                    }
                    retobj.Add(new
                    {
                        imgCount,
                        imgPraisesCount,
                        redisUser.NickName,
                        redisUser.HeadImgUrl,
                        redisUser.Uid,
                        isAttention = isAttention
                    });
                }
                return JsonResponseHelper.HttpRMtoJson(new { totalpage = GetTotalPages(tupleUserList.Item1, parameter.pageSize), glist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalpage = 0, glist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
        }
        #endregion

        #region Comment
        [HttpPost]
        [Route("addcomment")]
        public async Task<HttpResponseMessage> addComment(CommunityParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.content) || parameter.topictype < 0 || parameter.cid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            //不一定非要评论此表的内容
            IndexCommunity community = new IndexCommunity();
            IndexUser communityUser = new IndexUser();
            if (parameter.topictype == (int)ECommunityTopicType.MMSQ)
            {
                community = await EsCommunityManager.GetByIdAsync(parameter.cid);
                if (community == null)
                    return JsonResponseHelper.HttpRMtoJson($"community is null,cid:{parameter.cid}", HttpStatusCode.OK, ECustomStatus.Fail);
                communityUser = await EsUserManager.GetByIdAsync(Guid.Parse(community.uid));
                if (communityUser == null || communityUser.Id.Equals(Guid.Empty))
                    return JsonResponseHelper.HttpRMtoJson($"communityUser null uid:{community.uid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            else if (parameter.topictype == (int)ECommunityTopicType.NoticeBoard)
            {
                var noticeboard = await EsNoticeBoardManager.GetByidAsync(parameter.cid);
                if (noticeboard == null)
                    return JsonResponseHelper.HttpRMtoJson($"noticboard is null,cid:{parameter.cid}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            else
            {
                return JsonResponseHelper.HttpRMtoJson($"没有此类型topictype:{parameter.topictype}", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentUid = Guid.Parse(user.Id);
            var redisUser = await RedisUserOp.GetByUidAsnyc(currentUid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"redisUser is null:uid:{currentUid}", HttpStatusCode.OK, ECustomStatus.Fail);
            Dictionary<string, string> dicNames = new Dictionary<string, string>();
            Dictionary<string, string> dicPics = new Dictionary<string, string>();
            dicNames[redisUser.Uid] = redisUser.NickName;
            dicPics[redisUser.Uid] = redisUser.HeadImgUrl;
            IndexComment comment = new IndexComment()
            {
                Id = Guid.NewGuid().ToString(),
                topic_id = parameter.cid.ToString(),
                topic_type = parameter.topictype,
                content = parameter.content,
                from_uid = user.Id,
                from_mid = user.mid,
                timestamp = Convert.ToInt32(CommonHelper.GetUnixTimeNow()),
                status = 1,
                Com_Replys = new List<IndexCom_Reply>(),
                dic_nickname = dicNames,
                dic_headerpic = dicPics,
            };
            var flag = await EsCommentManager.addCommentAsync(comment);
            if (flag)//如果评论成功，则记录一条提醒数据
            {
                if (parameter.topictype == (int)ECommunityTopicType.MMSQ)
                {
                    IndexCommunityBiz biz = new IndexCommunityBiz()
                    {
                        Id = Guid.NewGuid().ToString(),
                        uid = community.uid,
                        mid = communityUser.mid,
                        from_id = user.Id,
                        bizid = comment.Id,
                        biztype = (int)EComBizType.Comment,
                        extralid = community.Id,
                        isread = 0,
                        timestamp = (int)CommonHelper.GetUnixTimeNow()
                    };
                    await EsCommunityBizManager.AddBizAsync(biz);
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new
            {
                isOk = flag,
                comment = comment
            }, HttpStatusCode.OK, ECustomStatus.Success);

        }
        [HttpPost]
        [Route("addreply")]
        public async Task<HttpResponseMessage> AddReply(CommunityParameter parameter)
        {
            if (parameter == null || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.content) || parameter.to_uid.Equals(Guid.Empty) || parameter.commentId.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到openid:{parameter.openid}的用户！", HttpStatusCode.OK, ECustomStatus.Fail);
            Guid currentUid = Guid.Parse(user.Id);
            var redisUser = await RedisUserOp.GetByUidAsnyc(currentUid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"redisUser is null:uid:{currentUid}", HttpStatusCode.OK, ECustomStatus.Fail);
            IndexComment index = await EsCommentManager.GetByIdAsync(parameter.commentId);
            if (index == null)
                return JsonResponseHelper.HttpRMtoJson($"未找到该评论:commentid:{parameter.commentId}", HttpStatusCode.OK, ECustomStatus.Fail);
            //判断自己不能回复自己
            if (currentUid.Equals(parameter.to_uid))
                return JsonResponseHelper.HttpRMtoJson($"自己无法回复自己！", HttpStatusCode.OK, ECustomStatus.Fail);
            var to_user = await EsUserManager.GetByIdAsync(parameter.to_uid);
            if (to_user == null)
                return JsonResponseHelper.HttpRMtoJson($"to_user is null:to_uid:{parameter.to_uid}", HttpStatusCode.OK, ECustomStatus.Fail);
            IndexCom_Reply reply = new IndexCom_Reply()
            {
                Id = Guid.NewGuid().ToString(),
                content = parameter.content,
                from_uid = redisUser.Uid,
                status = 1,
                to_uid = parameter.to_uid.ToString(),
                timestamp = Convert.ToInt32(CommonHelper.GetUnixTimeNow()),
            };
            index.Com_Replys = index.Com_Replys ?? new List<IndexCom_Reply>();
            index.Com_Replys.Add(reply);
            index.dic_nickname[redisUser.Uid] = redisUser.NickName;
            index.dic_headerpic[redisUser.Uid] = redisUser.HeadImgUrl;
            var flag = await EsCommentManager.AddReplyAsync(index);
            if (flag)//如果回复成功，则记录一条提醒数据
            {
                if (index.topic_type == (int)ECommunityTopicType.MMSQ)
                {
                    IndexCommunityBiz biz = new IndexCommunityBiz()
                    {
                        Id = Guid.NewGuid().ToString(),
                        uid = reply.to_uid,
                        mid = to_user.mid,
                        from_id = reply.from_uid,
                        bizid = reply.Id,
                        biztype = (int)EComBizType.Reply,
                        extralid = index.Id,
                        isread = 0,
                        timestamp = (int)CommonHelper.GetUnixTimeNow()
                    };
                    await EsCommunityBizManager.AddBizAsync(biz);
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { isOk = flag, reply = reply, index.dic_nickname, index.dic_headerpic }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getcommentlist")]
        public async Task<HttpResponseMessage> GetCommentList(CommunityParameter parameter)
        {
            if (parameter == null || parameter.cid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || parameter.pageIndex <= 0 || parameter.pageSize < 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var community = await EsCommunityManager.GetByIdAsync(parameter.cid);
            if (community == null)
                return JsonResponseHelper.HttpRMtoJson($"commu is null cid:{parameter.cid}!", HttpStatusCode.OK, ECustomStatus.Fail);
            var tuple = await EsCommentManager.GetCommentByTopic_IdAsync(Guid.Empty, parameter.cid, parameter.pageIndex, parameter.pageSize);
            List<object> commentList = new List<object>();
            if (tuple != null)
            {
                #region 该条说说的评论列表

                int commentTotalPage = GetTotalPages(tuple.Item1, parameter.pageSize);
                foreach (var comment in tuple.Item2)
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
            }
            return JsonResponseHelper.HttpRMtoJson(commentList, HttpStatusCode.OK, ECustomStatus.Success);
        }

        /// <summary>
        /// 获取我的通知
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getnoticelist")]
        public async Task<HttpResponseMessage> GetMyNotice(CommunityParameter parameter)
        {
            if (parameter.uid == Guid.Empty || parameter.pageIndex <= 0 || parameter.pageSize <= 0 || parameter.bizType <= 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            List<NoticeObj> listRes = new List<NoticeObj>();
            int totalPage = 0;
            Tuple<int, List<IndexCommunityBiz>> tuple = null;
            List<int> type = null;
            if (parameter.bizType != (int)EComBizType.Comment)
                type = new List<int> { parameter.bizType };
            else type = new List<int> { (int)EComBizType.Comment, (int)EComBizType.Reply };
            tuple = await EsCommunityBizManager.GetCommunityBizListAsync(parameter.uid, Guid.Empty, Guid.Empty, type, parameter.pageIndex, parameter.pageSize);
            bool flag = await EsCommunityBizManager.UpdateCommunityAsync(parameter.uid, type);//更新通知为已读
            var list = tuple.Item2;
            var totalCount = tuple.Item1;
            totalPage = (int)Math.Ceiling(totalCount / 1.0 / parameter.pageSize);
            if (parameter.bizType == (int)EComBizType.Favour || parameter.bizType == (int)EComBizType.Subscribe)
            {
                foreach (var item in list)
                {
                    Guid fromUid = Guid.Parse(item.from_id);
                    if (fromUid == parameter.uid) continue;
                    var user = await RedisUserOp.GetByUidAsnyc(fromUid);
                    if (user != null)
                    {
                        NoticeObj obj = new NoticeObj()
                        {
                            uid = item.from_id,
                            name = user.NickName,
                            headerpic = user.HeadImgUrl,
                            bizType = parameter.bizType,
                            time = GetTimeString(item.timestamp),
                            timestamp = item.timestamp
                        };
                        if (parameter.bizType == (int)EComBizType.Favour)
                        {
                            var community = await EsCommunityManager.GetByIdAsync(Guid.Parse(item.bizid));
                            if (community != null && community.imgs != null && community.imgs.Count > 0)
                            {
                                obj.content = community.imgs[0];
                            }
                        }
                        listRes.Add(obj);
                    }
                }
            }
            else
            {
                foreach (var item in list)
                {
                    if (item.from_id == parameter.uid.ToString()) continue;
                    if (item.biztype == (int)EComBizType.Reply)
                    {
                        var comment = await EsCommentManager.GetByIdAsync(Guid.Parse(item.extralid));
                        if (comment != null)
                        {
                            var reply = comment.Com_Replys.Where(r => r.Id == item.bizid).FirstOrDefault();
                            if (reply != null)
                            {
                                listRes.Add(new NoticeObj
                                {
                                    uid = reply.from_uid,
                                    name = comment.dic_nickname[reply.from_uid],
                                    headerpic = comment.dic_headerpic[reply.from_uid],
                                    content = reply.content,
                                    topicId = comment.topic_id,
                                    bizType = item.biztype,
                                    timestamp = reply.timestamp,
                                    time = GetTimeString(reply.timestamp)
                                });
                            }
                        }
                    }
                    if (item.biztype == (int)EComBizType.Comment)
                    {
                        var comment = await EsCommentManager.GetByIdAsync(Guid.Parse(item.bizid));
                        if (comment != null)
                        {
                            listRes.Add(new NoticeObj
                            {
                                uid = comment.from_uid,
                                name = comment.dic_nickname[comment.from_uid],
                                headerpic = comment.dic_headerpic[comment.from_uid],
                                content = comment.content,
                                topicId = comment.topic_id,
                                bizType = item.biztype,
                                timestamp = comment.timestamp,
                                time = GetTimeString(comment.timestamp)
                            });
                        }
                    }
                }
            }
            var glist = listRes.OrderByDescending(r => r.timestamp).ToList();
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = glist }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        #endregion

        [HttpPost]
        [Route("getusersnearby")]
        public async Task<HttpResponseMessage> GetUsersNearby(CommunityParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || parameter.latitude == 0 || parameter.longitude == 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            var tuple = await EsBizLogStatistics.GetUsersNear(parameter.mid, parameter.latitude, parameter.longitude);
            var list = tuple.Item2;
            Dictionary<string, Coordinate> dic = new Dictionary<string, Coordinate>();
            List<UserNearby> listRes = new List<UserNearby>();
            foreach (var item in list)
            {
                dic[item.UserUuid] = item.Location;
            }
            string user_Skin = "";
            int age = 0;
            int imgTotalCount = 0;
            int imgFavourCount = 0;
            using (var coderepo = new CodeRepository())
            {
                foreach (var item in dic.Keys)
                {
                    Guid uid = Guid.Parse(item);
                    var indexUser = await EsUserManager.GetByIdAsync(uid);
                    var user = await RedisUserOp.GetByUidAsnyc(uid);
                    if (indexUser != null && user != null)
                    {
                        user_Skin = await coderepo.GetCodeSkin(indexUser.skin);
                        if (indexUser.age > 0)
                        {
                            DateTime birthday = CommonHelper.FromUnixTime(indexUser.age);
                            if (birthday < DateTime.Now)
                            {
                                age = DateTime.Now.Year - birthday.Year;
                            }
                        }
                        var TupleCommCount = await EsCommunityManager.GetCountAsync(parameter.mid, uid, (int)ECommunityTopicType.MMSQ, (int)ECommunityStatus.已发布);
                        if (TupleCommCount != null)
                        {
                            imgTotalCount = TupleCommCount.Item1;
                            imgFavourCount = TupleCommCount.Item2;
                        }
                        //var tuple4 =await EsCommunityBizManager.GetByAggUidAsync(parameter.mid, item, (int)EComBizType.Favour);
                        //if (tuple4 != null) imgFavourCount = tuple4.Item1;
                        Coordinate cor = dic[item];
                        UserNearby un = new UserNearby();
                        un.uid = item;
                        un.name = user.NickName;
                        un.headerpic = user.HeadImgUrl;
                        un.skin = user_Skin;
                        un.age = age;
                        un.imgCount = imgTotalCount;
                        un.favourCount = imgFavourCount;
                        un.distance = GeoHelper.GetDistance(cor.Lat, cor.Lon, parameter.latitude, parameter.longitude);
                        listRes.Add(un);
                    }
                }
                listRes = listRes.OrderBy(u => u.distance).ToList();
            }
            return JsonResponseHelper.HttpRMtoJson(new { glist = listRes }, HttpStatusCode.OK, ECustomStatus.Success);
        }
        public class UserNearby
        {
            public string uid { get; set; }
            public string name { get; set; }
            public string headerpic { get; set; }
            public string skin { get; set; }
            public int age { get; set; }
            public int imgCount { get; set; }
            public int favourCount { get; set; }
            public double distance { get; set; }
        }
        private int GetTotalPages(int totalCount, int pageSize)
        {
            if (totalCount <= 0)
                return 0;
            if (totalCount % pageSize == 0)
                return totalCount / pageSize;
            return totalCount / pageSize + 1;
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

        private class NoticeObj
        {
            public string uid { get; set; }
            public string name { get; set; }
            public string headerpic { get; set; }
            public string content { get; set; }
            public string topicId { get; set; }
            public int bizType { get; set; }
            public int timestamp { get; set; }
            public string time { get; set; }
        }
    }
}