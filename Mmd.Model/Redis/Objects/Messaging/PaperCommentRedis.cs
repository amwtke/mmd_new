using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.Messaging
{
    [RedisDBNumber("7")]
    public class PaperCommentRedis
    {
        [RedisKey]
        public long Id { get; set; }

        /// <summary>
        /// 评论.UUID|unixtime|msg text
        /// </summary>
        [PCommentList("EVERY_KEY", ListPush.Left)]
        public string Comment { get; set; }
    }
    public class PCommentListAttribute : RedisListAttribute
    {
        public PCommentListAttribute(string name, ListPush push) : base(name, push)
        { }
    }

}
