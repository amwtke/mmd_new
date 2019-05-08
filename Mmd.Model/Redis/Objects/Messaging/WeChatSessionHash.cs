using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.Messaging
{
    /// <summary>
    /// 加好友或者第一次聊天的时候进行双向的绑定。
    /// uuid1_uuid2-----sessionid1
    /// uuid2_uuid1-----sessionid1
    /// </summary>
    [RedisDBNumber("3")]
    [RedisHash("user.message.user_to_session.hash")]
    public class WeChatUserToSessionHash
    {
        /// <summary>
        /// uuid1_uuid2的形式。
        /// </summary>
        [RedisKey]
        public string Key { get; set; }

        [RedisHashEntry("SessionId")]
        public string SessionId { get; set; }
    }

    [RedisDBNumber("3")]
    [RedisHash("user.message.session_to_user.hash")]
    public class WeChatSessoinToUsersHash
    {
        [RedisKey]
        public string SessionId { get; set; }

        [RedisHashEntry("Users")]
        public string Users { get; set; }
    }
}
