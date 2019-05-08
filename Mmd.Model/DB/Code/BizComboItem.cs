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
    [Table("Code_Biz_TaocanItem")]
   public class CodeBizTaocanItem
    {
        /// <summary>
        /// 套餐类型
        /// </summary>
        [Key, Column(Order = 0)]
        public string tc_type { get; set; }
        /// <summary>
        /// 套餐条目类型
        /// </summary>
        [Key, Column(Order = 1)]
        public string biz_type { get; set; }
        /// <summary>
        /// 业务单位数量
        /// </summary>
        public int? biz_unit_count { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string description { get; set; }

        //[ForeignKey("biz_type")]
        //public virtual CodeBizType BizType { get; set; }
    }
}
