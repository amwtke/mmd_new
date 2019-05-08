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
    [Table("code_skin")]
    public class CodeSkin
    {
        [Key]
        public int code { get; set; }
        public string skin { get; set; }
    }
}
