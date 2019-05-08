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
    [Table("M_Biz")]
    public class MBiz//商铺开通的业务表
    {
        [Key,Column(Order = 0)]
        public Guid mid { get; set; }
        [Key, Column(Order = 1)]
        public string biz_type { get; set; }
        public int? quota_remain { get; set; }
        public int? audit_period { get; set; }
        public double? last_audit_time { get; set; }//上次审计时间
        public double? last_add_time { get; set; }//上次充值时间
        public bool? isvalid { get; set; }
        //[ForeignKey("audit_period")]
        //public virtual CodeAuditPeriod AuditPeriod{get;set;}
        //[ForeignKey("biz_type")]
        //public virtual CodeBizType BizType { get; set; }
    }

    public enum EBizType
    {
        DD=0,
    }
}
