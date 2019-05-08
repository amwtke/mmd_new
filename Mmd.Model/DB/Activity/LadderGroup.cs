using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Activity
{
    [Serializable]
    [Table("ladder_group")]
    public class LadderGroup
    {
        [Key]
        public Guid gid { get; set; }
        public Guid mid { get; set; }
        public Guid pid { get; set; }
        public string pno { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string pic { get; set; }
        public int? waytoget { get; set; }
        public int product_count { get; set; }
        public int product_quotacount { get; set; }
        public int origin_price { get; set; }
        public int status { get; set; }
        public double start_time { get; set; }
        public double end_time { get; set; }
        public double last_update_time { get; set; }
        [NotMapped]
        public List<LadderPrice> PriceList { get; set; }
    }
}
