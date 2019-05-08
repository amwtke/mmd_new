using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.att.CustomAtts.sets
{
    public class TestObjectIdSetAttribute : RedisSetAttribute
    {
        public TestObjectIdSetAttribute(string name) : base(name)
        { }
    }
    public class TestObjectId2SetAttribute : RedisSetAttribute
    {
        public TestObjectId2SetAttribute(string name) : base(name)
        { }
    }
}
