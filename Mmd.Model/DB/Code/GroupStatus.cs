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
    [Table("Code_Group_Status")]
    public class CodeGroupStatus//商家发布团的状态：已发布，已删除，待发布
    {
        [Key]
        public int code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }

    public enum EGroupStatus
    {
        已发布 = 0,
        已删除 = 1,
        待发布 = 2,
        已过期 = 3,
        已结束 = 4,
    }

    public enum EGroupLuckyStatus
    {
        待开奖 = 0,
        已开奖 = 1
    }

    public enum EGroupWaytoget
    {
        自提 = 0,
        快递到家 = 1,
        自提或快递到家 = 2
    }
}
