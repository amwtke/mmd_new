using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.ForTest
{
    [RedisDBNumber("10")]
    public class ForTestRedis
    {
        [RedisKey]
        [ForTestAppidSet("fortest.appid.set")]
        public string AppId { get; set; }
    }

    public class ForTestAppidSetAttribute : RedisSetAttribute
    {
        public ForTestAppidSetAttribute(string name) : base(name)
        {
        }
    }
}
