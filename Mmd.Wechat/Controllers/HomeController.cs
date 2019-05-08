using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Lib.Weixin.Component;

namespace mmd.wechat.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public async Task<ActionResult> Index()
        {
            ViewBag.url = await WXComponentHelper.GetAuthUrlAsync();
            return View();
        }

        public async Task<ActionResult> about()
        {
            ViewBag.url = await WXComponentHelper.GetAuthUrlAsync();
            return View();
        }

        public async Task<ActionResult> examples()
        {
            ViewBag.url = await WXComponentHelper.GetAuthUrlAsync();
            return View();
        }

        public async Task<ActionResult> intro()
        {
            ViewBag.url = await WXComponentHelper.GetAuthUrlAsync();
            return View();
        }

        public ActionResult Upload()
        {
            return View();
        }

        public JsonResult UploadTxt(HttpPostedFileBase txtFile)
        {
            try
            {
                if (txtFile.ContentType != "text/plain")
                {
                    return Json(new { status = "Fail", message = "FileTypeError" });
                }
                string fileName = txtFile.FileName;
                string path = Server.MapPath("/" + fileName);
                txtFile.SaveAs(path);
                return Json(new { status = "Success", message = "Success" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error",message = ex.Message});
            }
        }
    }
}