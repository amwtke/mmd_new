using MD.Model.Configuration.Att;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration.ElasticSearch
{
    public class IESIndexInterface
    {
        [MDKey("RemotePort")]
        public string RemotePort { get; set; }

        [MDKey("RemoteAddress")]
        public string RemoteAddress { get; set; }
    }
}
