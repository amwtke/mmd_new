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
    [Table("logistics_template")]
    public class Logistics_Template
    {
        [Key]
        public Guid ltid { get; set; }
        public Guid mid { get; set; }
        public string name { get; set; }
        public double createtime { get; set; }
        public double lastupdatetime { get; set; }
        [NotMapped]
        public List<Logistics_TemplateItem> items { get; set; }
    }
}
