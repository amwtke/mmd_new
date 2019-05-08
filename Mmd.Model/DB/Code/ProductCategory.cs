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
    [Table("Code_Catalogue")]
    public class CodeProductCategory//商品分类
    {
        [Key]
        public int code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
        public int? sortid { get; set; }
    }
}
