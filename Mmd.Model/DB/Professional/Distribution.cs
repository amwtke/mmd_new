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
    [Table("distribution")]
    public class Distribution
    {
        [Key]
        public Guid oid { get; set; }
        public Guid mid { get; set; }
        public Guid gid { get; set; }
        /// <summary>
        /// 购买人
        /// </summary>
        public Guid buyer { get; set; }
        /// <summary>
        /// 分享人
        /// </summary>
        public Guid sharer { get; set; }
        /// <summary>
        /// 上一次的佣金总额
        /// </summary>
        public int last_commission { get; set; }
        /// <summary>
        /// 佣金金额
        /// </summary>
        public int commission { get; set; }
        /// <summary>
        /// 最后的佣金总额
        /// </summary>
        public int finally_commission { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public double? createtime { get; set; }
        /// <summary>
        /// 是否拼团成功，创建订单时为0，拼团成功后为1
        /// </summary>
        public int isptsucceed { get; set; }
        /// <summary>
        /// 最后修改时间
        /// </summary>
        public double lastupdatetime { get; set; }
        /// <summary>
        /// 来源类型0:订单佣金（+），1：佣金结算（-）
        /// </summary>
        public int sourcetype { get; set; }
        /// <summary>
        /// 备注,在订单手动退款扣除佣金时记录订单号
        /// </summary>
        public string remark { get; set; }
    }

    public enum EDisSourcetype
    {
        订单佣金=0,
        佣金结算=1
    }
}
