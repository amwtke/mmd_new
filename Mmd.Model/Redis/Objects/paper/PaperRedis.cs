using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.paper
{
    [RedisDBNumber("7")]
    public class PaperRedis
    {
        [PZanCountZset("paper.zan.zset", "")]
        [PReadCountZset("paper.read.zset", "")]
        [PCommentCountZset("paper.comment.zset", "")]

        [PReadPeopleZset("EVERY_KEY", "")]
        [PZanPeopleZset("EVERY_KEY", "")]

        [RedisKey]
        public string Id { get; set; }
    }

    public class PZanCountZsetAttribute : RedisZSetAttribute
    {
        public PZanCountZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class PReadCountZsetAttribute : RedisZSetAttribute
    {
        public PReadCountZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class PCommentCountZsetAttribute : RedisZSetAttribute
    {
        public PCommentCountZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class PReadPeopleZsetAttribute : RedisZSetAttribute
    {
        public PReadPeopleZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class PZanPeopleZsetAttribute : RedisZSetAttribute
    {
        public PZanPeopleZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }
}
