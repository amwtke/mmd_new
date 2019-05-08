using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB
{
    [Serializable]
    [Table("PriceMatrix")]
    public class PriceMatrix//团购的价格矩阵
    {
        [Key]
        public Guid gid { get; set; }
        public int member_quota { get; set; }//参团人数配额
        public decimal price { get; set; }
        public string description { get; set; }
        public Guid last_update_user { get; set; }
        public double timestamp { get; set; }
    }
}
