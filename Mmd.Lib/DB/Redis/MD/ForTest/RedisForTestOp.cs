using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.ForTest;

namespace MD.Lib.DB.Redis.MD.ForTest
{
    public static class RedisForTestOp
    {
        private static RedisManager2<WeChatRedisConfig> _redis;
        static RedisForTestOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        #region test appid

        public static async Task<bool> AddTestAppidAsync(string appid)
        {
            return await _redis.SetAddAsync<ForTestRedis, ForTestAppidSetAttribute>(appid, appid);
        }

        public static async Task<bool> RemoveTestAppidAsnyc(string appid)
        {
            return await _redis.SetRemoveAsync<ForTestRedis, ForTestAppidSetAttribute>(appid, appid);
        }

        public static async Task<bool> ContainAppidAsync(string appid)
        {
            return await _redis.SetContainAsync<ForTestRedis, ForTestAppidSetAttribute>(appid, appid);
        }

        public static async Task<List<string>> GetAllAppidsAsnyc(string key=null)
        {
            var redisList = await _redis.SetMembersAsync<ForTestRedis, ForTestAppidSetAttribute>(key);
            if (redisList != null && redisList.Count > 0)
            {
                List<string> ret = new List<string>();
                foreach (var r in redisList)
                {
                    ret.Add(r);
                }
                return ret;
            }
            return null;
        }
        #endregion
    }
}
