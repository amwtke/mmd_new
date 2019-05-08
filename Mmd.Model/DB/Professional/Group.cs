using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB.Code;

namespace MD.Model.DB
{
    [Serializable]
    [Table("Group")]
    public class Group//开团信息主表.提货方式在商家定义活动的时候就指定。
    {
        [Key]
        public Guid gid { get; set; }//团的uuid
        public Guid mid { get; set; }//商铺号
        public Guid aaid { get; set; }//团购的宣传图文
        public Guid pid { get; set; }//商品编号
        public string group_headpic_dir { get; set; }//团购活动的主题图片

        [Display(Name = "团标题")]
        [Required(ErrorMessage = "  必填！")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "请输入3到40个字！")]
        public string title { get; set; }

        [Display(Name = "团描述")]
        [Required(ErrorMessage = "  必填！")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "请输入10到80个字！")]
        public string description { get; set; }

        /// <summary>
        /// 商品剩余库存
        /// </summary>
        [Display(Name = "团商品总额")]
        [Required(ErrorMessage = "  必填！")]
        [RegularExpression(@"^[0-9]\d*$", ErrorMessage = "必须录入正整数！")]
        public int? product_quota { get; set; }

        public int? product_setting_count { get; set; }

        /// <summary>
        /// 参团人数
        /// </summary>
        [Display(Name = "团人数限制")]
        [Required(ErrorMessage = "  必填！")]
        [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "必须录入正整数！")]
        [Range(2, int.MaxValue, ErrorMessage = "拼团人数大于1！")]
        //[Range(1, int.MaxValue,ErrorMessage = "数据格式不正确！")]
        public int? person_quota { get; set; }
        /// <summary>
        /// 从发布起，团持续的时间 单位秒
        /// </summary>
        public double? time_limit { get; set; }//
        public int? waytoget { get; set; }//取货方式

        public Guid ltid { get; set; } //运费模板id
        /// <summary>
        /// 商品详情中是否显示正在拼团中列表
        /// </summary>
        public int? isshowpting { get; set; }

        /// <summary>
        /// 原价:单位分
        /// </summary>
        public int? origin_price { get; set; }

        /// <summary>
        /// 团购价:单位分
        /// </summary>
        public int? group_price { get; set; }

        public int? status { get; set; }//商家发布团的状态
        public string biz_type { get; set; }//业务类型，跟商铺的权限相关,关联m_biz表
        public double? last_update_time { get; set; }
        public Guid last_update_user { get; set; }

        public string advertise_pic_url { get; set; }

        public double? group_start_time { get; set; }

        public double? group_end_time { get; set; }

        //分销佣金金额：单位分
        public int Commission { get; set; }
        /// <summary>
        /// 团长再减
        /// </summary>
        [NotMapped]
        public int? leader_price { get; set; }

        /// <summary>
        /// 是否使用机器人
        /// </summary>
        [NotMapped]
        public int? userobot { get; set; }

        /// <summary>
        /// 该商家下的所有门店
        /// </summary>
        [NotMapped]
        public List<WriteOffPoint> WriteOffPoints { get; set; }
        /// <summary>
        /// 活动门店（用,拼接）
        /// </summary>
        [NotMapped]
        public string activity_point { get; set; }

        /// <summary>
        /// 限制购买次数
        /// </summary>
        [NotMapped]
        public int? order_limit { get; set; }

        /// <summary>
        /// 团类型 0：普通团，1抽奖团
        /// </summary>
        public int? group_type { get; set; }

        /// <summary>
        /// 中奖人数
        /// </summary>
        [NotMapped]
        public int? lucky_count { get; set; }
        /// <summary>
        /// 抽奖团状态0：待开奖1：已开奖
        /// </summary>
        [NotMapped]
        public int? lucky_status { get; set; }
        /// <summary>
        /// 抽奖团结束日期
        /// </summary>
        [NotMapped]
        public string lucky_endTime { get; set; }

        [ForeignKey("biz_type")]
        public virtual CodeBizType BizType { get; set; }
        [ForeignKey("status")]
        public virtual CodeGroupStatus GroupStatus { get; set; }
        [ForeignKey("waytoget")]
        public virtual CodeWayToGet WTGId { get; set; }
    }
}
