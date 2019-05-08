using MD.Model.Redis.Att.CustomAtts.Sets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects
{
    [RedisHash("tester.hash")]
    [RedisDBNumber("0")]
    public class TesterRedis
    {
        [TesterOpenIdSet("tester.set")]
        [RedisKey]
        public string Openid { get; set; }
    }
}
