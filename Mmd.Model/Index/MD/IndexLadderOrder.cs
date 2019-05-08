using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "ladderorder")]
    public class IndexLadderOrder
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "o_no", Type = FieldType.String)]
        public string o_no { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "goid", Type = FieldType.String)]
        public string goid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "gid", Type = FieldType.String)]
        public string gid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "waytoget", Type = FieldType.Integer)]
        public int waytoget { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "paytime", Type = FieldType.Double)]
        public double? paytime { get; set; }//支付时间,可以是形成订单的时间

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "order_price", Type = FieldType.Integer)]
        public int order_price { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int status { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "buyer", Type = FieldType.String)]
        public string buyer { get; set; }//发起者id

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "default_writeoff_point", Type = FieldType.String)]
        public string default_writeoff_point { get; set; }//提货点

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeoffer", Type = FieldType.String)]
        public string writeoffer { get; set; }//核销员uuid

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeoffday", Type = FieldType.Double)]
        public double? writeoffday { get; set; }//核销时间

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeoff_point", Type = FieldType.String)]
        public string writeoff_point { get; set; }//核销地址

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
