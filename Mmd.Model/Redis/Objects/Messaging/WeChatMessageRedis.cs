using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.Messaging
{
    [RedisDBNumber("4")]
    public class WeChatMessageRedis
    {
        /// <summary>
        /// 会话的ID。由参与人共同生成的一个id，标识一个会话。为UUID。
        /// </summary>
        [RedisKey]
        public string SessionId { get; set; }

        /// <summary>
        /// openid:Message正文。的形式存在。
        /// </summary>
        [WeChatMessageList("EVERY_KEY",ListPush.Left)]
        public string Message { get; set; }
    }
    /// <summary>
    /// 每个user都有两个zset，一个记录登陆时间的排序，一个记录未读消息的排序
    /// </summary>
    [RedisDBNumber("4")]
    public class MessageCenterSessionRedis
    {
        [RedisKey]
        public string UserUuid { get; set; }

        [TimeStampZset("EVERY_KEY", "TimeStamp")]
        [UnReadMessageCountZset("EVERY_KEY", "")]//只创建
        public string SessionId { get; set; }

        
        public double TimeStamp { get; set; }
    }

    public class WeChatMessageListAttribute : RedisListAttribute
    {
        public WeChatMessageListAttribute(string name, ListPush push) : base(name, push)
        { }
    }

    public class TimeStampZsetAttribute : RedisZSetAttribute
    {
        public TimeStampZsetAttribute(string name,string filed):base(name,filed)
        {

        }
    }
    public class UnReadMessageCountZsetAttribute : RedisZSetAttribute
    {
        public UnReadMessageCountZsetAttribute(string name, string filed) : base(name, filed)
        {

        }
    }

}
