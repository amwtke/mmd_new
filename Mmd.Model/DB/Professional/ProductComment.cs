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
    [Table("product_comment")]
    public class ProductComment
    {
        [Key]
        public Guid pcid { get; set; }
        public Guid pid { get; set; }
        public Guid uid { get; set; }
        public Guid mid { get; set; }
        public int? u_age { get; set; }
        public int? u_skin { get; set; }
        public int? score { get; set; }
        /// <summary>
        /// 评论内容
        /// </summary>
        public string comment { get; set; }
        /// <summary>
        /// 评论回复
        /// </summary>
       public string comment_reply { get; set; }
        /// <summary>
        /// 嗮图链接，|符号分割
        /// </summary>
        public string imglist { get; set; }

        /// <summary>
        /// 是否加精
        /// </summary>
        public int? isessence { get; set; }

        /// <summary>
        /// 点赞数
        /// </summary>
        public int? praise_count { get; set; }
        /// <summary>
        /// 评论时间
        /// </summary>
        public double? timestamp { get; set; }
        /// <summary>
        /// 回复时间
        /// </summary>
        public double? timestamp_reply { get; set; }
    }
}
