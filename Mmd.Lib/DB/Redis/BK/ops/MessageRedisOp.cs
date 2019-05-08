using MD.Model.Configuration.Redis;
using MD.Model.Redis.Objects.Messaging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Util;

namespace MD.Lib.DB.Redis.Objects
{
    public static class MessageRedisOp
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static MessageRedisOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        public static async Task<double> AddUnreadScore(string uuid,string sessionId, double score)
        {
            MessageCenterSessionRedis o = new MessageCenterSessionRedis();
            o.UserUuid = uuid; o.SessionId = sessionId;

            var db = _redis.GetDb<MessageCenterSessionRedis>();
            string zsetName = _redis.GetKeyName<UnReadMessageCountZsetAttribute>(o);
            return await db.SortedSetIncrementAsync(zsetName, sessionId, score);
        }

        /// <summary>
        /// 清空一个uuid，某个sessionid的聊天未读记录。
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static async Task<bool> CleanUnreadScore(string uuid,string sessionId)
        {
            MessageCenterSessionRedis o = new MessageCenterSessionRedis();
            o.UserUuid = uuid; o.SessionId = sessionId;

            var db = _redis.GetDb<MessageCenterSessionRedis>();
            string zsetName = _redis.GetKeyName<UnReadMessageCountZsetAttribute>(o);
            return await db.SortedSetAddAsync(zsetName, sessionId, 0);
        }

        /// <summary>
        /// 获取一个uuid某个聊天session的未读消息数量。
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static async Task<double> GetUnreadScore(string uuid, string sessionId)
        {
            MessageCenterSessionRedis o = new MessageCenterSessionRedis();
            o.UserUuid = uuid;o.SessionId = sessionId;

            var db = _redis.GetDb<MessageCenterSessionRedis>();
            string zsetName = _redis.GetKeyName<UnReadMessageCountZsetAttribute>(o);
            double? value = await db.SortedSetScoreAsync(zsetName, sessionId);
            if (value.HasValue)
                return value.Value;
            return 0;
        }

        /// <summary>
        /// 判断一个用户是否有未读消息
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public static async Task<bool> IsGetUnredScore(string uuid)
        {
            MessageCenterSessionRedis o = new MessageCenterSessionRedis();
            o.UserUuid = uuid;
            string zsetName = _redis.GetKeyName<UnReadMessageCountZsetAttribute>(o);
            var pair = await _redis.GetRangeByRankWithScoreAsync<MessageCenterSessionRedis>(zsetName, from: 0, to: 0);
            if (pair == null || pair.Length==0)
                return false;
            return pair[0].Value > 0;
        }

        public static async Task<bool> SetOrUpdateTimestampToNow(string uuid, string sessionid)
        {
            MessageCenterSessionRedis o = new MessageCenterSessionRedis();
            o.UserUuid = uuid; o.SessionId = sessionid;
            o.TimeStamp = CommonHelper.GetUnixTimeNow();
            return await _redis.SaveObjectAsync(o);
        }

        /// <summary>
        /// 按照时间先后顺序获取一个用户的聊天session
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="sessionId"></param>
        /// <param name="orderWay"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>返回UUid的最近的会话session列表。</returns>
        public static async Task<KeyValuePair<string, double>[]> GetSessionsTimeStampByUuid(string uuid, Order orderWay = Order.Descending, long from = 0, long to = -1)
        {
            MessageCenterSessionRedis o = new MessageCenterSessionRedis();
            o.UserUuid = uuid;
            string zsetName = _redis.GetKeyName<TimeStampZsetAttribute>(o);
            return await _redis.GetRangeByRankWithScoreAsync<MessageCenterSessionRedis>(zsetName, orderWay, from, to);
        }

        public static async Task<KeyValuePair<string, double>[]> GetSessionsUnredByUuid(string uuid, Order orderWay = Order.Descending, long from = 0, long to = -1)
        {
            MessageCenterSessionRedis o = new MessageCenterSessionRedis();
            o.UserUuid = uuid;
            string zsetName = _redis.GetKeyName<UnReadMessageCountZsetAttribute>(o);
            return await _redis.GetRangeByRankWithScoreAsync<MessageCenterSessionRedis>(zsetName, orderWay, from, to);
        }

        static ConcurrentDictionary<string, List<string>> _sessionIdToUsersDic = new ConcurrentDictionary<string, List<string>>();
        /// <summary>
        /// 根据sessionid获取会话中的成员的uuid。
        /// </summary>
        /// <param name="sessionid"></param>
        /// <returns></returns>
        public static async Task<List<string>> GetUUidsBySessionId(string sessionid)
        {
            //先读缓存
            List<string> ret = null;
            if (_sessionIdToUsersDic.TryGetValue(sessionid, out ret))
                return ret;

            WeChatSessoinToUsersHash obj = await _redis.GetObjectFromRedisHash<WeChatSessoinToUsersHash>(sessionid);
            if (obj != null && !string.IsNullOrEmpty(obj.Users))
            {
                ret = obj.Users.Split(new char[] { ',' }).ToList();
                _sessionIdToUsersDic[sessionid] = ret;
                return ret;
            }
            return ret;
        }

        ///// <summary>
        ///// 获取seesionid的会话中除开我的另一个人的uuid。
        ///// </summary>
        ///// <param name="myUuid"></param>
        ///// <param name="sessionId"></param>
        ///// <returns></returns>
        //public static async Task<string> GetOtherUuidInSameSessionId(string myUuid, string sessionId)
        //{
        //    WeChatSessoinToUsersHash obj = await _redis.GetObjectFromRedis<WeChatSessoinToUsersHash>(sessionId);
        //    if (obj != null && !string.IsNullOrEmpty(obj.Users))
        //    {
        //        myUuid = myUuid.ToUpper();
        //        foreach (var s in obj.Users.Split(new char[] { ',' }))
        //        {
        //            string temp = s.ToUpper();
        //            if (!temp.Equals(myUuid))
        //                return temp;
        //        }
        //    }
        //    return null;
        //}
    }
}
