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
    [Table("AttValue")]
    public class AttValue
    {
        [Key, Column(Order = 0)]
        public Guid owner { get; set; }

        [Key, Column(Order = 1)]
        public Guid attid { get; set; }

        public string value { get; set; }
        public double timestamp { get; set; }
    }
}
