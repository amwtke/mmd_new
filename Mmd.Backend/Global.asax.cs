using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MD.Lib.DB.Redis;
using MD.Lib.MQ;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration.Redis;

namespace Mmd.Backend
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_End()
        {
            WXComponentHelper.Close();
            RedisManager2<WeChatRedisConfig>.CloseAll();
            MQManager.CloseAll();
        }
    }
}
