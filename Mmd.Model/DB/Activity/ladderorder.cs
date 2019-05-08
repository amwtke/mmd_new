using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Activity
{
    [Serializable]
    [Table("ladder_order")]
    public class LadderOrder
    {
        [Key]
        public Guid oid { get; set; }
        public string o_no { get; set; }
        public Guid mid { get; set; }
        public Guid goid { get; set; }
        public Guid gid { get; set; }
        public int waytoget { get; set; }//提货方式,从Group属性继承
        public double paytime { get; set; }//支付时间,可以是形成订单的时间
        public int order_price { get; set; }
        public int status { get; set; }
        public Guid buyer { get; set; }//发起者id
        public Guid default_writeoff_point { get; set; }//提货点
        public Guid writeoffer { get; set; }//核销员uuid
        public double? writeoffday { get; set; }//核销时间
        public Guid writeoff_point { get; set; }//核销地址
    }
}
