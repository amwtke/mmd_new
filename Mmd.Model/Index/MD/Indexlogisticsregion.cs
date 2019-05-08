using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "logisticsregion")]
    public class Indexlogisticsregion
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.Integer)]
        public int Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "name", Type = FieldType.String)]
        public string name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "orderId", Type = FieldType.Integer)]
        public int orderId { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "categoryLevel", Type = FieldType.Integer)]
        public int categoryLevel { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "fatherId", Type = FieldType.Integer)]
        public int fatherId { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "code", Type = FieldType.String)]
        public string code { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "family", Type = FieldType.String)]
        public string family { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "remark", Type = FieldType.String)]
        public string remark { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "isDeleted", Type = FieldType.Integer)]
        public int isDeleted { get; set; }
    }
}
