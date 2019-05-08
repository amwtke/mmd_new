using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.Statistics
{
    [RedisHash("Sta.LoginCheckUser.hash")]
    [RedisDBNumber("1")]
   public class StaLoginCheckRedis
    {
        [RedisKey]
        [RedisHashEntry("userid")]
        public string userid { get; set; }
        [RedisHashEntry("expTime")]
        public double expTime { get; set; }
    }
}
