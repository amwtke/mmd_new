using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Code
{
    [Table("Code_Morder_status")]
    public class CodeMorderStatus
    {
        [Key]
        public int? code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }

    public enum EMorderStatus
    {
        未支付=0,
        已生效=1,
        已撤销=2
    }
}
