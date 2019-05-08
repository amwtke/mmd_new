using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MD.Model.DB.Code
{
    [Serializable]
    [Table("logistics_templateitem")]
    public class Logistics_TemplateItem
    {
        [Key]
        public Guid id { get; set; }
        public Guid ltid { get; set; }
        public int first_amount { get; set; }
        public int first_fee { get; set; }
        public int additional_amount { get; set; }
        public int additional_fee { get; set; }
        public string regions { get; set; }
        public double createtime { get; set; }
        public double lastupdatetime { get; set; }
    }
}
