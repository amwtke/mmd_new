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
    [Table("Code_Post_Company")]
    public class CodeExpress//快递公司
    {
        [Key]
        public string code { get; set; }
        public string value { get; set; }
        public string tel { get; set; }
    }
}
