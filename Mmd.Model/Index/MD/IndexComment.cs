using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "comment")]
    public class IndexComment
    {
        /// <summary>
        /// 主键
        /// </summary>
        [ElasticProperty(Name = "Id", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string Id { get; set; }

        /// <summary>
        /// 主题ID
        /// </summary>
        [ElasticProperty(Name = "topic_id", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string topic_id { get; set; }

        /// <summary>
        /// 主题type，来源于ECommunityTopicType
        /// </summary>
        [ElasticProperty(Name = "topic_type", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int topic_type { get; set; }

        /// <summary>
        /// 评论内容
        /// </summary>
        [ElasticProperty(Name = "content", Type = FieldType.String, Index = FieldIndexOption.Analyzed, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string content { get; set; }

        /// <summary>
        /// 评论用户id
        /// </summary>
        [ElasticProperty(Name = "from_uid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string from_uid { get; set; }

        /// <summary>
        /// 评论用户来源商家
        /// </summary>
        [ElasticProperty(Name = "from_mid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string from_mid { get; set; }

        /// <summary>
        /// 评论时间
        /// </summary>
        [ElasticProperty(Name = "timestamp", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int timestamp { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [ElasticProperty(Name = "status", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int status { get; set; }

        /// <summary>
        /// 回复内容list
        /// </summary>
        [ElasticProperty(Name = "com_replys", Type = FieldType.Nested, Index = FieldIndexOption.NotAnalyzed)]
        public List<IndexCom_Reply> Com_Replys { get; set; }

        /// <summary>
        /// 昵称列表
        /// </summary>
        [ElasticProperty(Name = "dic_nickname", Type = FieldType.Object, Index = FieldIndexOption.NotAnalyzed)]
        public Dictionary<string,string> dic_nickname { get; set; }

        /// <summary>
        /// 头像列表
        /// </summary>
        [ElasticProperty(Name = "dic_headerpic", Type = FieldType.Object, Index = FieldIndexOption.NotAnalyzed)]
        public Dictionary<string, string> dic_headerpic { get; set; }


        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }


    }
}
