using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB.Code;

namespace MD.Model.DB.Professional
{
    [Serializable]
    [Table("group_media")]
    public class Group_Media
    {
        [Key]
        public Guid gid { get; set; }
        public Guid mid { get; set; }
        public string pic { get; set; }
        public string media_id { get; set; }
        public double createtime { get; set; }
    }
}
