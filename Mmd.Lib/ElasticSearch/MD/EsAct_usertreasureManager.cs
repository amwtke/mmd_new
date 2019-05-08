using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.Index.MD;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsAct_usertreasureManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsAct_usertreasureManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsAct_usertreasureConfig _config = null;
        static EsAct_usertreasureManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsAct_usertreasureConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsAct_usertreasureConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexAct_usertreasure>(_client, _config.IndexName, new IndexSettings()
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

        public static async Task<IndexAct_usertreasure> GenObjAsync(Guid utid)
        {
            using (var acti = new ActivityRepository())
            {
                var db_UserTrea = await acti.GetUserTreasureByUtidAsync(utid);
                if (db_UserTrea == null || db_UserTrea.utid.Equals(Guid.Empty))
                    return null;
                IndexAct_usertreasure index_ut = new IndexAct_usertreasure();
                index_ut.Id = db_UserTrea.utid.ToString();
                index_ut.uid = db_UserTrea.uid.ToString();
                index_ut.mid = db_UserTrea.mid.ToString();
                index_ut.openid = db_UserTrea.openid;
                index_ut.btid = db_UserTrea.btid.ToString();
                index_ut.bid = db_UserTrea.bid.ToString();
                index_ut.status = db_UserTrea.status;
                index_ut.open_time = db_UserTrea.open_time;
                index_ut.writeofftime = db_UserTrea.writeofftime;
                if (db_UserTrea.writeoffer != null) index_ut.writeoffer = db_UserTrea.writeoffer.ToString();
                return index_ut;
            }
        }

        public static async Task<bool> AddOrUpdateAsync(IndexAct_usertreasure obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_usertreasure>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexAct_usertreasure l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexAct_usertreasure>((u) =>
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

        public static async Task<IndexAct_usertreasure> GetByUidAsync(Guid uid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_usertreasure>(s => s.Query(q => q.Term(t => t.OnField("uid").Value(uid.ToString()))));
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
        public static async Task<IndexAct_usertreasure> GetByOpenidAsync(Guid bid, string openid)
        {
            try
            {
                QueryContainer container = null;
                var bidContainer = Query<IndexAct_usertreasure>.Term("bid", bid.ToString());
                container = container && bidContainer;
                var openidContainer = Query<IndexAct_usertreasure>.Term("openid", openid);
                container = container && openidContainer;


                var result = await _client.SearchAsync<IndexAct_usertreasure>(s => s.Index(_config.IndexName).Query(container)
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

        public static async Task<List<IndexAct_usertreasure>> GetBybidAsync(Guid bid,int pageIndex,int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                var bidContainer= Query<IndexAct_usertreasure>.Term("bid", bid.ToString());
                var result = await _client.SearchAsync<IndexAct_usertreasure>(s => s.Index(_config.IndexName).Query(bidContainer).Skip(from).Take(size));
                if (result.Total >= 1)
                {
                    return result.Documents.ToList();
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
