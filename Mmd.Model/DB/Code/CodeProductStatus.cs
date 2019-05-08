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
    [Table("Code_ProductStatus")]
    public class CodeProductStatus
    {
        [Key]
        public int code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }

    public enum EProductStatus
    {
        已添加=0,
        已删除=1
    }
}
