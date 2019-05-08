//using MD.Lib.DB.Redis;
//using MD.Lib.DB.Redis.Objects;
//using MD.Lib.DB.Repositorys;
//using MD.Lib.Log;
//using MD.Model.Configuration.MQ;
//using MD.Model.Configuration.Redis;
//using MD.Model.Configuration.User;
//using MD.Model.MQ;
//using MD.Model.Redis.Objects.Messaging;
//using StackExchange.Redis;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using MD.Lib.Util;

//namespace MD.Lib.MQ
//{
//    public static class WeChatSendMQHelper
//    {
//        static RedisManager2<WeChatRedisConfig> _redis;
//        static WeChatSendMQHelper()
//        {
//            if (_redis == null)
//                _redis = new RedisManager2<WeChatRedisConfig>();
//            try
//            {
//                MQManager.Prepare_All_P_MQ();
//            }
//            catch (Exception ex)
//            {
//                MDLogger.LogErrorAsync(typeof(WeChatSendMQHelper), ex);
//                throw ex;
//            }
//        }


//        static ConcurrentDictionary<string, string> _userToSessionIdDIc = new ConcurrentDictionary<string, string>();
//        /// <summary>
//        /// 获取如果没有则创建一对uuid的session值。
//        /// </summary>
//        /// <param name="from">user uuid</param>
//        /// <param name="to">user uuid</param>
//        /// <returns></returns>
//        public static async Task<string> GetOrCreateSessionId(string from, string to)
//        {
//            from = from.Trim().ToUpper();
//            to = to.Trim().ToUpper();
//            string key = from + "_" + to;

//            //先看缓存中是否存在
//            string ret = null;
//            if (_userToSessionIdDIc.TryGetValue(key, out ret))
//                return ret;

//            //redis中是否存在
//            var sessionObject = await _redis.GetObjectFromRedisHash<WeChatUserToSessionHash>(key);
//            if (sessionObject != null && !String.IsNullOrEmpty(sessionObject.SessionId))
//            {
//                _userToSessionIdDIc[key] = sessionObject.SessionId;
//                return sessionObject.SessionId;
//            }

//            //双向绑定到一个sessionid,
//            //初始化user.message.user_to_session.hash这个redishash，
//            //因为sessionid是在发送消息的时候才会第一次调用。
//            string key2 = to + "_" + from;
//            WeChatUserToSessionHash o1 = new WeChatUserToSessionHash();
//            WeChatUserToSessionHash o2 = new WeChatUserToSessionHash();
//            string uuid = Guid.NewGuid().ToString();
//            o1.Key = key; o1.SessionId = uuid;
//            o2.Key = key2; o2.SessionId = uuid;
//            bool flag = await _redis.SaveObjectAsync(o1);
//            flag = await _redis.SaveObjectAsync(o2);


//            //然后反向初始化sessionid取user的redis hash
//            //user.message.session_to_user.hash
//            //users按照','分割。
//            WeChatSessoinToUsersHash h = new WeChatSessoinToUsersHash();
//            h.SessionId = uuid;
//            h.Users = to + "," + from;
//            flag = await _redis.SaveObjectAsync(h);

//            if (!flag)
//            {
//                MDLogger.LogErrorAsync(typeof(WeChatSendMQHelper), new Exception("怎么回事？sessionid创建失败！"));
//            }

//            return uuid;
//        }
//        /// <summary>
//        /// 从一个user uuid发送消息到另一个user uuid
//        /// </summary>
//        /// <param name="from">user uuid</param>
//        /// <param name="to">user uuid</param>
//        /// <param name="message"></param>
//        /// <returns></returns>
//        public static async Task<bool> SendMessage(string fromUuid, string toUuid, string message)
//        {
//            fromUuid = fromUuid.Trim().ToUpper();
//            toUuid = toUuid.Trim().ToUpper();
//            //准备sessionid
//            string sessionId = await GetOrCreateSessionId(fromUuid, toUuid);
//            ChatMessageMQ obj = new ChatMessageMQ();
//            obj.From = fromUuid;
//            obj.To = toUuid;
//            obj.PayLoad = message;
//            obj.TimeStamp = CommonHelper.GetUnixTimeNow();
//            obj.Uuid = Guid.NewGuid().ToString();
//            obj.SessionId = sessionId;
//            obj.MsgType = MsgTypeMQ.Chat;

//            //double d = await MessageRedisOp.AddUnreadScore(toUuid, sessionId, 1);

//            ////更新自己的session timestamp
//            //await MessageRedisOp.SetOrUpdateTimestampToNow(fromUuid, sessionId);

