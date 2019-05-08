using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "grouporder")]
    public class IndexGroupOrder
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "go_no", Type = FieldType.String)]
        public string go_no { get; set; }//数字代码,便于记忆,可以根据时间来生成

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "gid", Type = FieldType.String)]
        public string gid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pid", Type = FieldType.String)]
        public string pid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "leader", Type = FieldType.String)]
        public string leader { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "user_left", Type = FieldType.Integer)]
        public int? user_left { get; set; }//还剩几个

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "price", Type = FieldType.Integer)]
        public int? price { get; set; }//原价:单位分

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "go_price", Type = FieldType.Integer)]
        public int? go_price { get; set; }//原价:单位分

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int? status { get; set; }//团购订单的状态：团购失败 团购进行中 团购成功

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "create_date", Type = FieldType.Double)]
        public double? create_date { get; set; }//组团时间

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "expire_date", Type = FieldType.Double)]
        public double? expire_date { get; set; }//结束时间

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
