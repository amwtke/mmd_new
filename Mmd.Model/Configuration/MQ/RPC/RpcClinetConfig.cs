using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;

namespace MD.Model.Configuration.MQ.RPC
{
    [MDConfig("RPC","Client")]
    public class RpcClinetConfig
    {
        [MDKey("ClientRetryTimes")]
        public string ClientRetryTimes { get; set; }

        [MDKey("RetryDelta")]
        public string RetryDelta { get; set; }
    }
}
