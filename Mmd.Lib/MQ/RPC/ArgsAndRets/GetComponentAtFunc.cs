using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.MQ.RPC;

namespace MD.Lib.MQ.RPC.ArgsAndRets
{
    public static class GetComponentAtFunc
    {
        public static RpcArgs GenArgs()
        {
            RpcArgs ret = new RpcArgs {["NoArgs"] = "True"};
            return ret;
        }

        public static RpcResults GenResults(string at)
        {
            RpcResults ret = new RpcResults {["at"] = at};
            return ret;
        }

        public static string GetAtFromRpcResult(RpcResults rr)
        {
            return rr["at"].ToString();
        }
    }
}
