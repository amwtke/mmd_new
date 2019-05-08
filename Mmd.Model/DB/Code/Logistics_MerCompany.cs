using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MD.Model.DB.Code
{
    public class Logistics_MerCompany
    {
        [Key, Column(Order = 0)]
        public Guid mid { get; set; }
        [Key, Column(Order = 1)]
        public string companyCode { get; set; }
        public string companyName { get; set; }
        public int orderId { get; set; }
        public int isDefault { get; set; }
        public double createtime { get; set; }
    }
}
