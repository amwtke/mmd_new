using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MD.Backend.MDRoute;

namespace Mmd.Backend
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            ////第三方平台接收微信消息的控制器
            //routes.MapRoute(
            //    name: "wxopen",
            //    url:"{action}/{id}",
            //    defaults:new {controller="Home",action="Index",id=UrlParameter.Optional}
            //    );
            //第三方平台接收微信消息的控制器
            routes.MapRoute(
                name: "Msg",
                url: "{appid}/callback",
                defaults: new { controller = "Msg", action = "callback", id = UrlParameter.Optional });
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
