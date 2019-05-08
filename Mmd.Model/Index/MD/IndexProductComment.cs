using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "productcomment")]
    public  class IndexProductComment
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pid", Type = FieldType.String)]
        public string pid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "uid", Type = FieldType.String)]
        public string uid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "u_age", Type = FieldType.Integer)]
        public int u_age { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "u_skin", Type = FieldType.Integer)]
        public int u_skin { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "score", Type = FieldType.Integer)]
        public int score { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "comment", Type = FieldType.String)]
        public string comment { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "comment_reply", Type = FieldType.String)]
        public string comment_reply { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "imglist", Type = FieldType.String)]
        public string imglist { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "isessence", Type = FieldType.Integer)]
        public int isessence { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "praise_count", Type = FieldType.Integer)]
        public int praise_count { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "timestamp", Type = FieldType.Double)]
        public double timestamp { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "timestamp_reply", Type = FieldType.Double)]
        public double? timestamp_reply { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
