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
    [Table("logistics_company")]
    public class Logistics_Company
    {
        [Key]
        public Guid lcid { get; set; }
        public string companyCode { get; set; }
        public string companyName { get; set; }
        public int orderId { get; set; }
        public int isDelete { get; set; }
        public double createtime { get; set; }
       
    }
}
