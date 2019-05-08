using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Repositorys;
using MD.Lib.MQ.MD;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Redis;
using MD.Model.DB;
using MD.Model.Redis.RedisObjects.Vector;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using Order = StackExchange.Redis.Order;

namespace MD.Lib.Weixin.Vector.Vectors
{
    public enum ETimelineType
    {
        CJPT,   //参加拼团的类型
        KT,     //开团
        YDWZ,   //阅读文章后
    }
    /// <summary>
    /// 时间线
    /// </summary>
    public class TimeLineVectorProcessor : IVectorProcessor
    {
        public bool IsVisible(Model.DB.Professional.Vector v)
        {
            return v.visible;
        }

        public string GetVectorType()
        {
            return EVectorType.TL.ToString();
        }

        public VectorView Parser(string expression)
        {
            try
            {
                if (string.IsNullOrEmpty(expression))
                    return null;

                string[] parts = expression.Split(new[] {VectorCommonHelper.PS}, StringSplitOptions.RemoveEmptyEntries);//expression.Split(new char[] { '`' });

                if (parts.Length == 5)
                {
                    VectorView ret = new VectorView();

                    string from = parts[0];
                    ret.Owner = from;

                    if (!parts[1].Trim().Equals(string.Empty))
                        ret.Objects = parts[1].Split(new[] {VectorCommonHelper.IPS},
                            StringSplitOptions.RemoveEmptyEntries);

                    if (!parts[2].Trim().Equals(VectorCommonHelper.Empty))
                    {
                        List<string> temp = new List<string>();
                        foreach (var c in parts[2].Split(new[] { VectorCommonHelper.IPS },
                            StringSplitOptions.RemoveEmptyEntries))
                        {
                            var replace = VectorCommonHelper.Decode(c);//c.Replace("$001", ":");
                            temp.Add(replace);
                        }
                        ret.Contents = temp.ToArray();
                    }

                    ret.Timestamp = Convert.ToDouble(parts[3]);

                    ret.Type = parts[4];

                    return ret;
                }
                return null;
            }
            catch (Exception ex)
            {
                
                throw new MDException(typeof(TimeLineVectorProcessor),ex);
            }
        }

        public async Task Route(Model.DB.Professional.Vector v)
        {
            await VectorProcessorManager.PreRouteAsnyc(v);

            var _redis = new RedisManager2<WeChatRedisConfig>();
            if (!string.IsNullOrEmpty(v?.expression))
            {
                string from = v.expression.Split(new[] {VectorCommonHelper.PS},
                    StringSplitOptions.RemoveEmptyEntries)[0];
                Guid uid;
                if(Guid.TryParse(from,out uid))
                {
                    //获取好友信息
                    var ret =
                    await
                        _redis.GetRangeByRankAsync<VectorQMRedis, VectorUserQMZsetAttribute>(uid.ToString());
                    if (ret != null && ret.Length > 0)
                    {
                        //插入好友的时间线
                        foreach (var kv in ret)
                        {
                            await
                                _redis.AddScoreEveryKeyAsync<TimeLineVectorZsetRedis, TimeLineVectorZsetAttribute>(
                                    kv.Key, v.vid.ToString(), CommonHelper.GetUnixTimeNow());
                        }
                    }
                }
            }
        }

        public Model.DB.Professional.Vector GenVector(object obj)
        {
            if (obj == null)
                return null;

            TimeLineVectorParameter parameters = obj as TimeLineVectorParameter;
            Model.DB.Professional.Vector v = new Model.DB.Professional.Vector()
            {
                vid = Guid.NewGuid(),
                type = EVectorType.TL.ToString(),
                timestamp = CommonHelper.GetUnixTimeNow(),
                expression = parameters.GetExpression(),
                visible = true,
                owner = parameters.FromUuid
            };
            return v;
        }


        public class TimeLineVectorParameter
        {
            public string Type { get; set; }
            /// <summary>
            /// Vector激发人的uuid
            /// </summary>
            public Guid FromUuid { get; set; }
            /// <summary>
            /// 激发出的对象uuids
            /// </summary>
            public List<Guid> BizUuids { get; set; }
            /// <summary>
            /// 每个对象的评论
            /// </summary>
            public List<string> Comments { get; set; }

            public double TimeStamp { get; set; }

            private string GetBizs()
            {
                if (BizUuids != null && BizUuids.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var b in BizUuids)
                    {
                        sb.Append(b + VectorCommonHelper.IPS);
                    }
                    //sb.Remove(sb.Length - 1, 1);
                    return sb.ToString();
                }
                return VectorCommonHelper.Empty;
            }

