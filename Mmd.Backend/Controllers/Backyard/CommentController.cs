using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.UI;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Lib.MMBizRule.MerchantRule;
using System.Net.Http;
using MD.Lib.Util;
using System.Net;
using MD.Model.DB.Professional;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Weixin.Robot;
using MD.Model.Index.MD;

namespace Mmd.Backend.Controllers.Backyard
{
    public class CommentController : Controller
    {
        // GET: Comment
        public ActionResult Index()
        {
            var mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }

        public ActionResult Community()
        {
            var mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            //Merchant mer = await SessionHelper.GetMerchant(this);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }

        public async Task<PartialViewResult> GetCommunityList(int pageIndex,int pageSize,string query)
        {
            var tuple = await EsCommunityManager.GetListAsync(Guid.Empty, Guid.Empty, (int)ECommunityTopicType.MMSQ, pageIndex, pageSize, (int)ECommunityStatus.已发布, query);
            var list = tuple.Item2;
            if (list.Count > 0)
            {
                using (var repo = new BizRepository())
                {
                    var tupleMerchant = await repo.MerchantSearchByNameAsync("", 1, 1000, (int)ECodeMerchantStatus.已配置, "");
                    return PartialView("Backyard/Community/_CommunityPartial", new CommunityPartialObject()
                    {
                        PageSize = pageSize,
                        PageIndex = pageIndex,
                        TotalCount = tuple.Item1,
                        List = list,
                        ListMerchant = tupleMerchant.Item2
                    });
                }
            }
            return PartialView("Backyard/Community/_CommunityPartial", new CommunityPartialObject()
            {
                PageSize = pageSize,
                PageIndex = pageIndex,
                TotalCount = tuple.Item1,
                ListMerchant = new List<Merchant>(),
                List = new List<IndexCommunity>()
            });
        }

        public async Task<PartialViewResult> GetList(string mer,string pro, int pageIndex)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            int pageSize = 20;

            using (var repo = new BizRepository())
            {
                #region MyRegion
                //if (!string.IsNullOrEmpty(pro))
                //{
                //    var tupleMerchant = await repo.MerchantSearchByNameAsync(mer, 1, 1000, (int)ECodeMerchantStatus.已配置, "");
                //    if (tupleMerchant.Item1 > 0 && tupleMerchant.Item2 != null)
                //    {
                //        var listMerchant = tupleMerchant.Item2;
                //        List<Product> listProduct = new List<Product>();
                //        foreach (var item in listMerchant)
                //        {
                //            var tupleProduct = await repo.GetProductsByAppidAsync(item.wx_appid, pageIndex, pageSize);
                //            var list = tupleProduct.Item2;
                //            if (list != null && list.Count > 0)
                //            {
                //                listProduct.Concat(list);
                //            }
                //        }
                //        return PartialView("Backyard/Comment/_ProductPartial", new ProductPartialObject()
                //        {
                //            PageSize = pageSize,
                //            PageIndex = pageIndex,
                //            TotalCount = tuple.Item1,
                //            List = tuple.Item2,
                //            ListMerchant = tupleMerchant.Item2
                //        });
                //    }
                //} 
                #endregion
                var tuple = await repo.GetProductsAllAsync(pro,pageIndex, pageSize);
                //var tupleMerchant = await repo.MerchantSearchByNameAsync("", 1, 1000, (int)ECodeMerchantStatus.已配置, "");
                var tupleMerchant = repo.MerchantSearchByName("", 1, 1000);
                if (tuple.Item1 > 0 && tuple.Item2 != null && tuple.Item2.Count > 0)
                {
                    return PartialView("Backyard/Comment/_ProductPartial", new ProductPartialObject()
                    {
                        PageSize = pageSize,
                        PageIndex = pageIndex,
                        TotalCount = tuple.Item1,
                        List = tuple.Item2,
                        ListMerchant = tupleMerchant.Item2
                    });
                }
                return PartialView("Backyard/Comment/_ProductPartial", new ProductPartialObject()
                {
                    PageSize = pageSize,
                    PageIndex = pageIndex,
                    TotalCount = tuple.Item1,
                    ListMerchant = new List<Merchant>(),
                    List = new List<Product>()
                });
            }
        }

        public async Task<JsonResult> AddComment(Guid pid,Guid mid,string commentContent)
        {
            int age = CommonHelper.GetRandomNumber(18,40);
            int skin = CommonHelper.GetRandomNumber(1,6);
            int score = CommonHelper.GetRandomNumber(8, 11);
            Guid uid = await GetRandomOpenid(pid);
            if (uid!= Guid.Empty)
            {
                ProductComment comment = new ProductComment();
                comment.pcid = Guid.NewGuid();
                comment.pid = pid;
                comment.uid = uid;
                comment.mid = mid;
                comment.u_age = age;
                comment.u_skin = skin;
                comment.score = score;
                comment.comment = commentContent;
                comment.imglist = "";
                using (var reop = new BizRepository())
                {
                    var productcomment = await reop.SaveOrUpdateProductCommentAsync(comment);
                    if (productcomment != null)
                    {
                        //更新es
                        await EsProductCommentManager.AddOrUpdateAsync(EsProductCommentManager.GenObject(productcomment));
                        //计算出此商品评论的平均分与评论人数
                        var pcommentAgg = await reop.GetByTotalAndAvgScoreAsync(pid);
                        //更新商品评论平均分及总评论人数:Item1:total,Item2:avgScore
                        if (pcommentAgg != null && pcommentAgg.Item1 > 0 & pcommentAgg.Item2 > 0)
                        {
                            var p = await reop.UpdateProductScoreAsync(productcomment.pid, pcommentAgg.Item2, pcommentAgg.Item1);
                            await EsProductManager.AddOrUpdateAsync(EsProductManager.GenObject(p));
                        }
                    }
                }
                return Json(new { Status = "Success" });
            }
            return Json(new { Status = "Fail" ,message = "评论次数已用完，请添加机器人"});
        }

        public async Task<JsonResult> DelCommunity(Guid cid)
        {
            if (cid == Guid.Empty)
                return Json(new { Status = "Fail", message = "Parameter Error" });
            using (var repo = new BizRepository())
            {
                var flag = false;
                var dbcommunity = await repo.DelCommunityAsync(cid, (int)ECommunityStatus.已删除);
                if (dbcommunity != null)
                {
                    var tempindex = EsCommunityManager.GenObject(dbcommunity);
                    flag = await EsCommunityManager.AddOrUpdateAsync(tempindex);
                    if (flag)
                    {
                        await EsCommunityBizManager.DelBizsAsync(Guid.Empty, Guid.Empty, dbcommunity.cid, (int)EComBizType.Favour);//删除点赞日志
                    }
                }
            }
            return Json(new { Status = "Success" });
        }

        private async Task<Guid> GetRandomOpenid(Guid pid)
        {
            List<string> robotList = RobotHelper.getRobotsOpenids(30);
            foreach (string item in robotList)
            {
                var user = await EsUserManager.GetByOpenIdAsync(item);
                var myComment = await EsProductCommentManager.GetByPidAndUidAsync(pid, Guid.Parse(user.Id));
                if (myComment == null)
                {
                    return Guid.Parse(user.Id);
                }
            }
            return Guid.Empty;
        }

        public class ProductPartialObject
        {
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public List<Product> List { get; set; }
            public List<Merchant> ListMerchant { get; set; }
            public string Q { get; set; }
        }

        public class CommunityPartialObject
        {
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public List<IndexCommunity> List { get; set; }
            public List<Merchant> ListMerchant { get; set; }
            public string Q { get; set; }
        }
    }
}