using MD.Model.Configuration.Redis;
using MD.Model.Redis.Objects.Messaging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.DB.Redis.Objects
{
    public static class NoticeRedisOp
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static NoticeRedisOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        public static async Task<double> AddUnreadScore(Guid uuid, double score)
        {
            WeChatNoticeCountRedis o = new WeChatNoticeCountRedis();
            o.Uuid = uuid.ToString();

            var db = _redis.GetDb<WeChatNoticeCountRedis>();
            string zsetName = _redis.GetKeyName<UnReadNoticeCountZsetAttribute>(o);
            return await db.SortedSetIncrementAsync(zsetName, uuid.ToString(), score);
        }
        
        public static async Task<bool> CleanUnreadScore(Guid uuid)
        {
            WeChatNoticeCountRedis o = new WeChatNoticeCountRedis();
            o.Uuid = uuid.ToString();

            var db = _redis.GetDb<WeChatNoticeCountRedis>();
            string zsetName = _redis.GetKeyName<UnReadNoticeCountZsetAttribute>(o);
            return await db.SortedSetAddAsync(zsetName, uuid.ToString(), 0);
        }
        
        public static async Task<double> GetUnreadScore(Guid uuid)
        {
            WeChatNoticeCountRedis o = new WeChatNoticeCountRedis();
            o.Uuid = uuid.ToString();

            var db = _redis.GetDb<WeChatNoticeCountRedis>();
            string zsetName = _redis.GetKeyName<UnReadNoticeCountZsetAttribute>(o);
            double? value = await db.SortedSetScoreAsync(zsetName, uuid.ToString());
            if (value.HasValue)
                return value.Value;
            return 0;
        }        
        
    }
}
