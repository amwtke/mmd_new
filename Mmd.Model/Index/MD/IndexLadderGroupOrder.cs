using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "laddergrouporder")]
    public class IndexLadderGroupOrder
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "go_no", Type = FieldType.String)]
        public string go_no { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "gid", Type = FieldType.String)]
        public string gid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pid", Type = FieldType.String)]
        public string pid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "leader", Type = FieldType.String)]
        public string leader { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "expire_date", Type = FieldType.Double)]
        public double expire_date { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "price", Type = FieldType.Integer)]
        public int price { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "go_price", Type = FieldType.Integer)]
        public int go_price { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int status { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "create_date", Type = FieldType.Double)]
        public double create_date { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
