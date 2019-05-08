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
    [Table("MOrder")]
    public class MOrder//商铺充值订单
    {
        [Key]
        public Guid moid { get; set; }
        public Guid mid { get; set; }
        public string tc_type { get; set; }
        public int? tc_price { get; set; }
        public int? cash { get; set; }//实际支付的现金
        public int? pay_type { get; set; }//微信支付或者网银支付
        public string pay_transactionid { get; set; }//支付订单的id,如果是微信支付则是transactionid

        /// <summary>
        /// 产生订单的时间
        /// </summary>
        public double? timestamp { get; set; }

        public int? status { get; set; }

        public double? pay_time { get; set; }
        /// <summary>
        /// 购买了套餐多少份
        /// </summary>

        public int? buy_tc_shares { get; set; }

        [ForeignKey("tc_type")]
        public virtual CodeBizTaocan TaoCanType { get; set; }

        //[ForeignKey("pay_type")]
        //public virtual CodeMerPayType MpayType { get; set; }

        //[ForeignKey("status")]
        //public virtual CodeMorderStatus statusid { get; set; }
    }
}
