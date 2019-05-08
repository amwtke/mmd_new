using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Code
{
    [Serializable]
    /*团购失败已安排原路退款\退款成功\已支付，等待成团\已确认，待发货\配送中\已签收\已确认，待提货\已提货*/
    [Table("Code_Order_Status")]
    public class CodeOrderStatus
    {
        [Key]
        public int code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }
    /// <summary>
    /// 做显示用
    /// </summary>
    public enum EOrderStatusShow
    {
        退款中 = 0,
        已退款 = 1,
        已支付待成团 = 2,
        已成团未发货 = 3,
        已成团配送中 = 4,
        已成团待提货 = 5,
        已提货 = 6,
        未支付 = 7,
        退款失败 = 8,
        已发货 = 9
    }

    public enum EOrderStatus
    {
        退款中 = 0,
        已退款 = 1,
        已支付 = 2,
        已成团未发货 = 3,
        已成团配货中 = 4,
        已成团未提货 = 5,
        拼团成功 = 6,
        未支付 = 7,
        退款失败 = 8,
        已发货待收货 = 9
    }
    public enum EOrderLuckyStatus
    {
        已中奖 = 1,
        未中奖 = 0
    }
}
