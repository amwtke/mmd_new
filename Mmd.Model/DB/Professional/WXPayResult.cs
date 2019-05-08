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
    /*支付成功与否的结果,可能来自微信的通知，也可能是自己调的查询接口*/
    [Table("WXPayResult")]
    public class WXPayResult
    {
        [Key]
        public Guid id { get; set; }
        public string out_trade_no { get; set; }//mmd的订单号。yyyymmddhhmmss+16随机数
        public string return_code { get; set; }//SUCCESS/FAIL此字段是通信标识，非交易标识，交易是否成功需要查看trade_state来判断
        public string return_msg { get; set; }//return_code为fail的时候返回
        public string result_code { get; set; }//SUCCESS/FAIL.String(16)
        public string err_code { get; set; }
        public string err_code_des { get; set; }
        public string appid { get; set; }//微信分配的公众账号ID（企业号corpid即为此appId）String(32)
        public string mch_id { get; set; }
        public string openid { get; set; }//（支付结果通知时有）用户在商户appid下的唯一标识String(128)
        public string trade_type { get; set; }
        public int? bank_type { get; set; }//订单总金额，单位为分
        public int? total_fee { get; set; }
        public int? cash_fee { get; set; }//现金支付金额订单现金支付金额，详见支付金额
        public string transaction_id { get; set; }//微信支付id
        public string time_end { get; set; }
        public string notify_xml { get; set; }
        public string query_result_xml { get; set; }
        /*query的时候会有,支付状态:
            SUCCESS—支付成功
            REFUND—转入退款
            NOTPAY—未支付
            CLOSED—已关闭
            REVOKED—已撤销（刷卡支付）
            USERPAYING--用户支付中
            PAYERROR--支付失败(其他原因，如银行返回失败)*/
        public string trade_state { get; set; }
        public string trade_state_desc { get; set; }//对当前查询订单状态的描述和下一步操作的指引

        public double? timestamp { get; set; }
    }
}
