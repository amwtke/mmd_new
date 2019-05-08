using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index
{
    [ElasticType(Name = "professor")]
    public class ProfessorIndex
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }
        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "ResearchId", Type = FieldType.String, Analyzer = "ik_smart", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public long ResearchId { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "AccessCount", Type = FieldType.Double)]
        public double AccessCount { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "DiweiScore", Type = FieldType.Integer)]
        public int DiweiScore { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Interests", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string Interests { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Diwei", Type = FieldType.String, Analyzer = "ik_smart", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string Diwei { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Education", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string Education { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Danwei", Type = FieldType.String, Analyzer = "ik_smart", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string Danwei { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Name", Type = FieldType.String, Analyzer = "ik_smart", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string Name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Address", Type = FieldType.String, Analyzer = "ik_smart", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string Address { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
