using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "writeoffer")]
    public class IndexWriteoffer
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "woid", Type = FieldType.String)]
        public string woid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "is_valid", Type = FieldType.Boolean)]
        public bool is_valid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "openid", Type = FieldType.String)]
        public string openid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "timestamp", Type = FieldType.Double)]
        public double timestamp { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "realname", Type = FieldType.String)]
        public string realname { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "phone", Type = FieldType.String)]
        public string phone { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "commission", Type = FieldType.Integer)]
        public int commission { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
