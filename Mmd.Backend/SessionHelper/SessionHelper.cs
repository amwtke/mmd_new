using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Lib.DB.Repositorys;
using MD.Model.DB;
using RabbitMQ.Client.Impl;

namespace Mmd.Backend
{
    public enum ESessionStateKeys
    {
        AppId,
        Mid,
        MerName,
        MerStatus,
    }
    public static class SessionHelper
    {
        public static object Get(Controller controller, ESessionStateKeys key)
        {
            string keyString = Enum.GetName(typeof(ESessionStateKeys), key);            
            //return HttpContext.Current.Session[keyString];
            return controller.Session[keyString];
        }

        public static object Set(Controller controller, ESessionStateKeys key, object value)
        {
            string keyString = Enum.GetName(typeof(ESessionStateKeys), key);
            return controller.Session[keyString] = value;
        }

        public static async Task<Merchant> GetMerchant(Controller controller)
        {
            object value = Get(controller,ESessionStateKeys.AppId);
            if (value == null) return null;
            using (var repos = new BizRepository())
            {
                string appid = value.ToString();
                return await repos.GetMerchantByAppidAsync(appid);
            }
        }

        public static void Logout(Controller con)
        {
            con.Session.Abandon();
        }
    }
}