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
    public class BoxController : Controller
    {
        // GET: Box
        public async Task<ActionResult> Index()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            string url = MdWxSettingUpHelper.GenFindBoxUrl(mer.wx_appid);
            ViewBag.ProUrl = url;
            return View();
        }
        
        public async Task<ActionResult> Add()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.Type = "add";
            Box box = new Box();
            DateTime start = Convert.ToDateTime(DateTime.Now.AddDays(1).ToString("yyyy-MM-dd 08:00:00"));
            box.time_start = CommonHelper.ToUnixTime(start);
            box.time_end = CommonHelper.ToUnixTime(start.AddDays(3));
            box.BoxTreasureList = new List<BoxTreasure>();
            box.BoxTreasureList.Add(new BoxTreasure());
            return View("Modify", box);
        }

        public async Task<ActionResult> Detail(Guid id)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.Type = "detail";
            using (ActivityRepository repo = new ActivityRepository())
            {
                Box box = await repo.GetBoxByIdAsync(id);
                if (box != null)
                {
                    box.BoxTreasureList = await repo.GetBoxTreasureByBidAsync(id);
                    return View("Modify", box);
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
                Box box = await repo.GetBoxByIdAsync(id);
                if (box != null)
                {
                    box.BoxTreasureList = await repo.GetBoxTreasureByBidAsync(id);
                    return View(box);
                }
                else
                {
                    return Content("Parameter Error");
                }
            }
        }

        public async Task<JsonResult> ModifyBox(Box box, string type)
        {
            try
            {
                Merchant mer = await SessionHelper.GetMerchant(this);
                if (mer == null)
                    return Json(new { status = "SessionTimeOut", message = "登录超时！" });
                using (ActivityRepository repo = new ActivityRepository())
                {
                    bool IsAdd = false;
                    if (type == "add")
                    {
                        IsAdd = true;
                        box.bid = Guid.NewGuid();
                        box.status = (int) EBoxStatus.待发布;
                    }
                    else
                    {
                        Box b = await repo.GetBoxByIdAsync(box.bid);
                        if (b != null && b.status == (int)EBoxStatus.已上线)
                        {
                            return Json(new { status = "CanNotModified", message = "活动已上线，不能编辑！" });
                        }
                    }
                    box.mid = mer.mid;
                    box.appid = mer.wx_appid;
                    box.last_update_time = CommonHelper.ToUnixTime(DateTime.Now);
                    await repo.BoxAddOrUpdateAsync(box);
                    foreach (BoxTreasure item in box.BoxTreasureList)
                    {
                        if (IsAdd || item.btid == Guid.Empty)
                        {
                            item.btid = Guid.NewGuid();
                        }
                        if (item.pic.IndexOf("http://") < 0)
                        {
                            string picPath = GetUploadImgPath(item.pic, item.btid);
                            if (!string.IsNullOrEmpty(picPath))
                            {
                                item.pic = picPath;
                            }
                            else
                            {
                                return Json(new { status = "UploadImgFail", message = $"{item.btid}上传图片失败，请稍后再试！" });
                            }
                        }
                        item.quota_count = item.count;
                        item.bid = box.bid;
                        await repo.BoxTreasureAddOrUpdateAsync(item);
                    }
                }
                return Json(new { status = true, message = "Success"});
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex });
            }
        }

        public async Task<JsonResult> ChangeStatus(Guid id,int status)
        {
            try
            {
                using (ActivityRepository repo = new ActivityRepository())
                {
                    Box box = await repo.GetBoxByIdAsync(id);
                    if (box != null)
                    {
                        box.status = status;
                        bool res = await repo.UpdateBoxAsync(box);
                    }
                }
                return Json(new { status = ECustomStatus.Success, message = "Success" });
            }
            catch (Exception ex)
            {
                return Json(new { status = ECustomStatus.Fail, message = ex });
            }
        }

        private string GetUploadImgPath(string imgBase64,Guid guid)
        {
            try
            {
                var timestamp = CommonHelper.GetUnixTimeNow().ToString();
                int index = imgBase64.IndexOf(",");
                string imgStr = imgBase64.Substring(index + 1);
                byte[] byt = Convert.FromBase64String(imgStr);
                Stream stream = new MemoryStream(byt);
                var path = OssPicPathManager<OssPicBucketConfig>.UploadActivityPic(guid,"box", timestamp, stream);
                return path;
            }
            catch (Exception ex)
            {
                MD.Lib.Log.MDLogger.LogErrorAsync(typeof(BoxController),new Exception($"上传图片失败,{guid}:" + ex) );
                return "";
            }

        }

        public JsonResult PostImg(HttpPostedFileBase img)
        {
            try
            {
                var timestamp = CommonHelper.GetUnixTimeNow().ToString();
                var path = OssPicPathManager<OssPicBucketConfig>.UploadActivityPic(Guid.NewGuid(), timestamp, img.InputStream);
                return Json(new { status = "Success", path = path });
            }
            catch (Exception ex)
            {
                return Json(new { status = "Fail", message = ex });
            }
        }

        public async Task<PartialViewResult> BoxListPartial(int status, int pageIndex, int pageSize, string q)
        {
            //session
            object objMid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (objMid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            using (ActivityRepository repo = new ActivityRepository())
            {
                Guid mid = Guid.Parse(objMid.ToString());
                var tuple = await repo.GetBoxListByMidAsync(mid, status, pageIndex, pageSize);
                if (tuple.Item1 != 0 || tuple.Item2.Count != 0)
                {
                    List<Box> list = tuple.Item2;
                    List<StaBox> listSta = new List<StaBox>();
                    foreach (var item in list)
                    {
                        var tupleSta = await repo.GetStaCountByMidAsync(item.bid);
                        StaBox sb = new StaBox()
                        {
                            bid = item.bid,
                            time_start = item.time_start,
                            time_end = item.time_end,
                            status = item.status,
                            TreasureTotalCount = tupleSta.Item1,
                            TreasureOpenCount = tupleSta.Item2,
                            TreasureCheckCount = tupleSta.Item3
                        };
                        listSta.Add(sb);
                    }
                    return PartialView("Box/_BoxListPartial",
                        GenParameters(pageIndex, tuple.Item1, pageSize, status, listSta));
                }
                return PartialView("Box/_BoxListPartial",
                    GenParameters(pageIndex, tuple.Item1, pageSize, status, new List<StaBox>()));
            }
        }

        private PartialParameter GenParameters(int pageIndex, int totalCount, int pageSize, int status, List<StaBox> list)
        {
            return new PartialParameter(pageIndex, totalCount, pageSize, status, list);
        }

        public class PartialParameter
        {
            public PartialParameter(int pageIndex, int totalCount, int pageSize, int status,
                List<StaBox> list)
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
            public List<StaBox> List { get; set; }
        }

        public class StaBox : Box
        {
            public int TreasureTotalCount { get; set; }
            public int TreasureOpenCount { get; set; }
            public int TreasureCheckCount { get; set; }
        }

        public async Task<ActionResult> SignIndex()
        {
            return View();
        }

        public async Task<JsonResult> AddSign(HttpPostedFileBase pic)
        {
            //var path = OssPicPathManager<OssPicBucketConfig>.UploadGroupAdvertisPic(group.gid, timestamp.ToString(), picg.InputStream);
            
            //HttpPostedFileBase pic = HttpContext.Request.Files["pic"];
            pic.SaveAs(Server.MapPath("~/imgs/" + Guid.NewGuid() + ".jpg"));
            System.IO.Stream stream = pic.InputStream;
            return Json(new { status = "OK", length=stream.Length});
        }
    }
}