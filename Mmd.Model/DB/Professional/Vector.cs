using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Professional
{
    [Serializable]
    [Table("Vector")]
    public class Vector
    {
        [Key]
        public Guid vid { get; set; }
        public string type { get; set; }
        public string expression { get; set; }
        public double timestamp { get; set; }

        public bool visible { get; set; }

        public Guid owner { get; set; }
    }
}
