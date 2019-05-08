using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.WeChat.Biz
{
    [RedisHash("Biz.Merchant.Statistics.hash")]
    [RedisDBNumber("1")]
    public class MerchantStatisticsRedis
    {
        [RedisKey]
        [RedisHashEntry("mid")]
        public string mid { get; set; }

        /// <summary>
        /// 开团总数
        /// </summary>
        [RedisHashEntry("GroupOrderTotal")]
        public string GroupOrderTotal { get; set; }

        /// <summary>
        /// 成团数量
        /// </summary>
        [RedisHashEntry("GroupOrderOkTotal")]
        public string GroupOrderOkTotal { get; set; }

        /// <summary>
        /// 订单总数
        /// </summary>
        [RedisHashEntry("OrderTotal")]
        public string OrderTotal { get; set; }

        /// <summary>
        /// 拼团成功的订单数
        /// </summary>
        [RedisHashEntry("OrderOkTotal")]
        public string OrderOkTotal { get; set; }

        /// <summary>
        /// 总收入
        /// </summary>
        [RedisHashEntry("InComeTotal")]
        public string InComeTotal { get; set; }

        /// <summary>
        /// 到店收入总数
        /// </summary>
        [RedisHashEntry("DaoDianInComeTotal")]
        public string DaoDianInComeTotal { get; set; }

        /// <summary>
        /// 物流收入总数
        /// </summary>
        [RedisHashEntry("WuLiuInComeTotal")]
        public string WuLiuInComeTotal { get; set; }
    }
}
