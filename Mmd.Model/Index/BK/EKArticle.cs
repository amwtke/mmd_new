using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index
{
    [ElasticType(Name = "Ekarticle")]
    public class EKIndex
    {
        [ElasticProperty(Name = "AccountEmail", Index = FieldIndexOption.NotAnalyzed,Type = FieldType.String)]
        public string AccountEmail { get; set; }

        [ElasticProperty(Name = "IsExotic", Index = FieldIndexOption.NotAnalyzed, Type= FieldType.Boolean)]
        public bool IsExotic { get; set; }

        [ElasticProperty(Name = "IsPublic", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Boolean)]
        public bool IsPublic { get; set; }

        [ElasticProperty(Name = "IsTop", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double IsTop { get; set; }

        [ElasticProperty(Name = "Title", Index = FieldIndexOption.Analyzed, Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string Title { get; set; }

        [ElasticProperty(Name = "ArticleType", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string ArticleType { get; set; }

        [ElasticProperty(Name = "BodyText", Index = FieldIndexOption.Analyzed, Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string BodyText { get; set; }

        [ElasticProperty(Name = "HeadPic", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string HeadPic { get; set; }

        [ElasticProperty(Name = "PublicDate", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double PublicDate { get; set; }

        [ElasticProperty(Name = "Keywords", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string Keywords { get; set; }

        [ElasticProperty(Name = "Abstract", Index = FieldIndexOption.Analyzed, Type = FieldType.String)]
        public string Abstract { get; set; }

        [ElasticProperty(Name = "HitPoint", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int HitPoint { get; set; }

        [ElasticProperty(Name = "ReadPoint", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int ReadPoint { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "AccountEmailUuid", Type = FieldType.String)]
        public string AccountEmailUuid { get; set; }
    }
}
