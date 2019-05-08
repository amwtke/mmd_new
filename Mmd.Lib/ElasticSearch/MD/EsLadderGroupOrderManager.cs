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
    public static class EsLadderGroupOrderManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsLadderGroupOrderManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsLadderGroupOrderConfig _config = null;
        static EsLadderGroupOrderManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsLadderGroupOrderConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsLadderGroupOrderConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexLadderGroupOrder>(_client, _config.IndexName, new IndexSettings()
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
        public static async Task<bool> AddOrUpdateAsync(IndexLadderGroupOrder obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexLadderGroupOrder>((u) =>
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
        public static IndexLadderGroupOrder GenObject(LadderGroupOrder laddergrouporder)
        {
            if (laddergrouporder == null)
                return null;
            IndexLadderGroupOrder indexladdergo = new IndexLadderGroupOrder()
            {
                Id = laddergrouporder.goid.ToString(),
                go_no = laddergrouporder.go_no,
                gid = laddergrouporder.gid.ToString(),
                pid = laddergrouporder.pid.ToString(),
                mid = laddergrouporder.mid.ToString(),
                leader = laddergrouporder.leader.ToString(),
                expire_date = laddergrouporder.expire_date,
                price = laddergrouporder.price,
                status = laddergrouporder.status,
                create_date = laddergrouporder.create_date,
                go_price = laddergrouporder.go_price
            };
            return indexladdergo;
        }

        public static async Task<Tuple<int, List<IndexLadderGroupOrder>>> GetGroupOrderAsync(Guid gid, int status, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                QueryContainer Container = Query<IndexLadderGroupOrder>.Term("gid", gid);
                QueryContainer statusContainer = Query<IndexLadderGroupOrder>.Term("status", status);
                Container = Container && statusContainer;
                var result = await _client.SearchAsync<IndexLadderGroupOrder>(s => s.Index(_config.IndexName).Query(Container).SortAscending("create_date")
                .Skip(from).Take(size));

                var list = result.Documents.ToList();

                if (list.Count > 0)
                {
                    return Tuple.Create((int)result.Total, list);
                }
                return Tuple.Create(0, new List<IndexLadderGroupOrder>());
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        public static IndexLadderGroupOrder GetById(Guid goid)
        {
            try
            {
                var result = _client.Search<IndexLadderGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(goid))));
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

        public static async Task<IndexLadderGroupOrder> GetByIdAsync(Guid goid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(goid))));
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
