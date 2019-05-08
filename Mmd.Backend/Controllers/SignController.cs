using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Aliyun;
using MD.Model.DB;
using MD.Model.DB.Activity;
using MD.Model.DB.Code;
using System.IO;
using System.Web.Mvc;
namespace Mmd.Backend.Controllers
{
    public class SignController : Controller
    {
        // GET: Sign
        public async Task<ActionResult> Index()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            string url = MdWxSettingUpHelper.GenSignUrl(mer.wx_appid);
            ViewBag.ProUrl = url;
            return View();
        }

        public async Task<ActionResult> Add()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.Type = "add";
            Sign sign = new Sign();
            DateTime start = Convert.ToDateTime(DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"));
            sign.timeStart = CommonHelper.ToUnixTime(start);
            sign.timeEnd = CommonHelper.ToUnixTime(start.AddDays(3));
            return View("Modify", sign);
        }

        public async Task<ActionResult> Detail(Guid id)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.Type = "detail";
            using (ActivityRepository repo = new ActivityRepository())
            {
                Sign s = await repo.GetSignByIdAsync(id);
                if (s != null)
                {
                    return View("Modify", s);
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
                Sign s = await repo.GetSignByIdAsync(id);
                if (s != null)
                {
                    return View(s);
                }
                else
                {
                    return Content("Parameter Error");
                }
            }
        }

        public async Task<JsonResult> DoModify(Sign sign, string type)
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
                        sign.sid = Guid.NewGuid();
                        sign.status = (int)ESignStatus.待发布;
                    }
                    else
                    {
                        Sign s = await repo.GetSignByIdAsync(sign.sid);
                        if (s != null && s.status == (int)ESignStatus.已上线)
                        {
                            return Json(new { status = "CanNotModified", message = "活动已上线，不能编辑！" });
                        }
                    }
                    sign.mid = mer.mid;
                    sign.appid = mer.wx_appid;
                    sign.awardQuatoCount = sign.awardCount;
                    sign.last_update_time = CommonHelper.ToUnixTime(DateTime.Now);
                    if (sign.awardPic.IndexOf("http://") < 0)
                    {
                        string picPath = GetUploadImgPath(sign.awardPic, sign.sid);
                        if (!string.IsNullOrEmpty(picPath))
                        {
                            sign.awardPic = picPath;
                        }
                        else
                        {
                            return Json(new { status = "UploadImgFail", message = $"{sign.sid}上传图片失败，请稍后再试！" });
                        }
                    }
                    
                    await repo.SignAddOrUpdateAsync(sign);
                }
                return Json(new { status = true, message = "Success",data=sign });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex });
            }
        }

        public async Task<JsonResult> ChangeStatus(Guid id, int status)
        {
            try
            {
                using (ActivityRepository repo = new ActivityRepository())
                {
                    Sign sign = await repo.GetSignByIdAsync(id);
                    if (sign != null)
                    {
                        sign.status = status;
                        bool res = await repo.UpdateSignAsync(sign);
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
                var path = OssPicPathManager<OssPicBucketConfig>.UploadActivityPic(guid,"sign", timestamp, stream);
                return path;
            }
            catch (Exception ex)
            {
                MD.Lib.Log.MDLogger.LogErrorAsync(typeof(BoxController), new Exception($"上传图片失败,{guid}:" + ex));
                return "";
            }

        }
        public async Task<PartialViewResult> ListPartial(int status, int pageIndex, int pageSize, string q)
        {
            //session
            object objMid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (objMid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            using (ActivityRepository repo = new ActivityRepository())
            {
                Guid mid = Guid.Parse(objMid.ToString());
                var tuple = await repo.GetSignListByMidAsync(mid, pageIndex, pageSize);
                if (tuple.Item1 > 0 || tuple.Item2.Count > 0)
                {
                    List<Sign> list = tuple.Item2;
                    List<StaSign> listSta = new List<StaSign>();
                    foreach (var item in list)
                    {
                        var staTuple = await repo.GetSignCountByMidAsync(item.sid);
                        StaSign sta = new StaSign();
                        sta.sid = item.sid;
                        sta.timeStartStr = CommonHelper.FromUnixTime(item.timeStart).ToString("yyyy.MM.dd");
                        sta.timeEndStr = CommonHelper.FromUnixTime(item.timeEnd).ToString("yyyy.MM.dd");
                        sta.awardName = item.awardName;
                        sta.awardCount = item.awardCount;
                        sta.UserSignCount = staTuple.Item1;
                        sta.UserCheckCount = staTuple.Item2;
                        sta.status = item.status;
                        listSta.Add(sta);
                    }
                    return PartialView("Sign/_SignListPartial", GenParameters(pageIndex, tuple.Item1, pageSize, status, listSta));
                }
                return PartialView("Sign/_SignListPartial",
                    GenParameters(pageIndex, tuple.Item1, pageSize, status, new List<StaSign>()));
            }
        }

        private PartialParameter GenParameters(int pageIndex, int totalCount, int pageSize, int status, List<StaSign> list)
        {
            return new PartialParameter(pageIndex, totalCount, pageSize, status, list);
        }

        public class PartialParameter
        {
            public PartialParameter(int pageIndex, int totalCount, int pageSize, int status,
                List<StaSign> list)
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
            public List<StaSign> List { get; set; }
        }

        public class StaSign : Sign
        {
            public string timeStartStr { get; set; }
            public string timeEndStr { get; set; }
            public int UserSignCount { get; set; }
            public int UserCheckCount { get; set; }
        }
    }
}