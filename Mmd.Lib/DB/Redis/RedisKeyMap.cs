using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.DB.Redis
{
    /// <summary>
    /// XXXX_NAME的格式来记录redis中所有的key。以便更新与提取。
    /// XXXX为key的类型，Hash，String，List，Set与Zset。
    /// EveryKey——表示是一个多实例的键值对结构的键，这个键是个变化的String（如果没有Every则表示全局唯一）。XXXXEntry表示键值对的项。
    /// </summary>
    public enum RedisKeyMap
    {
        AuthDB=0,
        /// <summary>
        /// 为String类型的key，用于存储site的AccessToken。全局一个。并设置有效期。
        /// </summary>
        String_Site_AccessToken,

        /// <summary>
        /// Site的AccessToken过期的时间。用Unixtime的Double表示。全局一个。
        /// </summary>
        String_Site_TokenExpireIn,
     
    }
}
