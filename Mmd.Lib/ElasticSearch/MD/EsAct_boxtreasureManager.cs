using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
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
    public static class EsAct_boxtreasureManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsAct_boxtreasureManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsAct_boxtreasureConfig _config = null;
        static EsAct_boxtreasureManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsAct_boxtreasureConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsAct_boxtreasureConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexAct_boxtreasure>(_client, _config.IndexName, new IndexSettings()
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

        public static IndexAct_boxtreasure GenObject(BoxTreasure bt)
        {
            if (bt != null)
            {
                IndexAct_boxtreasure index = new IndexAct_boxtreasure();
                index.Id = bt.btid.ToString();
                index.bid = bt.bid.ToString();
                index.name = bt.name;
                index.count = bt.count;
                index.quota_count = bt.quota_count;
                index.description = bt.description;
                index.pic = bt.pic;
                return index;
            }
            else
            {
                return null;
            }
        }
        public static async Task<IndexAct_boxtreasure> GenObjectAsync(Guid btid)
        {
            using (var acti = new ActivityRepository())
            {
                var db_bt = await acti.GetBoxTreasureByIdAsync(btid);
                if (db_bt != null)
                {
                    IndexAct_boxtreasure index = new IndexAct_boxtreasure();
                    index.Id = db_bt.btid.ToString();
                    index.bid = db_bt.bid.ToString();
                    index.name = db_bt.name;
                    index.count = db_bt.count;
                    index.quota_count = db_bt.quota_count;
                    index.description = db_bt.description;
                    index.pic = db_bt.pic;
                    return index;
                }
                return null;
            }
        }
        public static async Task<bool> AddOrUpdateAsync(IndexAct_boxtreasure obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_boxtreasure>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexAct_boxtreasure l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexAct_boxtreasure>((u) =>
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

        public static async Task<List<IndexAct_boxtreasure>> GetBybidAsync(Guid bid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_boxtreasure>(s => s.Query(q => q.Term(t => t.OnField("bid").Value(bid.ToString()))));
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
        public static async Task<IndexAct_boxtreasure> GetBybtidAsync(Guid btid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_boxtreasure>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(btid.ToString()))));
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
    }
}
