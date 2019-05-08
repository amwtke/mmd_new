using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "noticeboard")]
    public class IndexNoticeBoard
    {
        [ElasticProperty(Name = "Id",Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string Id { get; set; }

        [ElasticProperty(Name = "title", Type = FieldType.String, Index = FieldIndexOption.Analyzed, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string title { get; set; }

        [ElasticProperty(Name = "mid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string mid { get; set; }

        [ElasticProperty(Name = "source", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string source { get; set; }

        [ElasticProperty(Name = "category", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int category { get; set; }

        [ElasticProperty(Name = "tag_1", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string tag_1 { get; set; }

        [ElasticProperty(Name = "tag_2", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string tag_2 { get; set; }

        [ElasticProperty(Name = "tag_3", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string tag_3 { get; set; }

        [ElasticProperty(Name = "thumb_pic", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string thumb_pic { get; set; }

        [ElasticProperty(Name = "description", Type = FieldType.String, Index = FieldIndexOption.Analyzed, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string description { get; set; }

        [ElasticProperty(Name = "status", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int status { get; set; }

        [ElasticProperty(Name = "timestamp", Type = FieldType.Double, Index = FieldIndexOption.NotAnalyzed)]
        public double timestamp { get; set; }

        [ElasticProperty(Name = "hits_count", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int hits_count { get; set; }

        [ElasticProperty(Name = "praise_count", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int praise_count { get; set; }

        [ElasticProperty(Name = "transmit_count", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int transmit_count { get; set; }

        [ElasticProperty(Name = "extend_1", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string extend_1 { get; set; }

        [ElasticProperty(Name = "extend_2", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string extend_2 { get; set; }

        [ElasticProperty(Name = "extend_3", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string extend_3 { get; set; }
        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
