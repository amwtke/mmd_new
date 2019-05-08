using MD.Model.Configuration.Redis;
using MD.Model.Redis.Objects.UserBehavior;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.DB.Redis.Objects
{
    public static class NameCardAccessCountOP
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static NameCardAccessCountOP()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        public static async Task<double> AddScore(string uuid, double score)
        {
            NameCardRedis o = new NameCardRedis();
            o.Useruuid = uuid.ToUpper();

            var db = _redis.GetDb<NameCardRedis>();
            string zsetName = _redis.GetKeyName<NameCardCountZsetAttribute>(o);
            return await db.SortedSetIncrementAsync(zsetName, o.Useruuid, score);
        }

        public static async Task<bool> CleanScore(string uuid)
        {
            NameCardRedis o = new NameCardRedis();
            o.Useruuid = uuid.ToUpper();

            var db = _redis.GetDb<NameCardRedis>();
            string zsetName = _redis.GetKeyName<NameCardCountZsetAttribute>(o);
            return await db.SortedSetAddAsync(zsetName, o.Useruuid, 0);
        }

        public static async Task<double> GetScore(string uuid)
        {
            NameCardRedis o = new NameCardRedis();
            o.Useruuid = uuid.ToUpper();

            var db = _redis.GetDb<NameCardRedis>();
            string zsetName = _redis.GetKeyName<NameCardCountZsetAttribute>(o);
            double? value = await db.SortedSetScoreAsync(zsetName, o.Useruuid);
            if (value.HasValue)
                return value.Value;
            return 0;
        }

        public static async Task<KeyValuePair<string, double>[]> GetRange(Order orderWay = Order.Descending, long from = 0, long to = -1)
        {
            NameCardRedis o = new NameCardRedis();
            string zsetName = _redis.GetKeyName<NameCardCountZsetAttribute>(o);
            return await _redis.GetRangeByRankWithScoreAsync<NameCardRedis>(zsetName, orderWay, from, to);
        }
    }
}
