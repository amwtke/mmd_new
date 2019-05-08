using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.WeChat.WeChat
{
    [RedisHash("Wx.TemplateMsg.hash")]
    [RedisDBNumber("1")]
    public class WxTemplateMsgRedis
    {
        [RedisKey]
        [RedisHashEntry("key")]
        public string key { get; set; } 

        [RedisHashEntry("tempId")]
        public string tempId { get; set; }

        public static string makeKey(string appid, string shortId)
        {
            return appid + "_" + shortId;
        }
    }
}
