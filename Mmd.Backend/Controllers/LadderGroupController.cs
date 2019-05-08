using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Aliyun;
using MD.Model.DB;
using MD.Model.DB.Activity;
using MD.Model.DB.Code;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Backend.Controllers
{
    public class LadderGroupController : Controller
    {
        // GET: Box
        public async Task<ActionResult> Index()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            string url = MdWxSettingUpHelper.GenLadderGroupListUrl(mer.wx_appid);
            ViewBag.ProUrl = url;
            ViewBag.appid = mer.wx_appid;
            return View();
        }

        public async Task<ActionResult> Add()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            bool canadd = await CanAddGroup();
            if (!canadd)
                return Content("您好，贵公司剩余的拼团订单额不足，无法创建新的拼团活动，请联系工作人员充值，联系电话：18108611928！");
            ViewBag.Type = "add";
            LadderGroup group = new LadderGroup();
            DateTime endTime = Convert.ToDateTime(DateTime.Now.AddDays(5).ToString("yyyy-MM-dd 08:00:00"));
            group.end_time = CommonHelper.ToUnixTime(endTime);
            group.PriceList = new List<LadderPrice>();
            group.PriceList.Add(new LadderPrice());
            group.status = (int)ELadderGroupStatus.待发布;
            return View("Modify", group);
        }

        public async Task<ActionResult> Detail(Guid id)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.Type = "detail";
            using (ActivityRepository repo = new ActivityRepository())
            {
                LadderGroup group = await repo.GetGroupByIdAsync(id);
                if (group != null)
                {
                    group.PriceList = await repo.LadderPriceGetByGidAsync(group.gid);
                    return View("Modify", group);
                }
                else
                {
                    return Content("Parameter Error");
                }
            }
        }

        public async Task<ActionResult> Modify(Guid id)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.Type = "modify";
            using (ActivityRepository repo = new ActivityRepository())
            {
                LadderGroup group = await repo.GetGroupByIdAsync(id);
                if (group != null)
                {
                    group.PriceList = await repo.LadderPriceGetByGidAsync(group.gid);
                    return View("Modify", group);
                }
                else
                {
                    return Content("Parameter Error");
                }
            }
        }

        public async Task<JsonResult> DoModify(LadderGroup group, string type)
        {
            try
            {
                Merchant mer = await SessionHelper.GetMerchant(this);
                if (mer == null)
                    return Json(new { status = "SessionTimeOut", message = "登录超时！" });
                Product p = null;
                long pno = 0;
                if (long.TryParse(group.pno,out pno))
                {
                    using (var rep = new BizRepository())
                    {
                        p = rep.GetProductByPno(group.pno);
                        if (p == null)
                        {
                            return Json(new { status = "PnoError", message = "商品编号错误！" });
                        }
                    }
                }
                else
                {
                    return Json(new { status = "PnoError", message = "请输入商品编号！" });
                }
                if (group.PriceList == null || group.PriceList.Count <= 0)
                {
                    return Json(new { status = "PriceListError", message = "请输入成团人数和价格！" });
                }
                using (ActivityRepository repo = new ActivityRepository())
                {
                    bool IsAdd = false;
                    if (type == "add")
                    {
                        IsAdd = true;
                        group.gid = Guid.NewGuid();
                        group.start_time = 0;
                        group.status = (int)ELadderGroupStatus.待发布;
                        group.product_quotacount = group.product_count;
                    }
                    else
                    {
                        LadderGroup g = await repo.GetGroupByIdAsync(group.gid);
                        if (g != null)
                        {
                            group.status = g.status;
                            if (g.status == (int)ELadderGroupStatus.待发布)
                                group.product_quotacount = group.product_count;
                            else 
                                group.product_quotacount = g.product_quotacount;
                            group.start_time = CommonHelper.GetUnixTimeNow();
                        }
                    }
                    group.origin_price = p.price.Value;
                    group.pid = p.pid;
                    group.mid = mer.mid;
                    group.last_update_time = CommonHelper.ToUnixTime(DateTime.Now);
                    if (group.pic.IndexOf("http://") < 0)
                    {
                        string picPath = GetUploadImgPath(group.pic, group.gid);
                        if (!string.IsNullOrEmpty(picPath))
                        {
                            group.pic = picPath;
                        }
                        else
                        {
                            return Json(new { status = "UploadImgFail", message = $"{group.gid}上传图片失败，请稍后再试！" });
                        }
                    }
                    foreach (LadderPrice item in group.PriceList)
                    {
                        item.lpid = Guid.NewGuid();
                        item.gid = group.gid;
                        //item.group_price = item.group_price * 100;
                    }
                    await repo.GroupAddOrUpdateAsync(group);
                    await repo.LadderPriceAddAsync(group.PriceList);
                }
                return Json(new { status = true, message = "Success" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public async Task<JsonResult> ChangeStatus(Guid id, int status)
        {
            try
            {
                using (ActivityRepository repo = new ActivityRepository())
                {
                    LadderGroup group = await repo.GetGroupByIdAsync(id);
                    if (group != null)
                    {
                        group.status = status;
                        if (status == (int)ELadderGroupStatus.已发布)
                        {
                            group.start_time = CommonHelper.GetUnixTimeNow();
                        }
                        group.last_update_time = CommonHelper.GetUnixTimeNow();
                        group.PriceList = await repo.LadderPriceGetByGidAsync(group.gid);
                        await repo.GroupAddOrUpdateAsync(group);
                        await repo.LadderPriceAddAsync(group.PriceList);
                    }
                }
                return Json(new { status = ECustomStatus.Success, message = "Success" });
            }
            catch (Exception ex)
            {
                return Json(new { status = ECustomStatus.Fail, message = ex });
            }
        }

        public async Task<JsonResult> Addproduct_quota(Guid gid, int product_quota)
        {
            try
            {
                using (ActivityRepository repo = new ActivityRepository())
                {
                    LadderGroup group = await repo.GetGroupByIdAsync(gid);
                    if (group != null)
                    {
                        group.last_update_time = CommonHelper.GetUnixTimeNow();
                        group.PriceList = await repo.LadderPriceGetByGidAsync(group.gid);
                        group.product_count += product_quota;
                        group.product_quotacount += product_quota;
                        await repo.GroupAddOrUpdateAsync(group);
                    }
                }
                return Json(new { status = ECustomStatus.Success, message = "Success" });
            }
            catch (Exception ex)
            {
                return Json(new { status = ECustomStatus.Fail, message = ex });
            }
        }

        private string GetUploadImgPath(string imgBase64, Guid guid)
        {
            try
            {
                var timestamp = CommonHelper.GetUnixTimeNow().ToString();
                int index = imgBase64.IndexOf(",");
                string imgStr = imgBase64.Substring(index + 1);
                byte[] byt = Convert.FromBase64String(imgStr);
                Stream stream = new MemoryStream(byt);
                var path = OssPicPathManager<OssPicBucketConfig>.UploadActivityPic(guid,"ladder", timestamp, stream);
                return path;
            }
            catch (Exception ex)
            {
                MD.Lib.Log.MDLogger.LogErrorAsync(typeof(BoxController), new Exception($"上传图片失败,{guid}:" + ex));
                return "";
            }
        }

        public async Task<PartialViewResult> GroupListPartial(int status, int pageIndex, int pageSize, string q)
        {
            //session
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            int codeStatus = 0;
            if (status == 1)
            {
                codeStatus = (int)ELadderGroupStatus.已发布;
            }
            else if(status == 0)
            {
                codeStatus = (int)ELadderGroupStatus.待发布;
            }
            else
            {
                codeStatus = (int)ELadderGroupStatus.已结束;
            }
            using (ActivityRepository repo = new ActivityRepository())
            {
                Guid mid = mer.mid;
                var tuple = await repo.GetGroupListByMidAsync(mid, codeStatus, pageIndex, pageSize);
                //if (tuple.Item1 != 0 || tuple.Item2.Count != 0)
                //{
                List<LadderGroup> list = tuple.Item2;
                if (status == 1 || status == 2)
                {
                    List<StaLadderGroup> listSta = new List<StaLadderGroup>();
                    foreach (var item in list)
                    {
                        StaLadderGroup sg = CommonHelper.GenFromParent<StaLadderGroup>(item);
                        var tupleSta = await repo.GetGroupStaCount(item.gid);
                        sg.groupCountOpen = tupleSta.Item1;
                        sg.userCountTotal = tupleSta.Item2;
                        sg.orderCountH = tupleSta.Item3;
                        sg.orderAmount = tupleSta.Item4 / 100.00;
                        sg.PriceList = await repo.LadderPriceGetByGidAsync(item.gid);
                        sg.ProUrl = MdWxSettingUpHelper.GenLadderGroupDetailUrl(mer.wx_appid, item.gid);
                        listSta.Add(sg);
                    }
                    return PartialView("LadderGroup/_GroupListPartial",
                        GenParameters(pageIndex, tuple.Item1, pageSize, status, listSta));
                }
                else
                {
                    foreach (var item in list)
                    {
                        item.PriceList = await repo.LadderPriceGetByGidAsync(item.gid);
                    }
                        return PartialView("LadderGroup/_GroupListPartial2",
                        new  PartialParameter2(pageIndex, tuple.Item1, pageSize, status, list));
                }
                //}
                //return PartialView("LadderGroup/_GroupListPartial",
                //    GenParameters(pageIndex, tuple.Item1, pageSize, status, new List<StaLadderGroup>()));
            }
        }
        public async Task<bool> CanAddGroup()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return false;
            using (var repo = new BizRepository())
            {
                var mbiz = await repo.GetMbizAsync(mer.mid, "DD");
                if (mbiz != null && mbiz.quota_remain > 0)
                {
                    return true;
                }
            }
            return false;
        }
        private PartialParameter GenParameters(int pageIndex, int totalCount, int pageSize, int status, List<StaLadderGroup> list)
        {
            return new PartialParameter(pageIndex, totalCount, pageSize, status, list);
        }

        public class PartialParameter2
        {
            public PartialParameter2(int pageIndex, int totalCount, int pageSize, int status,
               List<LadderGroup> list)
            {
                PageIndex = pageIndex;
                TotalCount = totalCount;
                PageSize = pageSize;
                Status = status;
                List = list;
            }
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int Status { get; set; }
            public List<LadderGroup> List { get; set; }
        }

        public class PartialParameter
        {
            public PartialParameter(int pageIndex, int totalCount, int pageSize, int status,
                List<StaLadderGroup> list)
            {
                PageIndex = pageIndex;
                TotalCount = totalCount;
                PageSize = pageSize;
                Status = status;
                List = list;
            }
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int Status { get; set; }
            public List<StaLadderGroup> List { get; set; }
        }

        public class StaLadderGroup : LadderGroup
        {
            public string pname { get; set; }
            public int minGroupPrice { get; set; }
            public int groupCountOpen { get; set; }
            public int userCountTotal { get; set; }
            public int orderCountH { get; set; }
            public double orderAmount { get; set; }
            public string ProUrl { get; set; }
        }
    }
}