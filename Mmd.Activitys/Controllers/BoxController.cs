using MD.Lib.Util;
using MD.Model.DB.Activity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Activitys.Controllers
{
    public class BoxController : Controller
    {
        // GET: Box
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ModifyBox(Box box,string type)
        {
            foreach (var item in box.BoxTreasureList)
            {
                item.tid = Guid.NewGuid();
               
            }
            return Json(box);
        }
    }
}