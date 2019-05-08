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
    [Table("Code_Biz_Taocan")]
    public class CodeBizTaocan//套餐
    {
        [Key]
        public string tc_type { get; set; }//套餐类型
        public string title { get; set; }
        public int? price { get; set; }
        public string description { get; set; }//套餐规则描述
        public Guid aaid { get; set; }//宣传html文档
        public bool? isvalid { get; set; }//套餐是否有效
        public double? unvalid_date { get; set; }//套餐失效日期
        /// <summary>
        /// 最多可以领取的次数。如果是0，代表无限次购买。
        /// </summary>
        public int? limit { get; set; }

        //[ForeignKey("tc_type")]
        //public virtual List<CodeBizTaocanItem> TaocanItem { get; set; }
    }

    public enum ECodeTaocanType
    {
        KTJS10,
        KTJS2000,
        CDD
    }

    public enum ECodeTaocanLimit
    {
        无限制=0,
    }
}
