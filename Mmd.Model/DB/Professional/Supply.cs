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
    [Table("supply")]
    public class Supply
    {
        [Key]
        public Guid sid { get; set; }
        public long? s_no { get; set; }
        [Display(Name ="商品名称")]
        [Required(ErrorMessage = "  必填!")]
        public string name { get; set; }
        public int? category { get; set; }
        public int? brand { get; set; }
        [Required(ErrorMessage = "  必填!")]
        [RegularExpression(@"^[0-9]+([.][0-9]+){0,1}$", ErrorMessage = "金额格式错误")]
        public int? market_price { get; set; }
        [Required(ErrorMessage = "  必填!")]
        [RegularExpression(@"^[0-9]+([.][0-9]+){0,1}$", ErrorMessage = "金额格式错误")]
        public int? supply_price { get; set; }
        [Required(ErrorMessage = "  必填!")]
        [RegularExpression(@"^[0-9]+([.][0-9]+){0,1}$", ErrorMessage = "金额格式错误")]
        public int? group_price { get; set; }
        [Required(ErrorMessage = "  必填!")]
        public string standard { get; set; }
        [Required(ErrorMessage = "  必填!")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "请输入正整数")]
        [Range(1,int.MaxValue, ErrorMessage= "不能为0")]
        public int? quota_min { get; set; }
        [Required(ErrorMessage = "  必填!")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "请输入正整数")]
        [Range(1, int.MaxValue,ErrorMessage ="必须大于最小购买限制")]
        public int? quota_max { get; set; }
        [Required(ErrorMessage = "  必填!")]
        public string pack { get; set; }
        [Required(ErrorMessage = "  请上传图片！")]
        public string advertise_pic_1 { get; set; }
        [Required(ErrorMessage = "  请上传图片！")]
        public string advertise_pic_2 { get; set; }
        [Required(ErrorMessage = "  请上传图片！")]
        public string advertise_pic_3 { get; set; }
        [Required(ErrorMessage = "  必填!")]
        public string description { get; set; }
        public double? timestamp { get; set; }
        public int? status { get; set; }
        public string headpic_dir { get; set; }
    }
}
