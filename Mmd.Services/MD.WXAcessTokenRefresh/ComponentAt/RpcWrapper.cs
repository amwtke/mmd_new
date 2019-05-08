using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.MQ.RPC.ArgsAndRets;
using MD.Model.MQ.RPC;

namespace MD.WXAcessTokenRefresh.ComponentAt
{
    public static class RpcWrapper
    {
        public static RpcResults GetComponentAtWrapper(RpcArgs args)
        {
            string at = Helper.GetComponentAt();
            var ret = GetComponentAtFunc.GenResults(at);
            return ret;
        }

        public static RpcResults GetAuthorizerAtWrapper(RpcArgs args)
        {
            string appid = GetAuthorizerAtFunc.GenAppidFromRpcArgs(args);
            if (!string.IsNullOrEmpty(appid))
            {
                var at = Helper.GetAuthorizerAtByAppId(appid);
                var ret = GetAuthorizerAtFunc.GenResults(at);
                return ret;
            }
            return null;
        }
    }
}
