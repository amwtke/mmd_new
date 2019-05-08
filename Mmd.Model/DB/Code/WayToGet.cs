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
    /*提货方式。商家发布活动时设置。*/
    [Table("Code_WayToGet")]
    public class CodeWayToGet
    {
        [Key]
        public int code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }

    public enum EWayToGet
    {
        自提=0,
        物流=1
    }
}
