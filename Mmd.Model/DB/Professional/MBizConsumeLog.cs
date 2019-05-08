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
    [Table("MBizConsumeLog")]
    public class MBizConsumeLog
    {
        [Key]
        public long id { get; set; }

        public Guid mid { get; set; }

        public string biz_type { get; set; }

        public int count { get; set; }

        public Guid source { get; set; }

        public double timestamp { get; set; }

        public string extension_1 { get; set; }
        public string extension_2 { get; set; }
        public string extension_3 { get; set; }

    }
}
