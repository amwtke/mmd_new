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
    [Table("Code_AuditPeriod")]
    public class CodeAuditPeriod// 审计周期，格式是 天/时/分/秒/消费个
    {
        [Key]
        public int? code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }
}
