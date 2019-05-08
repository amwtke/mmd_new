using MD.Model.DB.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB
{
    [Serializable]
    [Table("NoticeBoard")]
    public class NoticeBoard
    {
        public NoticeBoard()
        {
            hits_count = 1000;
            praise_count = 0;
            transmit_count = 0;
        }
        [Key]
        public Guid nid { get; set; }
        public Guid mid { get; set; }
        [Display(Name = "文章标题")]
        [Required(ErrorMessage = "  必填！")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "请输入1到20个字！")]
        public string title { get; set; }
        [Display(Name = "来源")]
        [Required(ErrorMessage = "  必填！")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "请输入1到10个字！")]
        public string source { get; set; }

        public int notice_category { get; set; }

        public string tag_1 { get; set; }
        public string tag_2 { get; set; }
        public string tag_3 { get; set; }
        [Required(ErrorMessage = "  请上传图片！")]
        public string thumb_pic { get; set; }
        public string description { get; set; }
        public int status { get; set; }
        public double timestamp { get; set; }
        /// <summary>
        /// 点击量、阅读量
        /// </summary>
        public int hits_count { get; set; }
        /// <summary>
        /// 点赞量
        /// </summary>
        public int praise_count { get; set; }
        /// <summary>
        /// 转发量
        /// </summary>
        public int transmit_count { get; set; }
        public string extend_1 { get; set; }
        public string extend_2 { get; set; }
        public string extend_3 { get; set; }
        [ForeignKey("status")]
        public virtual CodeNoticeBoardStatus statusid { get; set; }
    }
}
