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
    [Table("Product")]
    public class Product
    {
        [Key]
        public long? p_no { get; set; }
        public Guid pid { get; set; }
        [Required(ErrorMessage = "  必填!")]
        public string name { get; set; }
        [Required(ErrorMessage = "  必填！")]
        public string description { get; set; }
        [Required(ErrorMessage = "  必填！")]
        public int? price { get; set; }
        public Guid mid { get; set; }
        [ForeignKey("category")]
        public virtual CodeProductCategory categoryid { get; set; }
        public int? category { get; set; }//商品分类码,跟商家有关,商家可以自定义
        public Guid aaid { get; set; }
        [Required(ErrorMessage = "  必填！")]
        public string standard { get; set; }//商品的规格,如1包10片等，1瓶10毫升,是用户自己输入
        public Guid last_update_user { get; set; }
        public double? timestamp { get; set; }
        [Required(ErrorMessage = "  请上传图片！")]
        public string advertise_pic_1 { get; set; }
        [Required(ErrorMessage = "  请上传图片！")]
        public string advertise_pic_2 { get; set; }
        [Required(ErrorMessage ="  请上传图片！")]
        public string advertise_pic_3 { get; set; }

        public int? status { get; set; }
        public double? avgScore { get; set; }
        public int? grassCount { get; set; }
        public int? scorePeopleCount { get; set; }
        [ForeignKey("status")]
        public virtual CodeProductStatus statusid { get; set; }
    }
}
