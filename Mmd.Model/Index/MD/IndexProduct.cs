using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "product")]
    public class IndexProduct
    {
        /// <summary>
        /// uuid
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "name", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "description", Type = FieldType.String, Analyzer = "ik_smart", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string description { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "p_no", Type = FieldType.Long)]
        public long p_no { get; set; }

        [ElasticProperty(Name = "timestamp", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double timestamp { get; set; }

        [ElasticProperty(Name = "price", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int price { get; set; }

        [ElasticProperty(Name = "category", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int category { get; set; }

        [ElasticProperty(Name = "mid", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Name = "status", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int status { get; set; }

        [ElasticProperty(Name = "advertise_pic_1", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string advertise_pic_1 { get; set; }

        [ElasticProperty(Name = "advertise_pic_2", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string advertise_pic_2 { get; set; }

        [ElasticProperty(Name = "advertise_pic_3", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string advertise_pic_3 { get; set; }

        [ElasticProperty(Name = "aaid", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string aaid { get; set; }

        [ElasticProperty(Name = "last_update_user", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string last_update_user { get; set; }

        [ElasticProperty(Name = "standard", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string standard { get; set; }

        [ElasticProperty(Name = "avgScore", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double avgScore { get; set; }

        [ElasticProperty(Name = "scorePeopleCount", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int scorePeopleCount { get; set; }

        [ElasticProperty(Name = "grassCount", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int grassCount { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
