using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Backend.Controllers
{
    public class SessionController : Controller
    {
        // GET: Session
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SessionTimeOut()
        {
            return RedirectToAction("Merchant_Home", "Home");
        }

        public ActionResult Logout()
        {
            SessionHelper.Logout(this);
            return RedirectToAction("Merchant_Home", "Home");
        }
    }
}