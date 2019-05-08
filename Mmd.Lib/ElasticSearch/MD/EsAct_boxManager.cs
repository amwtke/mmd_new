using MD.Configuration;
using MD.Lib.DB.Repositorys;
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
    public static class EsAct_boxManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsAct_boxManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsAct_boxConfig _config = null;
        static EsAct_boxManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsAct_boxConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsAct_boxConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexAct_box>(_client, _config.IndexName, new IndexSettings()
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

        public static async Task<IndexAct_box> GenObjectAsync(Guid bid)
        {
            using (var repo = new ActivityRepository())
            {
                Box box = await repo.GetBoxByIdAsync(bid);
                if (box != null)
                {
                    IndexAct_box index = new IndexAct_box();
                    index.Id = box.bid.ToString();
                    index.mid = box.mid.ToString();
                    index.appid = box.appid;
                    index.title = box.title;
                    index.pic = box.pic;
                    index.description = box.description;
                    index.time_start = box.time_start;
                    index.time_end = box.time_end;
                    index.last_update_time = box.last_update_time;
                    index.status = box.status;
                    return index;
                }
                else
                {
                    return null;
                }
            }
        }

        public static IndexAct_box GenObject(Box box)
        {
            if (box != null)
            {
                IndexAct_box index = new IndexAct_box();
                index.Id = box.bid.ToString();
                index.mid = box.mid.ToString();
                index.appid = box.appid;
                index.title = box.title;
                index.pic = box.pic;
                index.description = box.description;
                index.time_start = box.time_start;
                index.time_end = box.time_end;
                index.last_update_time = box.last_update_time;
                index.status = box.status;
                return index;
            }
            else
            {
                return null;
            }
        }
        public static async Task<bool> AddOrUpdateAsync(IndexAct_box obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_box>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexAct_box l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexAct_box>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                var resoponse = await _client.IndexAsync(l, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }
        public static async Task<IndexAct_box> GetBybidAsync(Guid bid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_box>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(bid.ToString()))));
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
        public static async Task<IndexAct_box> GetBymidAsync(Guid mid, int status)
        {
            try
            {
                QueryContainer container = null;
                var statusContainer = Query<IndexAct_box>.Term("status", status);
                container = container && statusContainer;
                var midContainer = Query<IndexAct_box>.Term("mid", mid.ToString());
                container = container && midContainer;
                var result = await _client.SearchAsync<IndexAct_box>(s => s.Index(_config.IndexName).Query(container).SortDescending("last_update_time")
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
        public static async Task<IndexAct_box> GetByAppidAsync(string appid, int status, double? inTime = null)
        {
            try
            {
                double nowTime = CommonHelper.ToUnixTime(DateTime.Now);
                if (inTime != null)
                    nowTime = inTime.Value;
                QueryContainer container = null;
                var statusContainer = Query<IndexAct_box>.Term("status", status);
                container = container && statusContainer;
                var appidContainer = Query<IndexAct_box>.Term("appid", appid);
                container = container && appidContainer;
                var time1Container = Query<IndexAct_box>.Range(r => r.OnField(p => p.time_start).LowerOrEquals(nowTime));
                var time2Container = Query<IndexAct_box>.Range(r => r.OnField(p => p.time_end).GreaterOrEquals(nowTime));
                container = container && time1Container && time2Container;
                var result = await _client.SearchAsync<IndexAct_box>(s => s.Index(_config.IndexName).Query(container).SortDescending("last_update_time")
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
