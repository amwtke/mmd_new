using Nest;
namespace MD.Model.Index.MD
{
    [ElasticType(Name = "act_sign")]
    public class IndexAct_sign
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "appid", Type = FieldType.String)]
        public string appid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "timeStart", Type = FieldType.Double)]
        public double timeStart { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "timeEnd", Type = FieldType.Double)]
        public double timeEnd { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "awardName", Type = FieldType.String)]
        public string awardName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "awardDescription", Type = FieldType.String)]
        public string awardDescription { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "awardCount", Type = FieldType.Integer)]
        public int awardCount { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "awardQuatoCount", Type = FieldType.Integer)]
        public int awardQuatoCount { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "awardPic", Type = FieldType.String)]
        public string awardPic { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mustSignCount", Type = FieldType.Integer)]
        public int mustSignCount { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "title", Type = FieldType.String)]
        public string title { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "description", Type = FieldType.String)]
        public string description { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int status { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "last_update_time", Type = FieldType.Double)]
        public double last_update_time { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
