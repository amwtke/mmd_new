using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.WeChat.Biz.AttTable
{
    [RedisHash("Biz.AttTable.hash")]
    [RedisDBNumber("1")]
    public class AttTableRedis
    {
        [RedisKey]
        [RedisHashEntry("Key")]
        public string Key { get; set; }

        [RedisHashEntry("Value")]
        public string Value { get; set; }

        public static string MakeKey(Guid owner, Guid attid)
        {
            return owner.ToString() + "_" + attid.ToString();
        }
    }
}
