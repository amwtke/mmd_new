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
    [Table("logistics_region")]
    public class Logistics_Region
    {
        [Key]
        public int lid { get; set; }
        public string name { get; set; }
        public int orderId { get; set; }
        public int categoryLevel { get; set; }
        public int fatherId { get; set; }
        public string code { get; set; }
        public int isDeleted { get; set; }
    }
}
