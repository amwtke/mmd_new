using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.UserBehavior
{
    [RedisDBNumber("0")]
    public class UserLoginRedis
    {
        [UserLoginBehaviorLastTimeZset("login.behavior.LastAccessTime.zset", "")]
        [UserLoginBehaviorCountZset("login.behavior.Count.zset","")]
        [RedisKey]
        public string OpenId { get; set; }

        //[UserLoginBehaviorList("EVERY_KEY", ListPush.Left)]
        //public Double LastLoginTime { get; set; }
    }

    public class UserLoginBehaviorCountZsetAttribute : RedisZSetAttribute
    {
        public UserLoginBehaviorCountZsetAttribute(string name, string fieldName) : base(name, fieldName) { }
    }

    public class UserLoginBehaviorLastTimeZsetAttribute : RedisZSetAttribute
    {
        public UserLoginBehaviorLastTimeZsetAttribute(string name, string fieldName) : base(name, fieldName) { }
    }

    public class UserLoginBehaviorListAttribute : RedisListAttribute
    {
        public UserLoginBehaviorListAttribute(string name, ListPush push) : base(name, push) { }
    }

}
