using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.EK
{
    [RedisDBNumber("6")]
    public class EKTodayRedis
    {
        [EKZanCountZset("ek.zan.zset","")]
        [EKReadCountZset("ek.read.zset", "")]
        [EKCommentCountZset("ek.comment.zset", "")]

        [EKReadPepleZset("EVERY_KEY","")]
        [EKZanPepleZset("EVERY_KEY", "")]

        [RedisKey]
        public string EkId { get; set; }
    }

    public class EKReadPepleZsetAttribute : RedisZSetAttribute
    {
        public EKReadPepleZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class EKZanPepleZsetAttribute : RedisZSetAttribute
    {
        public EKZanPepleZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class EKZanCountZsetAttribute : RedisZSetAttribute
    {
        public EKZanCountZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class EKReadCountZsetAttribute : RedisZSetAttribute
    {
        public EKReadCountZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class EKCommentCountZsetAttribute : RedisZSetAttribute
    {
        public EKCommentCountZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }
}
