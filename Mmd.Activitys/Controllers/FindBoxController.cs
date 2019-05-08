using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB.Activity;
using MD.Model.DB.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Activitys.Controllers
{
    public class FindBoxController : Controller
    {
        // GET: FindBox
        public ActionResult Index()
        {
            return View();
        }
    }
}