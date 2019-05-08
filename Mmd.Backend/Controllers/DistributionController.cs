using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.Data;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Backend.Controllers
{
    public class DistributionController : Controller
    {
        // GET: Distribution
        public ActionResult Index()
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }

        public ActionResult Statistics()
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.mid = Guid.Parse(mid.ToString());
            return View();
        }

        public async Task<PartialViewResult> WoerGetPartial(int status, int pageIndex, string q)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            using (var repo = new BizRepository())
            {
                Guid merid = Guid.Parse(mid.ToString());
                var tuple = await repo.GetWOercomByMidAsync(merid, pageIndex, 20);
                List<WriteOfferView> list = tuple.Item2;
                DistributionPatialObject obj = new DistributionPatialObject();
                obj.PageIndex = pageIndex;
                obj.PageSize = 20;
                obj.TotalCount = tuple.Item1;
                obj.List = list;
                return PartialView("Distribution/DistributionPartial", obj);
            }
        }

        public async Task<PartialViewResult> StatisticsPartial(Guid gid)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            using (var repo = new BizRepository())
            {
                Guid merid = Guid.Parse(mid.ToString());
                var dic = await repo.GetDistributionByGidAsync(gid, (int)EDisSourcetype.订单佣金);
                var tuple = await repo.GetWOercomByMidAsync(merid, 1, 300);
                var listWoer = tuple.Item2;
                List<DistributeView> list = new List<DistributeView>();
                Dictionary<string, int> dicShareCount = await EsBizLogStatistics.GetUserIndexShareCount(gid);
                foreach (var item in listWoer)
                {
                    DistributeView obj = new DistributeView
                    {
                        uid = item.uid,
                        nickName = item.nickName,
                        realname = item.realname,
                        woname = item.woname,
                        DistributeCount = dic.ContainsKey(item.uid) ? dic[item.uid] : 0,
                        ShareCount = dicShareCount.ContainsKey(item.uid.ToString()) ? dicShareCount[item.uid.ToString()] : 0
                    };
                    list.Add(obj);
                }
                //list.Sort((a, b) => Convert.ToInt16(b.ShareCount - a.ShareCount));
                list = list.OrderByDescending(t => t.ShareCount).OrderBy(t=>t.woid).ToList();
                return PartialView("Distribution/StatisticsPartial", list);
            }
        }

        public async Task<JsonResult> StatisticsJson(List<Guid> gids)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return Json(new { status = "SessionTimeOut" });
            if (gids == null || gids.Count <= 0)
                return Json(new { status = "ParameterError" });
            Dictionary<string, GroupStaticsCount> dic = new Dictionary<string, GroupStaticsCount>();
            List<object> listGroup = new List<object>();
            using (var repo = new BizRepository())
            {
                Guid merid = Guid.Parse(mid.ToString());
                var listWoer = await repo.GetWOerviewByMid2Async(merid);
                foreach (Guid gid in gids)
                {
                    Dictionary<Guid,int> dicOrderCount = await repo.GetDistributionByGidAsync(gid, (int)EDisSourcetype.订单佣金);
                    Dictionary<string, int> dicShareCount = await EsBizLogStatistics.GetUserIndexShareCount(gid);
                    var group = await EsGroupManager.GetByGidAsync(gid);
                    listGroup.Add(new GroupStaticsCount()
                    {
                        gid = gid,
                        name = group.title,
                        dicShare = dicShareCount,
                        dicOrder = dicOrderCount.ToDictionary(o => o.Key.ToString(), o => o.Value)
                    });
                }
                object obj = new { listWoer = listWoer, listGroup = listGroup };
                return Json(new { status = "success", data = obj});
            }
        }

        private class GroupStaticsCount
        {
            public Guid gid { get; set; }
            public string name { get; set; }
            public Dictionary<string,int> dicShare { get; set; }
            public Dictionary<string,int> dicOrder { get; set; }
        }

        public async Task<JsonResult> GetCommissionList(Guid uid, int type)
        {
            using (var repo = new BizRepository())
            {
                var tuple = await repo.GetDistributionByUidAsync(uid, 1, 50, type);
                var list = tuple.Item2;
                List<object> retobj = new List<object>();
                if (list != null && list.Count > 0)
                {
                    foreach (var dis in list)
                    {
                        if (dis.sourcetype == (int)EDisSourcetype.订单佣金)
                        {
                            var order = await EsOrderManager.GetByIdAsync(dis.oid);
                            var group = await EsGroupManager.GetByGidAsync(dis.gid);
                            if (order == null || group == null)
                                continue;
                            retobj.Add(new
                            {
                                title = group.title,
                                order.o_no,
                                getcommissiontime = CommonHelper.FromUnixTime(dis.lastupdatetime).ToString("yyyy/MM/dd HH:mm"),
                                commission = dis.commission / 100.00,
                                sourcetypeName = ((EDisSourcetype)dis.sourcetype).ToString(),
                                sourcetype = dis.sourcetype,
                                finalcommission = dis.finally_commission / 100.00
                            });
                        }
                        else if (dis.sourcetype == (int)EDisSourcetype.佣金结算)
                        {
                            retobj.Add(new
                            {
                                title = EDisSourcetype.佣金结算.ToString(),
                                o_no = "",
                                getcommissiontime = CommonHelper.FromUnixTime(dis.lastupdatetime).ToString("yyyy/MM/dd HH:mm"),
                                commission = dis.commission / 100.00,
                                sourcetypeName = ((EDisSourcetype)dis.sourcetype).ToString(),
                                sourcetype = dis.sourcetype,
                                finalcommission = dis.finally_commission / 100.00
                            });
                        }
                    }
                }
                return Json(new { data = retobj });
            }
        }

        public async Task<ActionResult> Export()
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            using (var repo = new BizRepository())
            {
                Guid merid = Guid.Parse(mid.ToString());
                var tuple = await repo.GetWOercomByMidAsync(merid, 1, 300);
                List<WriteOfferView> list = tuple.Item2;
                if (list != null && list.Count > 0)
                {
                    DataTable dt = new DataTable("佣金结算");
                    dt.Columns.Add("核销员");
                    dt.Columns.Add("门店");
                    dt.Columns.Add("姓名");
                    dt.Columns.Add("电话");
                    dt.Columns.Add("佣金余额");
                    foreach (var o in list)
                    {
                        List<object> _temp = new List<object>();
                        _temp.Add(o.nickName);
                        _temp.Add(o.woname);
                        _temp.Add(o.realname);
                        _temp.Add(o.phone);
                        _temp.Add(o.commission / 100.00);
                        dt.Rows.Add(_temp.ToArray());
                    }
                    var csv = new CSVHelper(dt);
                    string csvString = csv.ExportCSV();
                    if (!string.IsNullOrEmpty(csvString))
                    {
                        byte[] bs = Encoding.GetEncoding("gb2312").GetBytes(csvString);
                        Stream st = new MemoryStream(bs);
                        return File(st, "text/csv", "佣金结算" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv");
                    }
                }
                return Content("暂无数据");
            }
        }

        [HttpPost]
        public ActionResult StatisticsExport(string dataString)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            if (!string.IsNullOrEmpty(dataString))
            {
                string data = "<table width=\"100%\" border=\"1\" cellspacing=\"1\" cellpadding=\"0\">";
                data += Server.UrlDecode(dataString);
                data += "</table>";
                Response.AppendHeader("Content-Disposition", "attachment;filename=\"" + "推广统计" + DateTime.Now.ToString("yyyy-MM-dd") + ".xls" + "\"");
                return Content(data, "Application/ms-excel", Encoding.UTF8);
            }
            return Content("暂无数据");
        }

        /// <summary>
        /// 结算佣金
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task<JsonResult> Settle(Guid uid, int amount)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            try
            {
                using (var repo = new BizRepository())
                {
                    int userCommission = repo.GetWriteOfferCommission(uid);
                    if (userCommission >= 0)
                    {
                        if (userCommission >= amount)
                        {
                            var resWriteOffer = repo.addWriteOfferCommission(uid, mer.mid, -amount);
                            if (resWriteOffer != null)
                            {
                                Distribution dis = new Distribution();
                                dis.last_commission = userCommission;
                                dis.commission = amount;
                                dis.finally_commission = userCommission - amount;
                                dis.mid = mer.mid;
                                dis.oid = Guid.NewGuid();
                                dis.gid = Guid.Empty;
                                dis.buyer = Guid.Empty;
                                dis.sharer = uid;
                                dis.isptsucceed = 1;
                                dis.lastupdatetime = CommonHelper.GetUnixTimeNow();
                                dis.sourcetype = (int)EDisSourcetype.佣金结算;
                                bool res = await repo.AddDistributionAsync(dis);
                                return Json(new { status = "success", message = res });
                            }
                            else
                                return Json(new { status = "fail", message = "UpdateFail" });
                        }
                        else
                            return Json(new { status = "fail", message = "CommissionNotEnough" });
                    }
                    else
                        return Json(new { status = "fail", message = "UidNotExist" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "fail", message = ex });
            }
        }

        public class DistributionPatialObject
        {
            public string qdate { get; set; }
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int? Status { get; set; }
            public List<WriteOfferView> List { get; set; }
        }

        public class DistributeView : WriteOfferView
        {
            public long ShareCount { get; set; }
            public int DistributeCount { get; set; }
        }
    }
}