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
    [Table("User_Post")]
    public class UserPost//用户在每个商铺的收货地址
    {
        [Key]
        public Guid upid { get; set; }
        public Guid uid { get; set; }
        public string name { get; set; }
        public string cellphone { get; set; }
        public string province { get; set; }
        public string city { get; set; }
        public string district { get; set; }
        public string code { get; set; }
        public string address { get; set; }
        public double createtime { get; set; }
        public bool is_default { get; set; }//是否是默认的收货地址
        public bool isdelete { get; set; }//是否删除
    }
}
