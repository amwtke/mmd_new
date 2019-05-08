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

namespace Mmd.Backend.Controllers.Backyard
{
    public class MerchantCheckController : Controller
    {
        // GET: MerchantCheck
        public async Task<ActionResult> Index()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");

            return View();
        }

        public async Task<PartialViewResult> GetList(string q, int pageIndex)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");

            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);

            using (var repo = new BizRepository())
            {
                var tuple = await repo.MerchantSearchByNameAsync(q, pageIndex, pageSize);
                if (tuple.Item1 > 0 && tuple.Item2 != null && tuple.Item2.Count > 0)
                {
                    return PartialView("Backyard/MerCheck/_MerCheckPartial", new MerCheckPartialObject()
                    {
                        PageSize = pageSize,
                        PageIndex = pageIndex,
                        TotalCount = tuple.Item1,
                        List = tuple.Item2,
                        Q = q
                    });
                }
                return PartialView("Backyard/MerCheck/_MerCheckPartial", new MerCheckPartialObject()
                {
                    PageSize = pageSize,
                    PageIndex = pageIndex,
                    TotalCount = tuple.Item1,
                    List = new List<Merchant>(),
                    Q = q
                });
            }
        }
        public async Task<PartialViewResult> Pass(string appid, int pageIndex, string q)
        {
            using (var repo = new BizRepository())
            {
                await repo.ChangeMerchantStatus(appid, ECodeMerchantStatus.已开通未配置);
                return await GetList(q, pageIndex);
            }
        }

        public async Task<PartialViewResult> Reject(string appid, int pageIndex, string q)
        {
            using (var repo = new BizRepository())
            {
                await repo.ChangeMerchantStatus(appid, ECodeMerchantStatus.未通过);
                return await GetList(q, pageIndex);
            }
        }

        public async Task<PartialViewResult> Recovery(string appid, int pageIndex, string q)
        {
            using (var repo = new BizRepository())
            {
                await repo.ChangeMerchantStatus(appid, ECodeMerchantStatus.待审核);
                return await GetList(q, pageIndex);
            }
        }

        public async Task<PartialViewResult> CDD(string appid, int pageIndex, string q, int cddCount)
        {
            using (var repo = new BizRepository())
            {
                var mer = await repo.GetMerchantByAppidAsync(appid);
                if (mer != null && !mer.mid.Equals(Guid.Empty))
                {
                    await MBizRule.BuyTaocan(mer, ECodeTaocanType.CDD, cddCount, 0);
                }
                return await GetList(q, pageIndex);
            }

        }
        public async Task<ActionResult> GetCzLog(Guid mid)
       {
            using (var repo = new BizRepository())
            {

                var log = await repo.GetModerAsync(mid, ECodeTaocanType.CDD.ToString());
                List<object> _ret = new List<object>();

                foreach (var morder in log)
                {
                    _ret.Add(new { timestamp = CommonHelper.FromUnixTime(morder.timestamp.Value).ToString(), buy_tc_shares = morder.buy_tc_shares });
                }
                JsonResult ret = new JsonResult()
                {
                    Data = _ret,
                    ContentType = "application/json",JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                return ret;
            }
        }
        public class MerCheckPartialObject
        {
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public List<Merchant> List { get; set; }

            public string Q { get; set; }
        }
    }
}