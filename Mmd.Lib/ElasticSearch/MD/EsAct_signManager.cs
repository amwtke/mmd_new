using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB.Activity;
using MD.Model.Index.MD;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsAct_signManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsAct_signManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsAct_signConfig _config = null;
        static EsAct_signManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsAct_signConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsAct_signConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexAct_sign>(_client, _config.IndexName, new IndexSettings()
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

        public static async Task<IndexAct_sign> GenObjectAsync(Sign sign)
        {
            if (sign != null)
            {
                IndexAct_sign index = new IndexAct_sign();
                index.Id = sign.sid.ToString();
                index.mid = sign.mid.ToString();
                index.appid = sign.appid;
                index.timeStart = sign.timeStart;
                index.timeEnd = sign.timeEnd;
                index.awardName = sign.awardName;
                index.awardCount = sign.awardCount;
                index.awardQuatoCount = sign.awardQuatoCount;
                index.awardDescription = sign.awardDescription;
                index.awardPic = sign.awardPic;
                index.mustSignCount = sign.mustSignCount;
                index.title = sign.title;
                index.description = sign.description;
                index.status = sign.status;
                index.last_update_time = sign.last_update_time;
                return index;
            }
            else
            {
                return null;
            }
        }
        public static async Task<bool> AddOrUpdateAsync(IndexAct_sign obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_sign>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexAct_sign>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                var resoponse = await _client.IndexAsync(obj, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }
        public static async Task<IndexAct_sign> GetBysidAsync(Guid bid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_sign>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(bid.ToString()))));
                if (result.Total >= 1)
                {
                    return result.Documents.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }
        public static async Task<IndexAct_sign> GetBymidAsync(Guid mid, int status)
        {
            try
            {
                QueryContainer container = null;
                var statusContainer = Query<IndexAct_sign>.Term("status", status);
                container = container && statusContainer;
                var midContainer = Query<IndexAct_sign>.Term("mid", mid.ToString());
                container = container && midContainer;
                var result = await _client.SearchAsync<IndexAct_sign>(s => s.Index(_config.IndexName).Query(container).SortDescending("last_update_time")
                .Skip(0).Take(1));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    return result.Documents.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }
        public static async Task<IndexAct_sign> GetByAppidAsync(string appid, int status, double? inTime = null)
        {
            try
            {
                double nowTime = CommonHelper.ToUnixTime(DateTime.Now);
                if (inTime != null)
                    nowTime = inTime.Value;
                QueryContainer container = null;
                var statusContainer = Query<IndexAct_sign>.Term("status", status);
                container = container && statusContainer;
                var appidContainer = Query<IndexAct_sign>.Term("appid", appid);
                container = container && appidContainer;
                var time1Container = Query<IndexAct_sign>.Range(r => r.OnField(p => p.timeStart).LowerOrEquals(nowTime));
                var time2Container = Query<IndexAct_sign>.Range(r => r.OnField(p => p.timeEnd).GreaterOrEquals(nowTime));
                container = container && time1Container && time2Container;
                var result = await _client.SearchAsync<IndexAct_sign>(s => s.Index(_config.IndexName).Query(container).SortDescending("last_update_time")
                .Skip(0).Take(1));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    return result.Documents.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }
    }
}
