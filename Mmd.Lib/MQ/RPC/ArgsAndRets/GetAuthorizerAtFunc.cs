using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.MQ.RPC;

namespace MD.Lib.MQ.RPC.ArgsAndRets
{
    public static class GetAuthorizerAtFunc
    {
        public static RpcArgs GenArgs(string appid)
        {
            RpcArgs ret = new RpcArgs { ["AppId"] = appid };
            return ret;
        }

        public static string GenAppidFromRpcArgs(RpcArgs ra)
        {
            return ra["AppId"]?.ToString();
        }

        public static RpcResults GenResults(string at)
        {
            RpcResults ret = new RpcResults { ["at"] = at };
            return ret;
        }

        public static string GetAtFromRpcResult(RpcResults rr)
        {
            return rr["at"]?.ToString();
        }
    }
}
