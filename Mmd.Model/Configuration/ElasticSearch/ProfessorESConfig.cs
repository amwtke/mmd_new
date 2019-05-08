using MD.Model.Configuration.Att;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration.ElasticSearch
{
    [MDConfig("ElasticSearch", "Professor")]
    public class ProfessorESConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }
}
