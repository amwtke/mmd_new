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
    [Table("Code_BizType")]
    /*mb映射到其他类型业务的配比.
     * biztype：就是商铺参加拼团，生团所用到的mmd套餐。
     * 如基本套餐就是订单团，团是按照订单计费。那么biztype就是DD；
     * 如果是阶梯团，则会要求商铺购买相应的生团数量，如一个月10个，那么商铺在生团时，就会消耗到一个quota。*/
    public class CodeBizType
    {
        [Key]
        public string biz_type { get; set; }
        public string unit { get; set; }//计量单位，格式是 个/月/年
        public string description { get; set; }
        public bool? isvalid { get; set; }
        public double? unvalid_date { get; set; }

        public int? audit_period { get; set; }

        [ForeignKey("audit_period")]
        public virtual CodeAuditPeriod Codeperiod { get; set; }
    }
}
