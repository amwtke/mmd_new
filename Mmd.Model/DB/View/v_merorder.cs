using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.View
{
    [Serializable]
    [Table("v_merorder")]
   public class v_merorder
    {
        [Key]
        public Guid mid { get; set; }
        public string merchant_name { get; set; }
        public Guid pid { get; set; }
        public string product_name { get; set; }
        public Guid gid { get; set; }
        public double last_update_time { get; set; }
        public int product_quota { get; set; }
        public int group_price { get; set; }
        public int person_quota { get; set; }
        public int userobot { get; set; }
        public Guid goid { get; set; }
        public int gostatus { get; set; }
        public Guid oid { get; set; }
        public int order_price { get; set; }
        public int orderstatus { get; set; }

    }
}
