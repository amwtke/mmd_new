using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "writeoffpoint")]
    public class IndexWriteOffPoint
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "address", Type = FieldType.String)]
        public string address { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "name", Type = FieldType.String)]
        public string name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "tel", Type = FieldType.String)]
        public string tel { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "cell_phone", Type = FieldType.Long)]
        public long? cell_phone { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "contact_person", Type = FieldType.String)]
        public string contact_person { get; set; }
        /// <summary>
        /// 空代表是总店
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "parent", Type = FieldType.String)]
        public string parent { get; set; }//空代表总店

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "is_valid", Type = FieldType.Boolean)]
        public bool? is_valid { get; set; }//空代表总店

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "timestamp", Type = FieldType.Double)]
        public double? timestamp { get; set; }//空代表总店

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "longitude", Type = FieldType.Double)]
        public double longitude { get; set; }//经度

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "latitude", Type = FieldType.Double)]
        public double latitude { get; set; }//纬度

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
