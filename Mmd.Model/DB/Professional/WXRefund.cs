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
    /*退款表
    包括退款申请与退款查询。
    退款查询可以返回退款状态与退款到的卡号信息。*/
    [Table("WXRefund")]
    public class WXRefund
    {
        [Key]
        public Guid id { get; set; }
        public string out_trade_no { get; set; }
        public string transaction_id { get; set; }
        public string out_refund_no { get; set; }
        public string appid { get; set; }
        public string mch_id { get; set; }
        public int? total_fee { get; set; }
        public int? refund_fee { get; set; }
        public string op_user_id { get; set; }//操作员,与MerId一致
        public double init_time { get; set; }//发起时间
        public string return_code { get; set; }
        public string return_msg { get; set; }
        public string result_code { get; set; }
        public string err_code { get; set; }
        public string err_code_des { get; set; }
        public string refund_id { get; set; }//微信退款单号。微信退款单号。String(28).return code==sucess的时候有。
        public int? cash_fee { get; set; }
        public string init_response_xml { get; set; }//申请退款发起后返回的xml文档。
        public string query_response_xml { get; set; }//查询退款进度返回的xml
        public int? refund_count { get; set; }//退款笔数
        /*退款状态：
        SUCCESS—退款成功
        FAIL—退款失败
        PROCESSING—退款处理中
        NOTSURE—未确定，需要商户原退款单号重新发起
        CHANGE—转入代发，退款到银行发现用户的卡作废或者冻结了，
            导致原路退款银行卡失败，资金回流到商户的现金帐号，需要商户人工干预，
            通过线下或者财付通转账的方式进行退款。*/
        public string refund_status { get; set; }
        /*如：招商银行信用卡0403

            取当前退款单的退款入账方
                1）退回银行卡：
                    {银行名称}{卡类型}{卡尾号}
                2）退回支付用户零钱:
                    支付用户零钱*/
        public string refund_recv_accout { get; set; }

        /// <summary>
        /// 1:系统自动退款,2:后台手动退款
        /// </summary>
        public int? operation_flag { get; set; }
    }
}
