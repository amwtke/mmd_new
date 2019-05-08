using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Code
{
    [Serializable]
    [Table("Code_MStatus")]
    public class CodeMerchantStatus
    {
        [Key]
        public int code { get; set; }
        public string description { get; set; }
    }

    public enum ECodeMerchantStatus
    {
        待审核=0,
        已开通未配置=1,
        未通过=2,
        已删除=3,
        已配置=4,
        审核中=5
    }
}
