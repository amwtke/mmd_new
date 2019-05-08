using MD.Model.DB.Code;
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
    [Table("NoticeReader")]
    public class NoticeReader
    {
        [Key, Column(Order = 0)]
        public Guid nid { get; set; }
        [Key, Column(Order = 1)]
        public Guid uid { get; set; }
        public string openid { get; set; }
        public double timestamp { get; set; }
        public string comment { get; set; }
        public string extend_1 { get; set; }
        public string extend_2 { get; set; }
        public string extend_3 { get; set; }
    }
}
