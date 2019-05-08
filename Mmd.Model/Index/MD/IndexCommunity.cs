using Nest;
using System;
using System.Collections.Generic;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "community")]
    public class IndexCommunity
    {
        [ElasticProperty(Name = "Id", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string Id { get; set; }

        [ElasticProperty(Name = "mid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string mid { get; set; }

        [ElasticProperty(Name = "uid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string uid { get; set; }

        [ElasticProperty(Name = "topic_type", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int topic_type { get; set; }

        [ElasticProperty(Name = "flag", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int flag { get; set; }

        [ElasticProperty(Name = "title", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string title { get; set; }

        [ElasticProperty(Name = "content", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string content { get; set; }

        [ElasticProperty(Name = "imgs", Type = FieldType.Nested, Index = FieldIndexOption.NotAnalyzed)]
        public List<string> imgs { get; set; }

        [ElasticProperty(Name = "hits", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int hits { get; set; }

        [ElasticProperty(Name = "praises", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int praises { get; set; }

        [ElasticProperty(Name = "transmits", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int transmits { get; set; }

        [ElasticProperty(Name = "status", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int status { get; set; }

        [ElasticProperty(Name = "createtime", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int createtime { get; set; }

        [ElasticProperty(Name = "lastupdatetime", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int lastupdatetime { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
