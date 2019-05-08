using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.att.CustomAtts.lists
{
    public class TestIdListAttribute : RedisListAttribute
    {
        public TestIdListAttribute(string name, ListPush push) : base(name, push)
        { }
    }

    public class TestId2ListAttribute : RedisListAttribute
    {
        public TestId2ListAttribute(string name, ListPush push) : base(name, push)
        { }
    }
}
