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
    [Table("ladder_price")]
    public class LadderPrice
    {
        [Key]
        public Guid lpid { get; set; }
        public Guid gid { get; set; }
        public int person_count { get; set; }
        public int group_price { get; set; }
    }
}
