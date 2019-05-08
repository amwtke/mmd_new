using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Weixin.Component;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.Index;
using MD.Model.Index.MD;
using Senparc.Weixin.MP.AdvancedAPIs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Backend.Controllers
{
    public class StatisticsController : Controller
    {
        // GET: Statistics
        public async Task<ActionResult> Index()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            //ViewBag.address = mer.service_region;
            ViewBag.seachDate = "";
            return View(mer);
        }

        public async Task<ActionResult> MerView()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View(mer);
        }

        public async Task<ActionResult> UserIncrease()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View(mer);
        }

        public async Task<ActionResult> GroupShare()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View(mer);
        }

        public async Task<JsonResult> GetUserIncrease(Guid mid, DateTime timeStart, DateTime timeEnd)
        {
            //DateTime timeEnd = DateTime.Now;
            //DateTime timeStart = timeEnd.AddMonths(-1);
            //Guid mid = Guid.Parse("d882482f-1975-483b-b075-caa5133763c9");
            List<IndexUser> list = await EsUserManager.GetByMidAsync(mid, timeStart, timeEnd);
            List<object> listRes = new List<object>();
            int days = (timeEnd - timeStart).Days;
            for (int i = 0; i < days; i++)
            {
                DateTime dtStart = timeStart.AddDays(i);
                double start = CommonHelper.ToUnixTime(dtStart);
                double end = CommonHelper.ToUnixTime(dtStart.AddDays(1));
                int count = list.Count(u => u.register_time > start && u.register_time < end);
                listRes.Add(new { KeyAsString = dtStart.ToString("yyyy-MM-dd"), DocCount = count });
            }
            return Json(new { status = "Success", data = listRes });
        }

        public async Task<JsonResult> GetGroupShareData(Guid mid,string gid, DateTime timeStart, DateTime timeEnd)
        {
            //Merchant mer = await SessionHelper.GetMerchant(this);
            //if (mer == null)
            //    return Json(new { status = "SessionNull", message = "登录超时" });
            Guid Gid = string.IsNullOrEmpty(gid) ? Guid.Empty : Guid.Parse(gid);
            var res = await EsBizLogStatistics.GetGroupShareDateHistogram(timeStart, timeEnd, mid, Gid);
            if (res != null && res.Items.Count > 0)
            {
                return Json(new { status = "Success", data = res });
            }
            else
            {
                List<object> listRes = new List<object>();
                int days = (timeEnd - timeStart).Days;
                for (int i = 0; i < days; i++)
                {
                    DateTime dtStart = timeStart.AddDays(i);
                    listRes.Add(new { KeyAsString = dtStart.ToString("yyyy-MM-dd"), DocCount = 0 });
                }
                object result = new { Items = listRes };
                return Json(new { status = "Success", data = result });
            }
        }

        public async Task<JsonResult> GetSubUserIncrease(string appid, string timeStart,string timeEnd)
        {
            //string appid = "wxfdb8751afdedfea0";
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
            var res = AnalysisApi.GetUserSummary(at, timeStart, timeEnd);
            return Json(new { data = res});
        }

        public async Task<JsonResult> GetSubUserCumulate(string appid, string timeStart, string timeEnd)
        {
            //string appid = "wxfdb8751afdedfea0";
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
            var res = AnalysisApi.GetUserCumulate(at, timeStart, timeEnd);
            return Json(new { data = res });
        }

        public async Task<JsonResult> GetSubUserCumulate2(string appid,DateTime timeStart,DateTime timeEnd)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            int days = (timeEnd - timeStart).Days;
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
            int length = (int)Math.Ceiling(days / 1.0 / 6);
            try
            {
                for (int i = 0; i < length; i = i + 6)
                {
                    DateTime dtStart = timeStart.AddDays(i);
                    DateTime dtEnd = dtStart.AddDays(5);
                    var res = AnalysisApi.GetUserCumulate(at, dtStart.ToString("yyyy-MM-dd"), timeEnd.ToString("yyyy-MM-dd"));
                    dic.Add(dtStart.ToString("yyyy-MM-dd"), res );
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "Success", data = dic,message=ex });
            }
            return Json(new { status = "Success", data = dic });
        }

        public async Task<JsonResult> GetSubUserCumulateNow()
        {
            //string appid = "wxfdb8751afdedfea0";
            
            string timeStart = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            string timeEnd = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            using (var repo = new BizRepository())
            {
                var tuple = await repo.MerchantSearchByNameAsync("", 1, 120, (int)ECodeMerchantStatus.已配置, "");
                var list = tuple.Item2;
                List<object> listRes = new List<object>();
                foreach (var mer in list)
                {
                    try
                    {
                        string appid = mer.wx_appid;
                        string at = await WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppIdAsync(appid);
                        var res = AnalysisApi.GetUserCumulate(at, timeStart, timeEnd);
                        listRes.Add(new { name = mer.name, res = res.list[0].cumulate_user });
                    }
                    catch (Exception ex)
                    {
                        listRes.Add(new { name = mer.name, res = ex.Message });
                        continue;
                    }
                    Thread.Sleep(10);
                }
                return Json(new { data = listRes });
            }     
        }

        public async Task<JsonResult> GetUserData(Guid mid,string gid, DateTime timeStart, DateTime timeEnd)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionNull", message="登录超时"});
            double to = CommonHelper.ToUnixTime(timeEnd);
            double f = CommonHelper.ToUnixTime(timeStart);
            Guid Gid = string.IsNullOrEmpty(gid) ? Guid.Empty : Guid.Parse(gid);
            var tuple = await EsBizLogStatistics.SearchBizViewAsnyc(ELogBizModuleType.PayView, Gid, Guid.Empty, mid, f, to, 1, 100000);
            List<BizIndex> list = tuple.Item2;
            List<object> listObj = new List<object>();
            foreach (var item in list)
            {
                if (item.Location != null && item.Location.Lon != 0)
                {
                    double[] coor = { item.Location.Lon, item.Location.Lat };
                    geometry obj = new geometry { type = "Point", coordinates = coor };
                    listObj.Add(new { geometry = obj });
                }
            }
            return Json(listObj);
        }
        public async Task<JsonResult> GetDateHistogram(Guid mid, DateTime timeStart, DateTime timeEnd)
        {
            //DateTime timeEnd = DateTime.Now;
            //DateTime timeStart = timeEnd.AddMonths(-1);
            //Guid mid = Guid.Parse("d882482f-1975-483b-b075-caa5133763c9");
            var res = await EsBizLogStatistics.GetDateHistogram(timeStart, timeEnd, mid , ELogBizModuleType.MidView.ToString());
            if (res != null && res.Items.Count > 0 )
            {
                return Json(new { status = "Success", data = res });
            }
            else
            {
                List<object> listRes = new List<object>();
                int days = (timeEnd - timeStart).Days;
                for (int i = 0; i < days; i++)
                {
                    DateTime dtStart = timeStart.AddDays(i);
                    listRes.Add(new { KeyAsString = dtStart.ToString("yyyy-MM-dd"), DocCount = 0 });
                }
                object result = new { Items = listRes };
                return Json(new { status = "Success", data = result });
            }
        }
        public async Task<JsonResult> GetWriteOffData(Guid mid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionNull", message = "登录超时" });
            //mer.mid = Guid.Parse("c5bef86f-fec9-4ce9-a0dc-a3e7ee46f3ca");
            var tup = await EsWriteOffPointManager.SearchAsnyc("", mid);
            List<IndexWriteOffPoint> listWriteOff = tup.Item2;
            List<object> listObj = new List<object>();
            foreach (var item in listWriteOff)
            {
                if (item.longitude != 0 && item.latitude != 0)
                {
                    listObj.Add(new { id = item.Id, name = item.name, lat = item.latitude, lng = item.longitude });
                }
            }
            return Json(new {status = "Success",data= listObj } );
        }

        public async Task<JsonResult> GetGroupByTime(Guid mid, DateTime timeStart, DateTime timeEnd)
        {
            double start = CommonHelper.ToUnixTime(timeStart);
            double end = CommonHelper.ToUnixTime(timeEnd);
            List<int> listStatus = new List<int> { (int)EGroupStatus.已发布, (int)EGroupStatus.已结束, (int)EGroupStatus.已过期 };
            var tup = await EsGroupManager.getGroupsAsync(new List<Guid> { mid}, null, 1,50,start,end);
            List<IndexGroup> listGroup = tup.Item2;
            List<object> listObj = new List<object>();
            foreach (var item in listGroup)
            {
                listObj.Add(new { id = item.Id, name = item.title });
            }
            return Json(new { status = "Success", data = listObj });
        }

        public class geometry
        {
            public string type { get; set; }
            public double[] coordinates { get; set; }
        }
    }
}