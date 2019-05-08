using MD.Model.Configuration.Att;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration.Redis
{
    public class RedisConfigBase
    {
        [MDKey("MasterHostAndPort")]
        public string MasterHostAndPort { get; set; }

        [MDKey("SlaveHostsAndPorts")]
        public string SlaveHostsAndPorts { get; set; }

        [MDKey("Password")]
        public string Password { get; set; }

        [MDKey("StringSeperator")]
        public string StringSeperator { get; set; }
    }
}
