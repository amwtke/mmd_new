using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;
using MD.Model.Configuration.ElasticSearch;

namespace MD.Model.Configuration.ElasticSearch.MD
{
    [MDConfig("ElasticSearch", "Group")]
    public class EsGroupConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }

    [MDConfig("ElasticSearch", "GroupOrder")]
    public class EsGroupOrderConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }

    [MDConfig("ElasticSearch", "Order")]
    public class EsOrderConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }
}




