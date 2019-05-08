using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.UI;
using MD.Model.DB.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Backend.Controllers.Backyard
{
    public class BrandController : Controller
    {
        // GET: Brand
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult Modify(int code,string value)
        {
            try
            {
                using (var repo = new CodeRepository())
                {
                    bool flag = repo.UpdateProductBrandAsync(code, value).Result;
                    return Json(new { result = flag });
                }
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex });
            }
        }

        public JsonResult Add(string value)
        {
            try
            {
                using (var repo = new CodeRepository())
                {
                    bool flag = repo.AddBrand(value);
                    return Json(new { result = flag });
                }
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex });
            }
        }

        public async Task<JsonResult> Del(int code)
        {
            try
            {
                using (var repo = new CodeRepository())
                {
                    var tuple = await EsSupplyManager.SearchAsnyc("", 1, 1, new List<int> { (int) ESupplyStatus.已上线 }, null, code);
                    if (tuple.Item1 > 0)
                    {
                        return Json(new { result = false,message = "当前品牌下有商品，不能删除！"});
                    }
                    bool flag = await repo.DelProductBrandAsync(code);
                    return Json(new { result = flag });
                }
            }
            catch (Exception ex)
            {
                return Json(new { result = false, message = ex });
            }
        }
        public async Task<PartialViewResult> BrandListPartial(int pageIndex, string q)
        {
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(ProductController), "UiBackEndConfig 没取到！");
            int size = int.Parse(uiConfig.PageSize);
            using (var repo = new CodeRepository())
            {
                var tuple = repo.GetProductBrand(pageIndex, size, q);
                return PartialView("Backyard/Brand/BrandListPartial", new BrandPartialObject()
                {
                    List = tuple.Item2,
                    PageIndex = pageIndex,
                    PageSize = size,
                    Q = q,
                    TotalCount = tuple.Item1
                });
            }
        }

        public class BrandPartialObject
        {
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int? category { get; set; }
            public int? brand { get; set; }
            public List<Codebrand> List { get; set; }
            public string Q { get; set; }
        }
    }
}