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
    [Table("Order")]
    public class Order//个人订单,如果选择自提，则mid就是自提点
    {
        [Key]
        public Guid oid { get; set; }
        //数字+字母.
        //年月日时分秒+16位随机数
        //2016 01 01--01 01 01-12312432534546.共30位。
        public string o_no { get; set; }
        public Guid mid { get; set; }
        public Guid goid { get; set; }
        public Guid gid { get; set; }

        public int? waytoget { get; set; }//提货方式,从Group属性继承
        public double? paytime { get; set; }//支付时间,可以是形成订单的时间
        public int? order_price { get; set; }
        /// <summary>
        /// 实际付款额,如果有优惠券可能会变
        /// </summary>
        public int? actual_pay { get; set; }
        /// <summary>
        /// 邮费
        /// </summary>
        public int? post_price { get; set; }
        public int? status { get; set; }
        public Guid buyer { get; set; }//发起者id
        public Guid upid { get; set; }//用户的取货地址列表
        public string post_company { get; set; }
        public string post_number { get; set; }//运单号
        public Guid default_writeoff_point { get; set; }//提货点
        public Guid writeoffer { get; set; }//核销员uuid
        public double? writeoffday { get; set; }//核销时间
        public string extral_info { get; set; }//备注信息
        /// <summary>
        /// 购买人姓名
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 购买人电话
        /// </summary>
        public string cellphone { get; set; }
        /// <summary>
        /// 邮寄地址(快递到家时存储)
        /// </summary>
        public string postaddress { get; set; }
        /// <summary>
        /// 发货时间
        /// </summary>
        public double? shipmenttime { get; set; }
        /// <summary>
        /// 中奖状态（0未中奖，1中奖）
        /// </summary>
        [NotMapped]
        public int? luckyStatus { get; set; }

        //[ForeignKey("status")]
        //public virtual CodeOrderStatus StatusId { get; set; }

        //[ForeignKey("waytoget")]
        //public virtual CodeWayToGet WayToGetId { get; set; }
    }
}
