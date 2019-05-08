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
    [Table("Code_NoticeBoardStatus")]
    public class CodeNoticeBoardStatus
    {
        [Key]
        public int code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }
    public enum ENoticeBoardStatus
    {
        待发布 = 0,
        已发布 = 1,
        已删除 = 2
    }
}
