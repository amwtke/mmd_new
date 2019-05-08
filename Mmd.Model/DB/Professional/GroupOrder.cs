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
    [Table("GroupOrder")]
    public class GroupOrder//团购的订单,类似于团生成的实例
    {
        [Key]
        public Guid goid { get; set; }
        public string go_no { get; set; }//数字代码,便于记忆,可以根据时间来生成
        public Guid gid { get; set; }
        public Guid pid { get; set; }
        public Guid leader { get; set; }
        public int? user_left { get; set; }//还剩几个
        public double? expire_date { get; set; }
        public int? price { get; set; }//原价:单位分
        /// <summary>
        /// 团购价
        /// </summary>
        public int? go_price { get; set; }
        public int? status { get; set; }//团购订单的状态：团购失败 团购进行中 团购成功
        public double? create_date { get; set; }//组团时间

        //[ForeignKey("status")]
        //public virtual CodeGroupOrderStatus StatusId { get; set; }
    }
}
