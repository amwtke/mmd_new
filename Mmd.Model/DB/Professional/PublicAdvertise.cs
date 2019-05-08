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
    [Table("PublicAdvertise")]
    public class PublicAdvertise//美哒自己的宣传
    {
        [Key]
        public Guid id { get; set; }
        public Guid aaid { get; set; }
        public string title { get; set; }
        public string content { get; set; }
    }
}
