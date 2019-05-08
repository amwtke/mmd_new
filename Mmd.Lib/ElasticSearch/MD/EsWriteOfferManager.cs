using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Log;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB;
using MD.Model.Index.MD;
using Nest;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsWriteOfferManager
    {
        static readonly object LockObject = new object();

        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsWriteOfferManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsWriteOfferConfig _config = null;
        static EsWriteOfferManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsWriteOfferConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsWriteOfferConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexWriteoffer>(_client, _config.IndexName, new IndexSettings()
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

        public static void MapAClient(ElasticClient client)
        {
            if (!ESHeper.BeSureMapping<IndexWriteoffer>(client, _config.IndexName, new IndexSettings()
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

        public static IndexWriteoffer GenObject(WriteOffer obj)
        {
            if (obj == null || obj.id == null)
                return null;
            IndexWriteoffer ret = new IndexWriteoffer()
            {
                Id = obj.uid.ToString(),
                mid = obj.mid.ToString(),
                openid = obj.openid,
                is_valid = obj.is_valid,
                woid = obj.woid.ToString(),
                realname = obj.realname,
                phone = obj.phone,
                commission = obj.commission
            };
            if (obj.timestamp != null) ret.timestamp = obj.timestamp.Value;
            return ret;
        }

        public static async Task<bool> AddOrUpdateAsync(IndexWriteoffer obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexWriteoffer>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexWriteoffer>((u) =>
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
        public static bool AddOrUpdate(IndexWriteoffer obj)
        {
            try
            {
                var result =  _client.Search<IndexWriteoffer>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r =  _client.Update<IndexWriteoffer>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                var resoponse =  _client.Index(obj, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static async Task<IndexWriteoffer> GetByUidAsync(Guid uid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexWriteoffer>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(uid))));
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
