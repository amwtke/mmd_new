using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "communitybiz")]
    public class IndexCommunityBiz
    {
        [ElasticProperty(Name = "Id", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string Id { get; set; }

        /// <summary>
        /// 主体的uid
        /// </summary>
        [ElasticProperty(Name = "uid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string uid { get; set; }

        [ElasticProperty(Name = "mid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string mid { get; set; }
        /// <summary>
        /// 来源
        /// </summary>
        [ElasticProperty(Name = "from_id", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string from_id { get; set; }

        [ElasticProperty(Name = "bizid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string bizid { get; set; }

        [ElasticProperty(Name = "biztype", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int biztype { get; set; }

        [ElasticProperty(Name = "extralid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string extralid { get; set; }

        [ElasticProperty(Name = "isread", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int isread { get; set; }

        [ElasticProperty(Name = "timestamp", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int timestamp { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }

    public enum EComBizType
    {
        /// <summary>
        /// 美美社区文章点赞
        /// </summary>
        Favour = 1,
        /// <summary>
        /// 美美社区用户关注
        /// </summary>
        Subscribe = 2,
        /// <summary>
        /// 帖子的评论
        /// </summary>
        Comment = 3,
        /// <summary>
        /// 评论的回复
        /// </summary>
        Reply = 4,
        /// <summary>
        /// 对发现美文章点赞
        /// </summary>
        NoticBoardFavour=5

    }
}
