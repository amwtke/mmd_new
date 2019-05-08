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
    [Table("code_group_types")]
    public class CodeGroupType
    {
        [Key]
        public int code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }
    public enum EGroupTypes
    {
        普通团 = 0,
        抽奖团 = 1
    }
}
