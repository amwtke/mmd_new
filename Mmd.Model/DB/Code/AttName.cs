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
    [Table("AttName")]
    public class AttName
    {
        [Key]
        public Guid attid { get; set; }
        public string table_name { get; set; }
        public string att_name { get; set; }
        public string unit { get; set; }
        public string description { get; set; }
    }
}
