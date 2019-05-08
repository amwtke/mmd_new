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

namespace Mmd.Backend.Controllers
{
    public class CommunityController : Controller
    {
        // GET: Community
        public ActionResult Index()
        {
            var mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }

        public async Task<PartialViewResult> GetCommunityList(int pageIndex, int pageSize, string query)
        {
            var mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            Guid guidMid = Guid.Parse(mid.ToString());
            var tuple = await EsCommunityManager.GetListAsync(guidMid, Guid.Empty, (int)ECommunityTopicType.MMSQ, pageIndex, pageSize, (int)ECommunityStatus.已发布, query);
            var list = tuple.Item2;
            if (list.Count > 0)
            {
                using (var repo = new BizRepository())
                {
                    return PartialView("Community/_CommunityPartial", new CommunityPartialObject()
                    {
                        PageSize = pageSize,
                        PageIndex = pageIndex,
                        TotalCount = tuple.Item1, 
                        List = list
                    });
                }
            }
            return PartialView("Community/_CommunityPartial", new CommunityPartialObject()
            {
                PageSize = pageSize,
                PageIndex = pageIndex,
                TotalCount = tuple.Item1,
                List = new List<IndexCommunity>()
            });
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

        public async Task<JsonResult> Forbidden(Guid uid,int days)
        {
            if (uid == Guid.Empty || days <= 0)
                return Json(new { Status = "Fail", message = "Parameter Error" });
            var mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return Json(new { Status = "Fail", message = "SessionTimeOut" });
            var timestamp = CommonHelper.ToUnixTime(DateTime.Now.AddDays(days));
            var index = await EsBlacklistManager.GetBlacklistAsync(uid, (int)EBlacklistType.美美社区发帖);
            if (index == null)
            {
                index = new IndexBlacklist()
                {
                    Id = Guid.NewGuid().ToString(),
                    mid = Guid.Parse(mid.ToString()).ToString(),
                    type = (int)EBlacklistType.美美社区发帖,
                    uid = uid.ToString(),
                    opentimestamp = timestamp,
                    createtime = CommonHelper.ToUnixTime(DateTime.Now)
                };
            }
            else
            {
                index.createtime = CommonHelper.ToUnixTime(DateTime.Now);
                index.opentimestamp = timestamp;
            }
            bool res = await EsBlacklistManager.AddOrUpdateAsync(index);
            return Json(new { Status = "Success", message = res });
        }

        public class CommunityPartialObject
        {
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public List<IndexCommunity> List { get; set; }
            public string Q { get; set; }
        }
    }
}