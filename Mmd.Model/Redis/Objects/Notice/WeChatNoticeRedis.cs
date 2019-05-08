using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.Messaging
{
    [RedisDBNumber("5")]
    public class WeChatNoticeRedis
    {
        [RedisKey]
        public string Uuid { get; set; }
        
        [NoticeList("EVERY_KEY", ListPush.Left)]
        public string Message { get; set; }
    }

    
    [RedisDBNumber("5")]
    public class WeChatNoticeCountRedis
    {
        [RedisKey]
        [UnReadNoticeCountZset("user.notice.Count.zset", "")]
        public string Uuid { get; set; }
    }

    public class NoticeListAttribute : RedisListAttribute
    {
        public NoticeListAttribute(string name, ListPush push) : base(name, push)
        { }
    }

    public class UnReadNoticeCountZsetAttribute : RedisZSetAttribute
    {
        public UnReadNoticeCountZsetAttribute(string name, string filed) : base(name, filed)
        {

        }
    }
}
