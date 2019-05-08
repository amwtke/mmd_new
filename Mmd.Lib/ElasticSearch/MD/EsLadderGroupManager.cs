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
    public static class EsLadderGroupManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsLadderGroupManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsLadderGroupConfig _config = null;
        static EsLadderGroupManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsLadderGroupConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsLadderGroupConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexLadderGroup>(_client, _config.IndexName, new IndexSettings()
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
        public static IndexLadderGroup GenObject(LadderGroup group)
        {
            if (group != null)
            {
                IndexLadderGroup index = new IndexLadderGroup();
                index.Id = group.gid.ToString();
                index.mid = group.mid.ToString();
                index.pid = group.pid.ToString();
                index.title = group.title;
                index.description = group.description;
                index.pic = group.pic;
                index.waytoget = group.waytoget.Value;
                index.product_count = group.product_count;
                index.product_quotacount = group.product_quotacount;
                index.origin_price = group.origin_price;
                index.status = group.status;
                index.start_time = group.start_time;
                index.end_time = group.end_time;
                index.last_update_time = group.last_update_time;
                index.PriceList = group.PriceList.Select(p => new Model.Index.MD.LadderPrice() { person_count = p.person_count, group_price = p.group_price }).ToList();
                return index;
            }
            else
            {
                return null;
            }
        }
        public static async Task<bool> AddOrUpdateAsync(IndexLadderGroup obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexLadderGroup l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexLadderGroup>((u) =>
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

        public static IndexLadderGroup GetByGid(Guid gid)
        {
            try
            {
                var result = _client.Search<IndexLadderGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(gid.ToString()))));
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

        public static async Task<IndexLadderGroup> GetByGidAsync(Guid gid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(gid.ToString()))));
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
        public static async Task<Tuple<int,List<IndexLadderGroup>>> GetBymidAsync(Guid mid, List<int> status,int pageIndex,int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                var midContainer = Query<IndexLadderGroup>.Term("mid", mid.ToString());
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    var stContainer = Query<IndexLadderGroup>.Term("status", s);
                    statusContainer = statusContainer || stContainer;
                }
                midContainer = midContainer && statusContainer;

                var result = await _client.SearchAsync<IndexLadderGroup>(s => s.Index(_config.IndexName).Query(midContainer).SortDescending("last_update_time").Skip(from).Take(size));
                if (result == null)
                    return null;
                int totalCount = (int)result.Total;
                var list = result.Documents.ToList();
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        public static async Task<bool> DeleteAsync(string gid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(gid))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexLadderGroup>((u) =>
                    {
                        u.Id(_id);
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

        public static async Task<bool> DeleteAsync()
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderGroup>(s => s);
                var list = result.Documents.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    string _id = list[i].Id;
                    var r = await _client.DeleteAsync<IndexLadderGroup>((u) =>
                    {
                        u.Id(_id);
                        u.Index(_config.IndexName);
                        return u;
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }
    }
}
