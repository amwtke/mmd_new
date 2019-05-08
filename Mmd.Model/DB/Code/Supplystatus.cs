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
    [Table("code_supplystatus")]
  public  class Supplystatus
    {
        [Key]
        public int code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }
}
