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
    /* 团购订单的状态：团购失败
     * 团购进行中
     * 团购成功*/
    [Table("Code_GroupOrder_Status")]
    public class CodeGroupOrderStatus
    {
        [Key]
        public int? code { get; set; }
        public string value { get; set; }
        public string description { get; set; }

    }

    public enum EGroupOrderStatus
    {
        拼团进行中 = 0,
        拼团成功=1,
        拼团失败=2,
        /// <summary>
        /// 未付款
        /// </summary>
        开团中=3,
    }
}
