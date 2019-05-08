using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Biz.Merchant;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.WeChat.Filters;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Product;
using MD.Lib.ElasticSearch.MD;
using System.Web;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using System.Collections.Concurrent;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Model.Configuration.Aliyun;
using Senparc.Weixin.MP.AdvancedAPIs;
using MD.Lib.Weixin.Component;
using System.IO;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/product")]
    [AccessFilter]
    public class ProductController : ApiController
    {
        [HttpPost]
        [Route("getproductcatalogue")]
        public async Task<HttpResponseMessage> getproductcatalogue(ProductParameter parameter)
        {
            try
            {
                using (var coderepo = new CodeRepository())
                {
                    var ret = await coderepo.GetAllProductCategoriesAsync();
                    if (ret == null || ret.Count == 0)
                        return JsonResponseHelper.HttpRMtoJson($"无商品分类数据", HttpStatusCode.OK, ECustomStatus.Fail);
                    return JsonResponseHelper.HttpRMtoJson(ret, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
            catch (Exception ex)
            {
                return JsonResponseHelper.HttpRMtoJson(ex.Message, HttpStatusCode.OK, ECustomStatus.Fail);
            }

        }

        [HttpPost]
        [Route("getlist")]
        public async Task<HttpResponseMessage> getlist(ProductParameter parameter)
        {
            if (parameter == null || parameter.mid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid) || parameter.QueryStr == null)
                return JsonResponseHelper.HttpRMtoJson($"parameter error,mid:{parameter.mid},opeinid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            int pageSize = MdWxSettingUpHelper.GetPageSize();
            var tuple = await EsProductManager.SearchList(parameter.QueryStr, parameter.mid, parameter.pageIndex, pageSize, "scorePeopleCount", parameter.category);
            if (tuple == null || tuple.Item2 == null)
                return JsonResponseHelper.HttpRMtoJson($"没有取到mid:{parameter.mid}的商品信息！", HttpStatusCode.OK, ECustomStatus.Fail);
            int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
            List<object> retobj = new List<object>();
            string fxUrl = MdWxSettingUpHelper.GenProductListUrl(parameter.appid);
            foreach (var product in tuple.Item2)
            {
                retobj.Add(new
                {
                    pid = product.Id,
                    pic1 = product.advertise_pic_1,
                    pic2 = product.advertise_pic_2,
                    pic3 = product.advertise_pic_3,
                    product.name,
                    origin_price = product.price / 100.00,
                    product.standard,
                    product.avgScore,
                    product.scorePeopleCount,
                    product.grassCount,
                    fxUrl
                });
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getdetail")]
        public async Task<HttpResponseMessage> getdetail(ProductParameter parameter)
        {
            if (parameter == null || parameter.pid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || string.IsNullOrEmpty(parameter.appid) ||
                parameter.pageIndex <= 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error,pid:{parameter.pid},opeinid:{parameter.openid},appid:{parameter.appid},pageIndex:{parameter.pageIndex}",
                    HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                using (var codebiz = new CodeRepository())
                {
                    int pageSize = MdWxSettingUpHelper.GetPageSize();
                    //当前用户信息
                    var currentuser = await repo.UserGetByOpenIdAsync(parameter.openid);
                    if (currentuser == null)
                        return JsonResponseHelper.HttpRMtoJson($"user is null,openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
                    //商品信息
                    var product = await EsProductManager.GetByPidAsync(parameter.pid);
                    if (product == null)
                        return JsonResponseHelper.HttpRMtoJson($"product is null,pid:{parameter.pid}", HttpStatusCode.OK, ECustomStatus.Fail);
                    //商品详情
                    string html = HttpUtility.HtmlDecode(product.description).Replace("\"", "\'");
                    //是否正在拼团
                    bool isPTing = false;
                    string ptUrl = "";
                    var group = await EsGroupManager.GetByPidAsync(parameter.pid, new List<int>() { (int)EGroupStatus.已发布 }, true);
                    if (group != null)
                    {
                        isPTing = true;
                        ptUrl = MdWxSettingUpHelper.GenGroupDetailUrl(parameter.appid, Guid.Parse(group.Id));
                    }
                    //该用户是否评论过
                    var myComment = await EsProductCommentManager.GetByPidAndUidAsync(parameter.pid, currentuser.uid);
                    //该用户是否种草过
                    var myGrass = await EsProductGrassManager.GetByPidAndUidAsync(parameter.pid, currentuser.uid);
                    //总评论
                    var tuple = await EsProductCommentManager.SearchAsync("", parameter.pid, parameter.pageIndex, pageSize);
                    double sumTotal = tuple.Item1;
                    int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
                    var commentList = new List<object>();
                    string fxUrl = MdWxSettingUpHelper.GenProductDetailUrl(parameter.appid, parameter.pid);

                    var codeSkinList = await codebiz.GetAllCodeSkinAsync();//肤质字典
                    var writeofferList = await repo.GetWOerByMid2Async(Guid.Parse(product.mid), true);//该商家下的所有核销员
                    #region  评论列表
                    foreach (var comment in tuple.Item2)
                    {
                        //评论人信息
                        Guid common_uid = Guid.Parse(comment.uid);
                        var common_user = await EsUserManager.GetByIdAsync(common_uid);
                        if (common_user == null)
                            continue;
                        UserInfoRedis userRedis =
                            await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(common_user.openid);
                        //我是否对此条评论点赞
                        var mycommentpraise = await EsProductCommentPraiseManager.GetByPcidAndUidAsync(Guid.Parse(comment.Id), currentuser.uid);
                        commentList.Add(new
                        {
                            pcid = comment.Id,
                            comment.comment,
                            comment.score,
                            comment.comment_reply,
                            imglist = string.IsNullOrEmpty(comment.imglist) ? null : comment.imglist.Split('|'),
                            comment.praise_count,//点赞数
                            comment.u_age,//年龄
                            u_skin = codeSkinList.ContainsKey(comment.u_skin) ? codeSkinList[comment.u_skin] : "",//肤质
                            name = userRedis.NickName,//昵称
                            userRedis.HeadImgUrl,//头像
                            isessence = comment.isessence == 1,//是否加精
                            isFriend = await RedisVectorOp.IsExistsQM(currentuser.uid, common_uid),//是否好友
                            isTarento = writeofferList.Where(p => p.uid.Equals(common_uid)).FirstOrDefault() != null,//是否美妆达人
                            isPraise = mycommentpraise != null,//我是否赞过此评论
                        });
                    }
                    #endregion
                    #region 年龄结构groupby
                    int range1 = 0, range2 = 0, range3 = 0, range4 = 0, range5 = 0;
                    Dictionary<string, double> ageDic = new Dictionary<string, double>();
                    foreach (var age in tuple.Item3)
                    {
                        int u_age = int.Parse(age.Key);
                        if (u_age <= 18)
                            range1 += (int)age.DocCount;
                        else if (u_age >= 19 && u_age <= 24)
                            range2 += (int)age.DocCount;
                        else if (u_age >= 25 && u_age <= 30)
                            range3 += (int)age.DocCount;
                        else if (u_age >= 31 && u_age <= 39)
                            range4 += (int)age.DocCount;
                        else if (u_age >= 40)
                            range5 += (int)age.DocCount;
                    }
                    ageDic.Add("18岁以下", (sumTotal == 0 ? 0 : Math.Round(range1 / sumTotal * 100, 2)));
                    ageDic.Add("19-24岁", (sumTotal == 0 ? 0 : Math.Round(range2 / sumTotal * 100, 2)));
                    ageDic.Add("25-30岁", (sumTotal == 0 ? 0 : Math.Round(range3 / sumTotal * 100, 2)));
                    ageDic.Add("31-39岁", (sumTotal == 0 ? 0 : Math.Round(range4 / sumTotal * 100, 2)));
                    ageDic.Add("40岁以上", (sumTotal == 0 ? 0 : Math.Round(range5 / sumTotal * 100, 2)));
                    #endregion
                    #region 肤质比例groupby
                    Dictionary<string, double> skinDic = new Dictionary<string, double>();
                    foreach (var codeskin in codeSkinList)
                    {
                        if (sumTotal > 0)
                        {
                            var agg_skin = tuple.Item4.Where(p => p.Key.Equals(codeskin.Key.ToString())).FirstOrDefault(); //根据code取出对应的统计数据
                            if (agg_skin == null)
                                skinDic.Add(codeskin.Value, 0);
                            else
                                skinDic.Add(codeskin.Value, agg_skin.DocCount);
                        }
                        else
                        {
                            skinDic.Add(codeskin.Value, 0);
                        }
                    }
                    #endregion
                    var retobj = new
                    {
                        pid = product.Id,
                        pic1 = product.advertise_pic_1,
                        pic2 = product.advertise_pic_2,
                        pic3 = product.advertise_pic_3,
                        product.name,
                        origin_price = product.price / 100.00,
                        product.standard,
                        product.avgScore,
                        product.scorePeopleCount,
                        product.grassCount,
                        description = html,
                        isPTing,//是否拼团中
                        ptUrl,//如果是拼团中，返回拼团的分享链接，否则返回""
                        isSetUser = currentuser.age > 0 && currentuser.skin > 0,//是否设置年龄与肤质
                        isSendingGrass = myGrass != null,//是否种草过
                        isComment = myComment != null,//是否评论过
                        myCommentPcid = myComment?.Id,//如果我评论过，返回我的评论pcid
                        myCommentUid = myComment?.uid,//如果我评论过，返回我的评论uid
                        commentList,//评论列表
                        rangeAgeList = ageDic.ToList(),//年龄比例
                        skinList = skinDic.ToList(),//肤质比例
                        fxUrl
                    };
                    return JsonResponseHelper.HttpRMtoJson(new { retobj, commentTotal = totalPage }, HttpStatusCode.OK, ECustomStatus.Success);
                }
            }
        }
        #region 商品评论
        [HttpPost]
        [Route("productcomment")]
        public async Task<HttpResponseMessage> productcomment(ProductParameter parameter)
        {
            if (parameter == null || parameter.pid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || parameter.score == null || string.IsNullOrEmpty(parameter.comment))
                return JsonResponseHelper.HttpRMtoJson($"parameter error", HttpStatusCode.OK, ECustomStatus.Fail);
            var product = await EsProductManager.GetByPidAsync(parameter.pid);
            if (product == null)
                return JsonResponseHelper.HttpRMtoJson($"product is null,pid:{parameter.pid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"esuser is null,openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            if (user.age == 0 || user.skin == 0)
                return JsonResponseHelper.HttpRMtoJson($"请先前往个人中心设置年龄和肤质！", HttpStatusCode.OK, ECustomStatus.Fail);
            int age = 0;
            if (user.age > 0)
            {
                DateTime birthday = CommonHelper.FromUnixTime(user.age);
                if (birthday < DateTime.Now)
                {
                    age = DateTime.Now.Year - birthday.Year;
                }
            }
            ProductComment comment = new ProductComment() { pcid = Guid.NewGuid() };
            //我是否评论过
            var myComment = await EsProductCommentManager.GetByPidAndUidAsync(parameter.pid, Guid.Parse(user.Id));
            if (myComment != null)
            {
                comment.pcid = Guid.Parse(myComment.Id);
                comment.imglist = myComment.imglist;
            }
            comment.pid = parameter.pid;
            comment.uid = Guid.Parse(user.Id);
            comment.mid = Guid.Parse(product.mid);
            comment.u_age = age;
            comment.u_skin = user.skin;
            comment.score = parameter.score;
            comment.comment = parameter.comment;
            //评论图片
            string fileName = CommonHelper.GetUnixTimeNow().ToString();
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(user.wx_appid);
            string commentImgPath = null;//初始值必须默认为Null,否则会分割失败
            if (parameter.imgList != null && parameter.imgList.Count() > 0)
            {
                for (int i = 0; i < parameter.imgList.Length; i++)
                {
                    if (string.IsNullOrEmpty(parameter.imgList[i].Trim()))
                        continue;
                    //如果是修改，则图片参数不可能都是id，有可能是已上传的路径
                    if (comment.imglist != null && comment.imglist.Contains(parameter.imgList[i]))//如果参数中包含已上传过的，则直接累加,不需要重新上传
                    {
                        commentImgPath += parameter.imgList[i].Trim() + "|";
                    }
                    else
                    {
                        //能进来的，应该是新上传的图片
                        var str = MediaApi.Get2(at, parameter.imgList[i].Trim());
                        if (str != null && str.Length > 0)
                        {
                            var path = OssPicPathManager<OssPicBucketConfig>.UploadProductCommentPic(parameter.pid, fileName.ToString() + "_" + i, str);
                            if (!string.IsNullOrEmpty(path))
                                commentImgPath += path + "|";
                        }
                    }
                }
            }
            comment.imglist = commentImgPath?.Trim('|');
            using (var reop = new BizRepository())
            {
                var productcomment = await reop.SaveOrUpdateProductCommentAsync(comment);
                if (productcomment != null)
                {
                    //更新es
                    await EsProductCommentManager.AddOrUpdateAsync(EsProductCommentManager.GenObject(productcomment));
                    //计算出此商品评论的平均分与评论人数
                    var pcommentAgg = await reop.GetByTotalAndAvgScoreAsync(parameter.pid);
                    //更新商品评论平均分及总评论人数:Item1:total,Item2:avgScore
                    if (pcommentAgg != null && pcommentAgg.Item1 > 0 & pcommentAgg.Item2 > 0)
                    {
                        var p = await reop.UpdateProductScoreAsync(productcomment.pid, pcommentAgg.Item2, pcommentAgg.Item1);
                        await EsProductManager.AddOrUpdateAsync(EsProductManager.GenObject(p));
                        return JsonResponseHelper.HttpRMtoJson(new { isOk = true }, HttpStatusCode.OK, ECustomStatus.Success);
                    }
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { isOk = false }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getcommentlist")]
        public async Task<HttpResponseMessage> getcommentlist(ProductParameter parameter)
        {
            if (parameter == null || parameter.pid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid) || parameter.pageIndex <= 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error,pid:{parameter.pid},pageIndex:{parameter.pageIndex}", HttpStatusCode.OK, ECustomStatus.Fail);
            //当前用户信息
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"esuser is null,openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var currentUserID = Guid.Parse(user.Id);
            int pageSize = MdWxSettingUpHelper.GetPageSize();
            var commentList = new List<object>();
            var tuple = await EsProductCommentManager.GetByPidAsync(parameter.pid, parameter.pageIndex, pageSize);
            int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
            using (var repo = new BizRepository())
            {
                using (var codebiz = new CodeRepository())
                {
                    var codeSkinList = await codebiz.GetAllCodeSkinAsync();//肤质字典
                    var writeofferList = await repo.GetWOerByMid2Async(Guid.Parse(user.mid), true);//该商家下的所有核销员
                    foreach (var comment in tuple.Item2)
                    {
                        //评论人信息
                        Guid common_uid = Guid.Parse(comment.uid);
                        var common_user = await EsUserManager.GetByIdAsync(common_uid);
                        if (common_user == null)
                            continue;
                        UserInfoRedis userRedis =
                            await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(common_user.openid);
                        //我是否对此条评论点赞
                        var mycommentpraise = await EsProductCommentPraiseManager.GetByPcidAndUidAsync(Guid.Parse(comment.Id), currentUserID);
                        commentList.Add(new
                        {
                            pcid = comment.Id,
                            comment.comment,
                            comment.score,
                            comment.comment_reply,
                            imglist = string.IsNullOrEmpty(comment.imglist) ? null : comment.imglist.Split('|'),
                            comment.praise_count,//点赞数
                            comment.u_age,//年龄
                            u_skin = codeSkinList.ContainsKey(comment.u_skin) ? codeSkinList[comment.u_skin] : "",//肤质
                            name = userRedis.NickName,//昵称
                            userRedis.HeadImgUrl,//头像
                            isessence = comment.isessence == 1,//是否加精
                            isFriend = await RedisVectorOp.IsExistsQM(currentUserID, common_uid),//是否好友
                            isTarento = writeofferList.Where(p => p.uid.Equals(common_uid)).FirstOrDefault() != null,//是否美妆达人
                            isPraise = mycommentpraise != null,//我是否赞过此评论
                        });
                    }
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, totalCount = tuple.Item1, glist = commentList }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getmycomment")]
        public async Task<HttpResponseMessage> getmyComment(ProductParameter parameter)
        {
            if (parameter == null || parameter.pid.Equals(Guid.Empty) || parameter.openid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,pcid:{parameter.pcid},openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var product = await EsProductManager.GetByPidAsync(parameter.pid);
            if (product == null)
                return JsonResponseHelper.HttpRMtoJson($"es product is null pid:{parameter.pid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"Es user is null,openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var currentUid = Guid.Parse(user.Id);//当前用户的uid
            var myComment = await EsProductCommentManager.GetByPidAndUidAsync(parameter.pid, currentUid);
            object retcomment = null;
            if (myComment != null)
            {
                retcomment = new
                {
                    myComment.score,
                    myComment.comment,
                    imglist = string.IsNullOrEmpty(myComment.imglist) ? null : myComment.imglist.Split('|'),
                };
            }
            var retproduct = new
            {
                pic1 = product.advertise_pic_1,
                product.name,
                origin_price = product.price / 100.00,
                product.standard,
            };
            return JsonResponseHelper.HttpRMtoJson(new { retproduct, retcomment }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("getmycommentlist")]
        public async Task<HttpResponseMessage> getmyCommentList(ProductParameter parameter)
        {
            if (parameter == null || parameter.openid.Equals(Guid.Empty) || parameter.pageIndex <= 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error,openid:{parameter.openid},pageIndex:{parameter.pageIndex}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"Es user is null,openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            int pageSize = MdWxSettingUpHelper.GetPageSize();
            var currentUid = Guid.Parse(user.Id);//当前用户的uid
            var tuple = await EsProductCommentManager.GetByUidAsync(currentUid, parameter.pageIndex, pageSize);
            int totalPage = MdWxSettingUpHelper.GetTotalPages(tuple.Item1);
            List<object> retobj = new List<object>();
            foreach (var comment in tuple.Item2)
            {
                var pid = Guid.Parse(comment.pid);
                var product = await EsProductManager.GetByPidAsync(pid);
                if (product == null) continue;
                retobj.Add(new
                {
                    pid = product.Id,
                    product.advertise_pic_1,
                    product.name,
                    origin_price = product.price / 100.00,
                    product.standard,
                    comment.score,
                });
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, glist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("seedinggrass")]
        public async Task<HttpResponseMessage> seedinggrass(ProductParameter parameter)
        {
            if (parameter == null || parameter.pid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,pcid:{parameter.pcid},openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var product = await EsProductManager.GetByPidAsync(parameter.pid);
            if (product == null)
                return JsonResponseHelper.HttpRMtoJson($"product is null,pid:{parameter.pid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"Es user is null,openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var currentUid = Guid.Parse(user.Id);//当前用户的uid
            //更新种草数
            using (var reop = new BizRepository())
            {
                var p = await reop.UpdateProductGrassAsync(parameter.pid);
                await EsProductManager.AddOrUpdateAsync(EsProductManager.GenObject(p));
                //更新IndexProductGrass(谁中了哪个商品的草)
                await EsProductGrassManager.AddOrUpdateAsync(EsProductGrassManager.GenObject(p.pid, currentUid));
            }
            return JsonResponseHelper.HttpRMtoJson(new { isOk = true }, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpPost]
        [Route("productcommentpraise")]
        public async Task<HttpResponseMessage> productCommentPraise(ProductParameter parameter)
        {
            if (parameter == null || parameter.pcid.Equals(Guid.Empty) || string.IsNullOrEmpty(parameter.openid))
                return JsonResponseHelper.HttpRMtoJson($"parameter error,pcid:{parameter.pcid},openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var product_comment = await EsProductCommentManager.GetByPcidAsync(parameter.pcid);
            if (product_comment == null)
                return JsonResponseHelper.HttpRMtoJson($"EsProductComment is null,pcid:{parameter.pcid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var user = await EsUserManager.GetByOpenIdAsync(parameter.openid);
            if (user == null)
                return JsonResponseHelper.HttpRMtoJson($"Es user is null,openid:{parameter.openid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var currentUid = Guid.Parse(user.Id);//当前用户的uid
            var mycommentpraise = await EsProductCommentPraiseManager.GetByPcidAndUidAsync(parameter.pcid, currentUid);
            if (mycommentpraise != null)
                return JsonResponseHelper.HttpRMtoJson("已赞", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var reop = new BizRepository())
            {
                var pc = await reop.UpdateProductComment_PraiseAsync(parameter.pcid);
                if (pc != null)
                {
                    //更新商品评论总点赞数
                    await EsProductCommentManager.AddOrUpdateAsync(EsProductCommentManager.GenObject(pc));
                    //增加用户评论点赞表（谁赞了哪条评论）
                    await EsProductCommentPraiseManager.AddOrUpdateAsync(EsProductCommentPraiseManager.GenObject(pc.pcid, currentUid));
                }
                return JsonResponseHelper.HttpRMtoJson(new { isOk = true }, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }
        #endregion
    }
}