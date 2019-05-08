using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Professional
{
    [Serializable]
    [Table("sta_user")]
    public class sta_user
    {
        [Key]
        public Guid uid { get; set; }
        public string loginname { get; set; }
        public string pwd { get; set; }
        public string nickname { get; set; }
        public Guid mid { get; set; }
        public string tel { get; set; }
        public string headerurl { get; set; }
        public double register_date { get; set; }
        public int is_valid { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        [NotMapped]
        public double expi_time { get; set; }
    }
}
