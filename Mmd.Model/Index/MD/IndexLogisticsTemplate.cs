using Nest;
using System;
using System.Collections.Generic;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "logistics_template")]
    public class IndexLogisticsTemplate
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "name", Type = FieldType.String)]
        public string name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "items", Type = FieldType.Nested)]
        public List<LogisticsTemplateItem> items { get; set; }
    }

    public class LogisticsTemplateItem
    {
        public string id { get; set; }
        public int first_amount { get; set; }
        public int first_fee { get; set; }
        public int additional_amount { get; set; }
        public int additional_fee { get; set; }
        public List<string> regions { get; set; }
    }
}
