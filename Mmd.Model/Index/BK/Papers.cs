using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index
{
    [ElasticType(Name = "papers")]
    public class PapersIndex
    {
        //标题
        [ElasticProperty(Name = "Title", Index = FieldIndexOption.Analyzed, Type = FieldType.String, Analyzer= "ik", IndexAnalyzer= "ik_max_word",SearchAnalyzer = "ik_smart")]
        public string Title { get; set; }
        //作者
        [ElasticProperty(Name = "Author", Index = FieldIndexOption.Analyzed, Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string Author { get; set; }
        //发布时间
        [ElasticProperty(Name = "PublishTime", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double PublishTime { get; set; }
        //原文下载地址 默认空 有值的话 照抄
        [ElasticProperty(Name = "ArticlePath", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string ArticlePath { get; set; }
        //期刊名
        [ElasticProperty(Name = "PostMagazine", Index = FieldIndexOption.Analyzed, Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string PostMagazine { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "AccountEmailUuid", Type = FieldType.String)]
        public string AccountEmailUuid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string KeyWords { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "ResearchFieldId", Type = FieldType.Long)]
        public long ResearchFieldId { get; set; }
    }
}
