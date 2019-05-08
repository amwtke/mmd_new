using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using MD.Lib.DB.Repositorys;

namespace MD.WeChat.Filters
{
    /// <summary>
    /// 封装一些filter的基本操作。
    /// </summary>
    public class WebApiBaseFilter :ActionFilterAttribute
    {
        protected string getActionName(HttpActionContext context)
        {
            return context.ActionDescriptor.ActionName;
        }
        protected string getActionName(HttpActionExecutedContext context)
        {
            return context.ActionContext.ActionDescriptor.ActionName;
        }


        protected Dictionary<string, object> getActionArguments(HttpActionContext context)
        {
            return context.ActionArguments;
        }

        protected Dictionary<string, object> getActionArguments(HttpActionExecutedContext context)
        {
            return context.ActionContext.ActionArguments;
        }

        protected List<string> getActionArgumentsKeys(HttpActionContext context)
        {
            return getActionArguments(context).Keys.ToList();
        }
        protected List<string> getActionArgumentsKeys(HttpActionExecutedContext context)
        {
            return getActionArguments(context).Keys.ToList();
        }
    }
}