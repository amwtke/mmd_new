using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.Vector
{

    #region 每个人的zset
    /// <summary>
    /// 每个人都有个Timeline的zset
    /// </summary>
    [RedisDBNumber("2")]
    public class VectorTimeLineRedis
    {
        [RedisKey]
        [VectorUserTimeLineZset("EVERY_KEY","")]
        public string UserUuid { get; set; }
    }

    public class VectorUserTimeLineZsetAttribute : RedisZSetAttribute
    {
        public VectorUserTimeLineZsetAttribute(string name, string fieldName) : base(name, fieldName) { }
    }


    /// <summary>
    /// 每个人都有的亲密zset
    /// </summary>
    [RedisDBNumber("3")]
    public class VectorQMRedis
    {
        [RedisKey]
        [VectorUserQMZset("EVERY_KEY", "")]
        public string UserUuid { get; set; }
    }

    public class VectorUserQMZsetAttribute : RedisZSetAttribute
    {
        public VectorUserQMZsetAttribute(string name, string fieldName) : base(name, fieldName) { }
    }
    #endregion

    #region 时间线的vector的zset
    [RedisDBNumber("4")]
    public class TimeLineVectorZsetRedis
    {
        [RedisKey]
        [TimeLineVectorZset("EVERY_KEY", "")]
        public string Uid { get; set; }
    }

    public class TimeLineVectorZsetAttribute : RedisZSetAttribute
    {
        public TimeLineVectorZsetAttribute(string name, string fieldName) : base(name, fieldName) { }
    }

    #endregion


    #region vector hash
    [RedisDBNumber("4")]
    [RedisHash("vector.hash")]
    public class VectorRedis
    {
        [RedisKey]
        //[RedisHashEntry("vid")]
        public string vid { get; set; }

        [RedisHashEntry("value")]
        public string value { get; set; }

        //public string GenValue(DB.Professional.Vector v)
        //{
        //    return $"expression>{v.expression}|timestamp>{v.timestamp}|type>{v.type}|visible>{v.visible}";
        //}

        //public DB.Professional.Vector GetObject()
        //{
        //    if (string.IsNullOrEmpty(value))
        //        return null;

        //    string[] parts = value.Split(new char[] {'|'});

        //    if (parts.Length > 0)
        //    {
        //        DB.Professional.Vector v = new DB.Professional.Vector();
        //        foreach (var f in parts)
        //        {
        //            string[] fs = f.Split(new char[] {'>'});
        //            if(fs.Length!=2)
        //                continue;
        //            string fName = fs[0];
        //            string fValue = fs[1];

        //            if (fName.Equals("expression"))
        //                v.expression = fValue;

        //            if (fName.Equals("timestamp"))
        //                v.timestamp = Convert.ToDouble(fValue);

        //            if (fName.Equals("type"))
        //                v.type = fValue;

        //            if (fName.Equals("visible"))
        //                v.visible = Convert.ToBoolean(fValue);
        //        }

        //        v.vid = Guid.Parse(vid);
        //        return v;
        //    }
        //    return null;
        //}
    }

    #endregion
}
