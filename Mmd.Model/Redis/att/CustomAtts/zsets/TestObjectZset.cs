using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.att.CustomAtts.zsets
{
    public class TestObjectIdZSetAttribute : RedisZSetAttribute
    {
        public TestObjectIdZSetAttribute(string name,string fieldName) : base(name, fieldName)
        { }
    }
    public class TestObjectId2ZSetAttribute : RedisZSetAttribute
    {
        public TestObjectId2ZSetAttribute(string name, string fieldName) : base(name, fieldName)
        { }
    }
}
