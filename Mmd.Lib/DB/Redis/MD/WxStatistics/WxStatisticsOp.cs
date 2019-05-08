using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Biz.UserBehavior;
// ReSharper disable All

namespace MD.Lib.DB.Redis.MD.WxStatistics
{
    public static class WxStatisticsOp
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static WxStatisticsOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        #region wx login tatol and exception

        public static async Task<bool> AddExceptionCount()
        {
            int currentValue = await GetExceptionCountAsync();
            currentValue = currentValue + 1;
            return await _redis.StringSetAsync<WxLoginStatisticsRedis, WxExceptionNumberAttribute>(currentValue);
        }

        public static async Task<int> GetExceptionCountAsync()
        {
            var currentValue = await _redis.StringGetAsync<WxLoginStatisticsRedis, WxExceptionNumberAttribute>();
            if (currentValue.IsNullOrEmpty)
                return 0;
            int ret;
            if (currentValue.TryParse(out ret))
                return ret;
            throw new Exception("Redis解析数据错误！object:WxLoginStatisticsRedis;att:WxExceptionNumberAttribute");
        }

        public static async Task<bool> AddTotalLoginCount()
        {
            int currentValue = await GetTotalLoginCount();
            currentValue = currentValue + 1;
            return await _redis.StringSetAsync<WxLoginStatisticsRedis, WxLoginNumberAttribute>(currentValue);
        }

        public static async Task<int> GetTotalLoginCount()
        {
            var currentValue = await _redis.StringGetAsync<WxLoginStatisticsRedis, WxLoginNumberAttribute>();
            if (currentValue.IsNullOrEmpty)
                return 0;
            int ret;
            if (currentValue.TryParse(out ret))
                return ret;
            throw new Exception("Redis解析数据错误！object:WxLoginStatisticsRedis;att:WxLoginNumberAttribute");
        }

        #endregion
    }
}