//            return MQManager.SendMQ_TB<WeChatMessageMQConfig>(obj);
//        }
//    }

//    public static class WeChatReceiveHelper
//    {
//        static RedisManager2<WeChatRedisConfig> _redis;
//        static WeChatReceiveHelper()
//        {
//            if (_redis == null)
//                _redis = new RedisManager2<WeChatRedisConfig>();
//        }


//        /// <summary>
//        /// 从redis中获取谁发的这条消息的集合。按照10步长取。
//        /// </summary>
//        /// <param name="from"></param>
//        /// <param name="to"></param>
//        /// <returns>uuid:message的格式</returns>
//        static async Task<List<string>> GetMessagesFromRedis(string fromUuid,string toUUid)
//        {
//            fromUuid = fromUuid.Trim().ToUpper();
//            toUUid = toUUid.Trim().ToUpper();

//            var db = _redis.GetDb<WeChatMessageRedis>();
//            string sessionId =await WeChatSendMQHelper.GetOrCreateSessionId(fromUuid, toUUid);
//            var config = MD.Configuration.MdConfigurationManager.GetConfig<UserBehaviorConfig>();
//            int count = Convert.ToInt32(config.GetMessageCount);
//            WeChatMessageRedis o = new WeChatMessageRedis();
//            o.SessionId = sessionId;
//            string listName = _redis.GetKeyName<WeChatMessageListAttribute>(o);
//            RedisValue[] messages = await db.ListRangeAsync(listName, 0, count - 1);
//            await db.ListTrimAsync(listName, 0, count-1);
//            return _redis.ConvertRedisValueToString(messages);
//        }

//        /// <summary>
//        /// 获取会话中最近的一个消息
//        /// </summary>
//        /// <param name="fromUuid"></param>
//        /// <param name="toUUid"></param>
//        /// <returns></returns>
//        public static async Task<string> GetFirstMessagesFromRedis(string fromUuid, string toUUid)
//        {
//            fromUuid = fromUuid.Trim().ToUpper();
//            toUUid = toUUid.Trim().ToUpper();

//            var db = _redis.GetDb<WeChatMessageRedis>();
//            string sessionId = await WeChatSendMQHelper.GetOrCreateSessionId(fromUuid, toUUid);
//            var config = MD.Configuration.MdConfigurationManager.GetConfig<UserBehaviorConfig>();
//            int count = Convert.ToInt32(config.GetMessageCount);
//            WeChatMessageRedis o = new WeChatMessageRedis();
//            o.SessionId = sessionId;
//            string listName = _redis.GetKeyName<WeChatMessageListAttribute>(o);
//            RedisValue[] messages = await db.ListRangeAsync(listName, 0, 0);
//            return _redis.ConvertRedisValueToString(messages)[0];
//        }

//        /// <summary>
//        /// 按照一定的步长去消息。
//        /// </summary>
//        /// <param name="fromUuid"></param>
//        /// <param name="toUuid"></param>
//        /// <param name="sectionNo"></param>
//        /// <returns></returns>
//        public static async Task<List<string>> GetMessage(string fromUuid, string toUuid, int sectionNo,bool isNeedCleanUnred=true)
//        {
//            fromUuid = fromUuid.Trim().ToUpper();
//            toUuid = toUuid.Trim().ToUpper();

//            var config = MD.Configuration.MdConfigurationManager.GetConfig<UserBehaviorConfig>();
//            int pageSize = Convert.ToInt32(config.GetMessageCount);
//            int fromIndex = pageSize * sectionNo;
//            string sessionid = await WeChatSendMQHelper.GetOrCreateSessionId(fromUuid, toUuid);

//            //清空fromUuid的sessionid的未读消息
//            if(isNeedCleanUnred)
//                await MessageRedisOp.CleanUnreadScore(fromUuid, sessionid);

//            if (sectionNo == 0)
//            {
//                return await GetMessagesFromRedis(fromUuid, toUuid);
//            }

//            using (MessageRepository r = new MessageRepository())
//            {
//                var list =  await r.GetLogRecordsAsync(fromIndex, pageSize, sessionid);
//                if (list == null || list.Count == 0)
//                    return null;
//                List<string> ret = new List<string>();
//                foreach(var l in list)
//                {
//                    double time = CommonHelper.ToUnixTime(l.TimeStamp);
//                    string from = l.From;
//                    string message = from + ":" + time.ToString() + ":" + l.Message;
//                    ret.Add(message);
//                }
//                return ret;
//            }
//        }
//    }
//}
