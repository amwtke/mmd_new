using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace mmd.wechat
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务.跨域访问能力。
            config.EnableCors(new EnableCorsAttribute("*", "*", "GET,POST"));
            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
    name: "api2",
    routeTemplate: "api2/{controller}/{action}/{id}",
    defaults: new { controller = "TestRepository", action = "test", id = RouteParameter.Optional }
);
        }
    }
}
