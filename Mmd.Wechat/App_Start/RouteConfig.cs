using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MD.WeChat.MDRoute;

namespace mmd.wechat
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            /*越特殊的越往前。
            */

            //微信支付——支付回调路径
            routes.Add("wxCallback", new DomainRoute(
                domain: "paycallback.mmpintuan.com",
                url: "",
                defaults: new { controller = "WxPayCallBack", action = "Index" }
                ));

            //美美哒自己公众号的控制器
            routes.Add("wechat", new DomainRoute(
                domain: "wechat.mmpintuan.com",
                url: "",
                defaults: new { controller = "WeChat", action = "Index" }
                ));

            //第三方平台接收微信消息的控制器
            routes.MapRoute(name: "Msg",
                url: "{appid}/callback",
                defaults: new { controller = "Msg", action = "callback", id = UrlParameter.Optional });


            //拼团的控制器,支付的目录
            routes.Add("pintuan", new DomainRoute(
                domain: "{appid}.wx.mmpintuan.com",
                url: "{controller}/{action}/{id}",
                defaults: new { appid = "", controller = "Group", action = "Index", id = "" }
                ));

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
