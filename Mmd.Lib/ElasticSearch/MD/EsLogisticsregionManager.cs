using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsLogisticsregionManager
    {
        static readonly object LockObject = new object();

        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsLogisticsregionManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsLogisticsregionConfig _config = null;
        static EsLogisticsregionManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsLogisticsregionConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsLogisticsregionConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<Indexlogisticsregion>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<Indexlogisticsregion>(client, _config.IndexName, new IndexSettings()
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

        public static Indexlogisticsregion GenObject(Logistics_Region lr)
        {
            if (lr != null)
            {
                Indexlogisticsregion indexlr = new Indexlogisticsregion()
                {
                    Id = lr.lid,
                    name = lr.name,
                    orderId = lr.orderId,
                    categoryLevel = lr.categoryLevel,
                    fatherId = lr.fatherId,
                    code = lr.code,
                    isDeleted = lr.isDeleted
                };
                return indexlr;
            }
            return null;
        }
        public static async Task<bool> AddOrUpdateAsync(Indexlogisticsregion obj)
        {
            try
            {
                var result = await _client.SearchAsync<Indexlogisticsregion>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));

                Indexlogisticsregion l = obj;
                //更新
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<Indexlogisticsregion>((u) =>
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

        public static async Task<List<Indexlogisticsregion>> GetAllAsync()
        {
            try
            {
                var result = await _client.SearchAsync<Indexlogisticsregion>(s => s.Index(_config.IndexName).MatchAll().SortAscending(p=>p.orderId).Take(4000));
                return result.Documents.ToList();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }
    }
}
