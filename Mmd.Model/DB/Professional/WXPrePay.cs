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
    [Table("WXPrePay")]
    public class WXPrePay//微信支付获取prepay_code的通信记录
    {
        [Key]
        public Guid id { get; set; }
        public string appid { get; set; }
        public string mch_id { get; set; }//微信支付分配的商户号
        public string out_trade_no { get; set; }
        public int total_fee { get; set; }
        public string trade_type { get; set; }
        public string notify_url { get; set; }//接收微信支付异步通知回调地址，通知url必须为直接可访问的url，不能携带参数
        public string spbill_create_ip { get; set; }//APP和网页支付提交用户端ip，Native支付填调用微信支付API的机器IP
        public string body { get; set; }//商品或支付单简要描述.String(128)
        public string return_code { get; set; }//SUCCESS/FAIL.此字段是通信标识，非交易标识，交易是否成功需要查看result_code来判断.String(16)
        public string result_code { get; set; }//SUCCESS/FAIL.String(16)
        public string return_msg { get; set; }//return_code为fail时返回错误原因
        public string err_code { get; set; }
        public string err_code_des { get; set; }
        public string code_url { get; set; }//trade_type为NATIVE时有返回，可将该参数值生成二维码展示出来进行扫码支付
        public string prepay_id { get; set; }//微信生成的预支付回话标识，用于后续接口调用中使用，该值有效期为2小时.String(64)
        public string request_xml { get; set; }
        public string response_xml { get; set; }
        public double timestamp { get; set; }
    }
}
