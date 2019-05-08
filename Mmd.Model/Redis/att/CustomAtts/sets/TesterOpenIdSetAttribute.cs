using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Att.CustomAtts.Sets
{
    public class TesterOpenIdSetAttribute : RedisSetAttribute
    {
        public TesterOpenIdSetAttribute(string name) : base(name)
        { }
    }
}
