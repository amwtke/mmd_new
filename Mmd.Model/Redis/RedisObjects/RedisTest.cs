using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Redis;

namespace MD.Model.Redis.RedisObjects
{
    [RedisDBNumber("0")]
    public class RedisTest
    {
        [RedisKey]
        [TestString("xiao")]
        public string RedisKey { get; set; }
    }

    public class TestStringAttribute : RedisStringAttribute
    {
        public TestStringAttribute(string name) : base(name)
        {
        }
    }
}
