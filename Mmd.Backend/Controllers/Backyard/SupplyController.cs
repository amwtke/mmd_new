using MD.Configuration;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Aliyun;
using MD.Model.Configuration.UI;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Backend.Controllers.Backyard
{
    public class SupplyController : Controller
    {
        // GET: Supply
        #region backend
        public async Task<ActionResult> Index()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }
        public async Task<ActionResult> AddSupply()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            Supply supply = new Supply() { sid = Guid.NewGuid() };
            return View(supply);
        }
        public async Task<ActionResult> EditSupply(Guid sid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            using (var repo = new BizRepository())
            {
                var supply = await repo.GetSupplyBySidAsync(sid);
                return View("AddSupply", supply);
            }
        }
        public async Task<ActionResult> UpdateSupplystatus(Guid sid,int status)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            using (var repo = new BizRepository())
            {
                var flag = await repo.UpdateSupplystatusAsync(sid, status);
                if (flag)
                {
                    //更新ES
                    var pEs = await EsSupplyManager.GenObject(sid);
                    if (!await EsSupplyManager.AddOrUpdateAsync(pEs))
                    {
                        return Content("Es update failed!");
                    }
                }
                return View("Index");
            }
        }
        public async Task<ActionResult> SupplySave(Supply supply, HttpPostedFileBase pic1, HttpPostedFileBase pic2, HttpPostedFileBase pic3)
        {
            try
            {
                if (supply.sid.Equals(Guid.Empty))
                    return Content("sid is null!");
                string fileName = CommonHelper.GetUnixTimeNow().ToString();
                //上传第一张图片，并获取路径
                if (pic1 != null && pic1.ContentLength > 0)
                {
                    var path1 = OssPicPathManager<OssPicBucketConfig>.UploadSupplyPic(supply.sid, fileName.ToString() + "_1", pic1.InputStream);
                    if (!string.IsNullOrEmpty(path1))
                        supply.advertise_pic_1 = path1;
                    else
                        throw new MDException(typeof(SupplyController), "上传1文件失败！");
                    supply.advertise_pic_1 = path1;
                }

                //上传第二张图片，并获取路径
                if (pic2 != null && pic2.ContentLength > 0)
                {
                    var path2 = OssPicPathManager<OssPicBucketConfig>.UploadSupplyPic(supply.sid, fileName.ToString() + "_2", pic2.InputStream);
                    if (!string.IsNullOrEmpty(path2))
                        supply.advertise_pic_2 = path2;
                    else
                        throw new MDException(typeof(SupplyController), "上传2文件失败！");
                    supply.advertise_pic_2 = path2;
                }
                //上传第三张图片，并获取路径
                if (pic3 != null && pic3.ContentLength > 0)
                {
                    var path3 = OssPicPathManager<OssPicBucketConfig>.UploadSupplyPic(supply.sid, fileName.ToString() + "_3", pic3.InputStream);
                    if (!string.IsNullOrEmpty(path3))
                        supply.advertise_pic_3 = path3;
                    else
                        throw new MDException(typeof(SupplyController), "上传3文件失败！");
                    supply.advertise_pic_3 = path3;
                }
                //图片上传成功，获得三张图片的路径，将由表单得到的数据存入BizRepository中
                using (BizRepository repo = new BizRepository())
                {
                    //将数据存入DB
                    await repo.SaveOrUpdateSupplyAsync(supply);
                    //数据存入之后，将存完数据后的对象取出来
                    var sup = await repo.GetSupplyBySidAsync(supply.sid);

                    //存入ES
                    var pEs = await EsSupplyManager.GenObject(sup.sid);
                    if (!await EsSupplyManager.AddOrUpdateAsync(pEs))
                    {
                        return Content("Es failed!");
                    }
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(SupplyController), ex);
            }
            return RedirectToAction("Index");
        }

        public async Task<PartialViewResult> SupplyListPartial_Backend(int pageIndex, string q, int? category = null, int? brand = null)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            }

            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(ProductController), "UiBackEndConfig 没取到！");
            int size = int.Parse(uiConfig.PageSize);
            var tuple = await EsSupplyManager.SearchAsnyc(q, pageIndex, size,new List<int>() { (int)ESupplyStatus.已上线,(int)ESupplyStatus.已下线 }, category, brand);
            using (var repo = new BizRepository())
            {
                List<Supply> ret = await repo.GetSupplyBySidAsync(tuple.Item2);
                return PartialView("Backyard/Supply/SupplyListPartial", new SupplyPartialObject()
                {
                    brand = brand,
                    category = category,
                    List = ret,
                    PageIndex = pageIndex,
                    PageSize = size,
                    Q = q,
                    TotalCount = tuple.Item1
                });
            }
        }

        public class SupplyPartialObject
        {
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int? category { get; set; }
            public int? brand { get; set; }
            public List<Supply> List { get; set; }
            public string Q { get; set; }
        }
        #endregion
        #region 商家后台
        public async Task<ActionResult> Index_mer()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }
        public async Task<PartialViewResult> SupplyListPartial_Merchant( string q, int pageIndex = 1, int? category = null, int? brand = null)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            }

            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(ProductController), "UiBackEndConfig 没取到！");
            int size = int.Parse(uiConfig.PageSize);
            var tuple = await EsSupplyManager.SearchAsnyc(q, pageIndex, size,new List<int>() { (int)ESupplyStatus.已上线}, category, brand);
            using (var repo = new BizRepository())
            {
                List<Supply> ret = await repo.GetSupplyBySidAsync(tuple.Item2);
                return PartialView("Supply/SupplyListPartial", new SupplyPartialObject()
                {
                    brand = brand,
                    category = category,
                    List = ret,
                    PageIndex = pageIndex,
                    PageSize = size,
                    Q = q,
                    TotalCount = tuple.Item1
                });
            }
        }
        public async Task<ActionResult> SupplyDetail(Guid sid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            using (var reop=new BizRepository())
            {
                var supply =await reop.GetSupplyBySidAsync(sid);
                if (supply == null || supply.sid.Equals(Guid.Empty))
                    return Content($"supply is null,sid:{sid}");
                supply.description = HttpUtility.HtmlDecode(supply.description).Replace("\"", "\'");
                return View("SupplyDetail",supply);
            }
        }
        #endregion

    }
}