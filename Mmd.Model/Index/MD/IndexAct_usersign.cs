using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "act_usersign")]
    public class IndexAct_usersign
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "uid", Type = FieldType.String)]
        public string uid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "openid", Type = FieldType.String)]
        public string openid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "sid", Type = FieldType.String)]
        public string sid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int status { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "signTime", Type = FieldType.Double)]
        public double signTime { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeoffTime", Type = FieldType.Double)]
        public double writeoffTime { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeoffer", Type = FieldType.String)]
        public string writeoffer { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
