using Nest;
using System;
using System.Collections.Generic;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "laddergroup")]
    public class IndexLadderGroup
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pid", Type = FieldType.String)]
        public string pid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "title", Type = FieldType.String)]
        public string title { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "description", Type = FieldType.String)]
        public string description { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pic", Type = FieldType.String)]
        public string pic { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "waytoget", Type = FieldType.Integer)]
        public int? waytoget { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "product_count", Type = FieldType.Integer)]
        public int product_count { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "product_quotacount", Type = FieldType.Integer)]
        public int product_quotacount { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "origin_price", Type = FieldType.Integer)]
        public int origin_price { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int status { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "start_time", Type = FieldType.Double)]
        public double start_time { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "end_time", Type = FieldType.Double)]
        public double end_time { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "last_update_time", Type = FieldType.Double)]
        public double last_update_time { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "PriceList", Type = FieldType.Nested)]
        public List<LadderPrice> PriceList { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }

    public class LadderPrice
    {
        public int person_count { get; set; }
        public int group_price { get; set; }
    }
}
