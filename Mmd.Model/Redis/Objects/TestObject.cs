using MD.Model.Redis.att.CustomAtts.lists;
using MD.Model.Redis.att.CustomAtts.sets;
using MD.Model.Redis.att.CustomAtts.zsets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects
{
    [RedisHash("test.hash")]
    [RedisDBNumber("1")]
    public class TestObjectRedis
    {
        [TestObjectIdSet("id.set")]
        [TestObjectIdZSet("id.zset", "Scoreid")]
        [TestIdList("id.list", ListPush.Left)]
        [RedisKey]
        public string Id { get; set; }

        [TestObjectId2Set("id2.set")]
        [TestObjectId2ZSet("id2.zset", "Scoreid2")]
        [TestId2List("id2.list", ListPush.Right)]
        [RedisHashEntry("id2")]
        public string Id2 { get; set; }

        [RedisHashEntry("Country")]
        public string Country { get; set; }

        [RedisHashEntry("NiceName")]
        public string NiceName { get; set; }

        [RedisHashEntry("Scoreid")]
        public double Scoreid { get; set; }

        [RedisHashEntry("Scoreid2")]
        public double Scoreid2 { get; set; }
    }
}
