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
    [Table("User_WriteOff")]
    public class User_WriteOff
    {
        [Key]
        public Guid uw_id { get; set; }
        public Guid uid { get; set; }
        public Guid mid { get; set; }

        /// <summary>
        /// 提货点的uuid
        /// </summary>
        public Guid woid { get; set; }
        public bool? is_default { get; set; }
        public double? create_time { get; set; }

        public string user_name { get; set; }
        public string cellphone { get; set; }
    }
}
