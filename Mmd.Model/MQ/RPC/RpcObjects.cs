using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.MQ.RPC
{
    [Serializable]
    public class RpcArgs
    {
        ConcurrentDictionary<string,object> _args = new ConcurrentDictionary<string, object>();
        public RpcArgs() { }

        public object this[string argName]
        {
            get
            {
                if(!string.IsNullOrEmpty(argName))
                    return _args[argName];
                return null;
            }
            set
            {
                if(!string.IsNullOrEmpty(argName))
                    _args[argName] = value;
            }
        }
    }


    [Serializable]
    public class RpcResults
    {
        ConcurrentDictionary<string, object> _results = new ConcurrentDictionary<string, object>();
        public RpcResults() { }

        public object this[string argName]
        {
            get
            {
                if (!string.IsNullOrEmpty(argName))
                    return _results[argName];
                return null;
            }
            set
            {
                if (!string.IsNullOrEmpty(argName))
                    _results[argName] = value;
            }
        }
    }
}
