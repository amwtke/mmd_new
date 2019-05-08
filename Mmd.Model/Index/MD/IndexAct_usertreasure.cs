using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "act_usertreasure")]
   public class IndexAct_usertreasure
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "uid", Type = FieldType.String)]
        public string uid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "openid", Type = FieldType.String)]
        public string openid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "btid", Type = FieldType.String)]
        public string btid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "bid", Type = FieldType.String)]
        public string bid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int status { get; set; }
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "open_time", Type = FieldType.Double)]
        public double open_time { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeofftime", Type = FieldType.Double)]
        public double writeofftime { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeoffer", Type = FieldType.String)]
        public string writeoffer { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
