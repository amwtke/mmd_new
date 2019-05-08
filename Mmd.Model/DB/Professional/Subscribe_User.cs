using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MD.Model.DB.Professional
{
    [Serializable]
    [Table("subscribe_user")]
    public class Subscribe_User
    {
        [Key]
        public string openid { get; set; }
        public string nickname { get; set; }
        public int sex { get; set; }
        public string language { get; set; }
        public string city { get; set; }
        public string province { get; set; }
        public string country { get; set; }
        public string headimgurl { get; set; }
        public long subscribe_time { get; set; }
        public string unionid { get; set; }
        public string remark { get; set; }
        public string groupid { get; set; }
        public int subscribe { get; set; }
    }
}
