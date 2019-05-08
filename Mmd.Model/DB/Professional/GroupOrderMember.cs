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
    [Table("GroupOrderMember")]
    public class GroupOrderMember//团订单成员表信息
    {
        [Key]
        public Guid goid { get; set; }
        public Guid userid { get; set; }
    }
}
