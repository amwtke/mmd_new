using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.Messaging
{
    [RedisDBNumber("6")]
    public class EKCommentRedis
    {
        /// <summary>
        /// 文章ID
        /// </summary>
        [RedisKey]
        public long Id { get; set; }

        /// <summary>
        /// 评论.UUID|unixtime|msg text
        /// </summary>
        [EKCommentList("EVERY_KEY", ListPush.Left)]
        public string Comment { get; set; }
    }

    public class EKCommentListAttribute : RedisListAttribute
    {
        public EKCommentListAttribute(string name, ListPush push) : base(name, push)
        { }
    }
}
