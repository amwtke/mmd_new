using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "act_boxtreasure")]
   public class IndexAct_boxtreasure
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "bid", Type = FieldType.String)]
        public string bid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "name", Type = FieldType.String)]
        public string name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "count", Type = FieldType.Integer)]
        public int count { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "quota_count", Type = FieldType.Integer)]
        public int quota_count { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "description", Type = FieldType.String)]
        public string description { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pic", Type = FieldType.String)]
        public string pic { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
