using MD.Model.Configuration.Att;
using MD.Model.Configuration.ElasticSearch;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration
{
    [MDConfig("Log","Default")]
    public class LogConfig : IESIndexInterface
    {
        [MDKey("TraceResponse")]
        public string TraceResponse { get; set; }
        //[MDKey("RemotePort")]
        //public string RemotePort { get; set; }
        //[MDKey("RemoteAddress")]
        //public string RemoteAddress { get; set; }
    }

    [MDConfig("ElasticSearch", "Log")]
    public class LogESConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }
}
