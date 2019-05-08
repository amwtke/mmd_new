using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;

namespace MD.Model.Configuration.ElasticSearch.MD
{
    [MDConfig("ElasticSearch", "Product")]
    public class EsProductConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }
    [MDConfig("ElasticSearch", "ProductComment")]
    public class EsProductCommentConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }

    [MDConfig("ElasticSearch", "ProductGrass")]
    public class EsProductGrassConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }
    [MDConfig("ElasticSearch", "ProductCommentPraise")]
    public class EsProductCommentPraiseConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }
}
