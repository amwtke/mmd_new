using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB
{
    [Serializable]
    [Table("M_Photos")]
    public class MerPhotos//商家的相册,阿里或者七牛存储
    {
        public Guid mid { get; set; }
        public string path { get; set; }
        public Guid last_update_user { get; set; }
        public double timestamp { get; set; }
    }
}
