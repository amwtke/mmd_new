using MD.Lib.Log;
using MD.Configuration;
using MD.Model.Configuration;
using MD.Model.Index;
using MD.Model.MQ;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch
{
    public enum ELogBizModuleType
    {
        /// <summary>
        /// 商家的浏览量
        /// </summary>
        MidView = 0,

        /// <summary>
        /// 商品浏览量
        /// </summary>
        GidView,
        /// <summary>
        /// 用户关注量
        /// </summary>
        UserSub,
        /// <summary>
        /// 取消关注
        /// </summary>
        UserUnsub,
        /// <summary>
        /// 支付
        /// </summary>
        PayView,
        /// <summary>
        /// 团活动分享
        /// </summary>
        GroupShare,
        /// <summary>
        /// 商家推送活动消息
        /// </summary>
        MerSendMsg
    }

    public enum ShareType
    {
        GroupIndex,
        GroupJoin
    }

    public static class LogESManager
    {
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(LogESManager), ex);
        }
        static ElasticClient _client = null;
        static LogESConfig _config = null;
        static LogESManager()
        {
            _client = ESHeper.GetClient<LogESConfig>();
            if (_client == null)
            {
                var err = new Exception("_client没有正确初始化！");
                LogError(err);
                throw err;
            }

            _config = MdConfigurationManager.GetConfig<LogESConfig>();
            if (_config == null)
            {
                var err = new Exception("配置没有正确初始化！");
                LogError(err);
                throw err;
            }

            init();
        }

        public static void MappingAClient(ElasticClient client)
        {
            //将biz与logevent两个mapping都帮到mdlog上。
            if (!ESHeper.BeSureMapping<LogEvent>(client, _config.IndexName, new IndexSettings()
            {
                NumberOfReplicas = Convert.ToInt32(_config.NumberOfReplica),
                NumberOfShards = Convert.ToInt32(_config.NumberOfShards),
            }))
            {
                var err = new Exception("Mapping没有正确初始化！");
                LogError(err);
                throw err;
            }

            if (!ESHeper.BeSureMapping<BizIndex>(client, _config.IndexName, new IndexSettings()
            {
                NumberOfReplicas = Convert.ToInt32(_config.NumberOfReplica),
                NumberOfShards = Convert.ToInt32(_config.NumberOfShards),
            }))
            {
                var err = new Exception("Mapping没有正确初始化！");
                LogError(err);
                throw err;
            }
        }

        static void init()
        {
            //将biz与logevent两个mapping都帮到mdlog上。
            if (!ESHeper.BeSureMapping<LogEvent>(_client, _config.IndexName, new IndexSettings()
            {
                NumberOfReplicas = Convert.ToInt32(_config.NumberOfReplica),
                NumberOfShards = Convert.ToInt32(_config.NumberOfShards),
            }))
            {
                var err = new Exception("Mapping没有正确初始化！");
                LogError(err);
                throw err;
            }

            if (!ESHeper.BeSureMapping<BizIndex>(_client, _config.IndexName, new IndexSettings()
            {
                NumberOfReplicas = Convert.ToInt32(_config.NumberOfReplica),
                NumberOfShards = Convert.ToInt32(_config.NumberOfShards),
            }))
            {
                var err = new Exception("Mapping没有正确初始化！");
                LogError(err);
                throw err;
            }
        }

        public static async Task<bool> AddOrUpdateLogEventAsync(LogEvent obj)
        {
            try
            {
                var result = await _client.SearchAsync<LogEvent>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                LogEvent l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<LogEvent>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                else
                {
                    var resoponse = await _client.IndexAsync<LogEvent>(l, (i) => { i.Index(_config.IndexName); return i; });
                    return resoponse.Created;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static bool AddOrUpdateLogEvent(LogEvent obj)
        {
            try
            {
                var result = _client.Search<LogEvent>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                LogEvent l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = _client.Update<LogEvent>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                else
                {
                    var resoponse = _client.Index<LogEvent>(l, (i) => { i.Index(_config.IndexName); return i; });
                    return resoponse.Created;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static async Task<bool> AddOrUpdateBizAsync(BizIndex obj)
        {
            try
            {
                var result = await _client.SearchAsync<BizIndex>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                BizIndex l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<BizIndex>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                else
                {
                    var resoponse = await _client.IndexAsync<BizIndex>(l, (i) => { i.Index(_config.IndexName); return i; });
                    return resoponse.Created;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static bool AddOrUpdateBiz(BizIndex obj)
        {
            try
            {
                var result = _client.Search<BizIndex>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                BizIndex l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = _client.Update<BizIndex>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                else
                {
                    var resoponse = _client.Index<BizIndex>(l, (i) => { i.Index(_config.IndexName); return i; });
                    return resoponse.Created;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static bool UpdateBiz(BizIndex obj)
        {
            try
            {
                var result = _client.Search<BizIndex>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                BizIndex l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = _client.Update<BizIndex>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static bool AddOrUpdateBiz(ElasticClient client,BizIndex obj)
        {
            try
            {
                var result = client.Search<BizIndex>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                BizIndex l = obj;
                if (result.Total == 0)
                {
                    var resoponse = client.Index<BizIndex>(l, (i) => { i.Index(_config.IndexName); return i; });
                    return resoponse.Created;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static LogEvent CopyFromLogEventMQ(LogEventMQ mq)
        {
            if (mq == null || string.IsNullOrEmpty(mq.Id) || Guid.Parse(mq.Id).Equals(Guid.Empty))
                return null;
            LogEvent ret = new LogEvent();
            foreach (System.Reflection.PropertyInfo pi in ret.GetType().GetProperties())
            {
                object value = mq.GetType().GetProperty(pi.Name).GetValue(mq);
                if(value!=null)
                    pi.SetValue(ret, value);
            }
            return ret;
        }

        public static BizIndex CopyFromBizMQ(BizMQ mq)
        {
            if (mq == null || string.IsNullOrEmpty(mq.Id) || Guid.Parse(mq.Id).Equals(Guid.Empty))
                return null;
            BizIndex ret = new BizIndex();
            foreach (System.Reflection.PropertyInfo pi in ret.GetType().GetProperties())
            {
                object value = mq.GetType().GetProperty(pi.Name).GetValue(mq);
                if(value!=null)
                    pi.SetValue(ret, value);
            }
            return ret;
        }
    }
}