            private string GetComments()
            {
                if (Comments != null && Comments.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var c in Comments)
                    {
                        var v = VectorCommonHelper.Encode(c);
                        sb.Append(v + VectorCommonHelper.IPS);
                    }
                    //sb.Remove(sb.Length - 1, 1);
                    return sb.ToString();
                }
                return VectorCommonHelper.Empty;
            }
            public string GetExpression()
            {
                return
                    $"{FromUuid}{VectorCommonHelper.PS}{GetBizs()}{VectorCommonHelper.PS}{GetComments()}{VectorCommonHelper.PS}{TimeStamp}{VectorCommonHelper.PS}{Type}";
            }
        }
    }

    public static class TimeLineVectorHelper
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static TimeLineVectorHelper()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        public static TimeLineVectorProcessor.TimeLineVectorParameter GenParameter(ETimelineType t,Guid fromUid,List<Guid>bizGuids,List<string> Comments)
        {
            if (fromUid.Equals(Guid.Empty) || bizGuids == null || bizGuids.Count == 0)
                return null;

            TimeLineVectorProcessor.TimeLineVectorParameter ret = new TimeLineVectorProcessor.TimeLineVectorParameter
            {
                Type = t.ToString(),
                FromUuid = fromUid
            };

            if (Comments == null || Comments.Count == 0)
            {
                Comments = new List<string>();
                for (int i = 0; i < bizGuids.Count; i++)
                {
                    if (t.Equals(ETimelineType.YDWZ))
                    {
                        Comments.Add("这篇文章不错耶！");
                    }
                    else if(t.Equals(ETimelineType.CJPT))
                    {
                        Comments.Add("我参加了拼团~");
                    }
                    else
                    {
                        Comments.Add("我开了个拼团，大家来快来拼啊~");
                    }
                }
            }
            ret.Comments = Comments;
            ret.BizUuids = bizGuids;
            ret.TimeStamp = CommonHelper.GetUnixTimeNow();
            return ret;
        }

        public static async Task<Tuple<int, List<KeyValuePair<string, double>>>> GetTopAsnyc(Guid uid, int index, int size = 10)
        {
            try
            {
                if (index <= 0)
                    return null;

                int from = (index - 1) * size;
                int to = from + size - 1;
                var ret =
                    await
                        _redis.GetRangeByRankAsync<TimeLineVectorZsetRedis, TimeLineVectorZsetAttribute>(uid.ToString(),
                            Order.Descending, from, to);

                //返回的vid与timestamp的列表
                List<KeyValuePair<string, double>> _list = new List<KeyValuePair<string, double>>();

                //返回列表总数
                int totalCount =
                    (int)await _redis.GetZsetCountAsync<TimeLineVectorZsetRedis, TimeLineVectorZsetAttribute>(uid.ToString());

                if (ret != null && ret.Length > 0)
                {
                    return Tuple.Create(totalCount, ret.ToList());
                }
                return Tuple.Create(0, _list);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(TimeLineVectorHelper), ex);
            }
        }

        public static void ZhongChao(Guid nid, Guid uid)
        {
            var p = GenParameter(ETimelineType.YDWZ, uid, new List<Guid>() {nid}, null);
            MqVectorManager.Send<TimeLineVectorProcessor>(p);
        }

        public static void VectorTuanSend(string openid, Guid goid)
        {
            if (string.IsNullOrEmpty(openid) || Guid.Empty.Equals(goid))
                return;
            try
            {
                var userRedis = new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<UserInfoRedis>(openid);
                if (string.IsNullOrEmpty(userRedis.Uid))
                    return;

                Guid uid = Guid.Parse(userRedis.Uid);

                using (var repo = new BizRepository())
                {
                    var go = repo.GroupOrderGet_TB(goid);
                    if (go != null)
                    {
                        if (go.leader.Equals(uid))
                        {
                            var p = GenParameter(ETimelineType.KT, uid, new List<Guid>() { goid }, null);
                            MqVectorManager.Send<TimeLineVectorProcessor>(p);
                        }
                        else
                        {
                            var p = GenParameter(ETimelineType.CJPT, uid, new List<Guid>() { goid }, null);
                            MqVectorManager.Send<TimeLineVectorProcessor>(p);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(TimeLineVectorHelper),ex);
            }
            
        }
    }
}
