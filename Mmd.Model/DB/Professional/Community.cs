using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Professional
{
    [Serializable]
    [Table("community")]
    public class Community
    {
        [Key]
        public Guid cid { get; set; }
        /// <summary>
        /// 商家mid
        /// </summary>
        public Guid mid { get; set; }
        /// <summary>
        /// 用户uid
        /// </summary>
        public Guid uid { get; set; }
        /// <summary>
        /// 标题
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string content { get; set; }
        /// <summary>
        /// 图片列表，|符号分割
        /// </summary>
        public string imgs { get; set; }
        /// <summary>
        /// 点击量
        /// </summary>
        public int hits { get; set; }
        /// <summary>
        /// 点赞量
        /// </summary>
        public int praises { get; set; }
        /// <summary>
        /// 转发量
        /// </summary>
        public int transmits { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public int createtime { get; set; }
        /// <summary>
        /// 最后修改时间
        /// </summary>
        public int lastupdatetime { get; set; }
        /// <summary>
        /// 主题类型，来源于ECommunityTopicType
        /// </summary>
        public int topic_type { get; set; }

        /// <summary>
        /// 文章标签
        /// </summary>
        public int? flag { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public int status { get; set; }
    }

    /// <summary>
    /// 为了能复用评论模块，我们引入一个topic_type字段来区分主题的类别
    /// </summary>
    public enum ECommunityTopicType
    {
        /// <summary>
        /// 美美社区
        /// </summary>
        MMSQ = 0,
        /// <summary>
        /// 发现美文章
        /// </summary>
        NoticeBoard = 1
    }
    public enum ECommunityStatus
    {
        已发布 = 0,
        已删除 = 1
    }
    public enum ECommunityFlag
    {
        学化妆 = 1,
        嗮好货 = 2,
        自拍 = 3
    }
}
