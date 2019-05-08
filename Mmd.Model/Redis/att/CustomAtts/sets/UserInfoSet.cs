using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.att.CustomAtts.sets
{
    public class UserInfoSetOpenIdSetAttribute : RedisSetAttribute
    {
        public UserInfoSetOpenIdSetAttribute(string name) : base(name) { }
    }

    public class UserInfoSetUnionIdSetAttribute : RedisSetAttribute
    {
        public UserInfoSetUnionIdSetAttribute(string name) : base(name) { }
    }
}
