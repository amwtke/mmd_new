using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "act_box")]
    public class IndexAct_box
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "appid", Type = FieldType.String)]
        public string appid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "title", Type = FieldType.String)]
        public string title { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pic", Type = FieldType.String)]
        public string pic { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "description", Type = FieldType.String)]
        public string description { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "time_start", Type = FieldType.Double)]
        public double time_start { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "time_end", Type = FieldType.Double)]
        public double time_end { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int status { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "last_update_time", Type = FieldType.Double)]
        public double last_update_time { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
