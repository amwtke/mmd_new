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
    [Table("Code_M_PayType")]
    public class CodeMerPayType//商户充值的来源。WX 微信支付。
    {
        [Key]
        public int? code { get; set; }

        public string value { get; set; }
        public string description { get; set; }
    }

    public enum EMPayType
    {
        微信支付=0,
        转账=1,
        银联支付=2,
        其他=99
    }
}
