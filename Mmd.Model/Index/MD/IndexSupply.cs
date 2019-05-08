using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "supply")]
    public class IndexSupply
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "s_no", Type = FieldType.Long)]
        public long s_no { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "name", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "category", Type = FieldType.Integer)]
        public int category { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "brand", Type = FieldType.Integer)]
        public int brand { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "market_price", Type = FieldType.Integer)]
        public int market_price { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "supply_price", Type = FieldType.Integer)]
        public int supply_price { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "group_price", Type = FieldType.Integer)]
        public int group_price { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "standard", Type = FieldType.String)]
        public string standard { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "quota_min", Type = FieldType.Integer)]
        public int quota_min { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "quota_max", Type = FieldType.Integer)]
        public int quota_max { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pack", Type = FieldType.String)]
        public string pack { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "advertise_pic_1", Type = FieldType.String)]
        public string advertise_pic_1 { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "advertise_pic_2", Type = FieldType.String)]
        public string advertise_pic_2 { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "advertise_pic_3", Type = FieldType.String)]
        public string advertise_pic_3 { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "description", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string description { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "timestamp", Type = FieldType.Double)]
        public double timestamp { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int status { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "headpic_dir", Type = FieldType.String)]
        public string headpic_dir { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
