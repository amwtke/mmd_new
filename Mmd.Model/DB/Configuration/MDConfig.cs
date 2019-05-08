using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB
{
    [Table("MD_Configuration")]
    public class MDConfigItem
    {
        [Key]
        public int Id { get; set; }
        public string Domain { get; set; }

        public string Module { get; set; }
        public string Function { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public double TimeStamp { get; set; }
    }
}
