using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MD.Wechat.Controllers
{
    public class RedirectController : Controller
    {
        // GET: Redirect
        public ActionResult Index(string auth_code, string expires_in)
        {
            //return Redirect(@"http://wx.mmpintuan.com/callback?auth_code=" + auth_code+ "&expires_in="+expires_in)
            ;
            ViewBag.url = @"http://wx.mmpintuan.com/callback?auth_code=" + auth_code + "&expires_in=" + expires_in;
            return View();
        }
    }
}