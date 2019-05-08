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
    public static class EsWriteOffPointManager
    {
        static readonly object LockObject = new object();

        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsWriteOffPointManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsWriteOffPointConfig _config = null;
        static EsWriteOffPointManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsWriteOffPointConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsWriteOffPointConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexWriteOffPoint>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexWriteOffPoint>(client, _config.IndexName, new IndexSettings()
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

        public static IndexWriteOffPoint GenObject(WriteOffPoint obj)
        {
            if (obj == null || obj.woid.Equals(Guid.Empty))
                return null;
            IndexWriteOffPoint ret = new IndexWriteOffPoint()
            {
                address = obj.address,
                cell_phone = obj.cell_phone,
                contact_person = obj.contact_person,
                Id = obj.woid.ToString(),
                mid = obj.mid.ToString(),
                parent = obj.parent.ToString(),
                tel = obj.tel,
                name = obj.name,
                is_valid = obj.is_valid,
                timestamp = obj.timestamp,
                longitude = obj.longitude,
                latitude = obj.latitude,
                KeyWords = obj.name + "," + obj.address + "," + obj.cell_phone + "," + obj.tel + "," + obj.contact_person
            };
            return ret;
        }

        public static async Task<bool> AddOrUpdateAsync(IndexWriteOffPoint obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexWriteOffPoint>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexWriteOffPoint l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexWriteOffPoint>((u) =>
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

        /// <summary>
        /// 用于数据迁移
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool AddOrUpdate(ElasticClient Client,IndexWriteOffPoint obj)
        {
            try
            {
                var result = Client.Search<IndexWriteOffPoint>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexWriteOffPoint l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = Client.Update<IndexWriteOffPoint>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                var resoponse = Client.Index(l, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static async Task<bool> DeleteAsync(IndexWriteOffPoint obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexWriteOffPoint>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexWriteOffPoint>((u) =>
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

        public static async Task<IndexWriteOffPoint> GetByIdAsync(Guid woId)
        {
            try
            {
                var result = await _client.SearchAsync<IndexWriteOffPoint>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(woId))));
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

        public static async Task<Tuple<int, List<IndexWriteOffPoint>>> SearchAsnyc(string queryStr, Guid mid, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexWriteOffPoint>.MatchAll();
                }
                else
                {
                    container =
                        Query<IndexWriteOffPoint>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                            .OnFields(new[] { "KeyWords" })
                            .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                var midContainer = Query<IndexWriteOffPoint>.Term("mid", mid.ToString());
                container = container && midContainer;

                var validContain = Query<IndexWriteOffPoint>.Term("is_valid", true);
                container = container && validContain;
                //search
                var result =
                    await
                        _client.SearchAsync<IndexWriteOffPoint>(
                            s =>
                                s.Index(_config.IndexName)
                                    .Query(container)
                                    .SortDescending("timestamp")
                                    .Skip(from)
                                    .Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexWriteOffPoint>());
        }

        public static async Task<Tuple<int, List<IndexWriteOffPoint>>> SearchAsnyc(string queryStr, Guid mid)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexWriteOffPoint>.MatchAll();
                }
                else
                {
                    container =
                        Query<IndexWriteOffPoint>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                            .OnFields(new[] { "KeyWords" })
                            .Analyzer("ik_smart"));
                }

                //主搜索
                var midContainer = Query<IndexWriteOffPoint>.Term("mid", mid.ToString());
                container = container && midContainer;

                var validContain = Query<IndexWriteOffPoint>.Term("is_valid", true);
                container = container && validContain;
                //search
                var result =
                    await
                        _client.SearchAsync<IndexWriteOffPoint>(
                            s =>
                                s.Index(_config.IndexName)
                                    .Query(container)
                                    .SortDescending("timestamp").Take(10000));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexWriteOffPoint>());
        }

        public static async Task<Tuple<int, List<IndexWriteOffPoint>>> SearchAsnyc(string queryStr, Guid mid, List<Guid> woidList)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexWriteOffPoint>.MatchAll();
                }
                else
                {
                    container =
                        Query<IndexWriteOffPoint>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                            .OnFields(new[] { "KeyWords" })
                            .Analyzer("ik_smart"));
                }

                //主搜索
                var midContainer = Query<IndexWriteOffPoint>.Term("mid", mid.ToString());
                container = container && midContainer;

                var validContain = Query<IndexWriteOffPoint>.Term("is_valid", true);
                container = container && validContain;
                //woidList
                if (woidList != null && woidList.Count > 0)
                {
                    QueryContainer woidAllContain = null;
                    foreach (var woid in woidList)
                    {
                        var wContain = Query<IndexWriteOffPoint>.Term("Id", woid);
                        woidAllContain = woidAllContain || wContain;
                    }
                    container = container && woidAllContain;
                }
                //search
                var result =
                    await
                        _client.SearchAsync<IndexWriteOffPoint>(
                            s =>
                                s.Index(_config.IndexName)
                                    .Query(container)
                                    .SortDescending("timestamp").Take(10000));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexWriteOffPoint>());
        }

        public static async Task<long> GetCountByMidAsync(Guid mid)
        {
            var container = Query<IndexWriteOffPoint>.Term("mid", mid.ToString());
            var container2 = Query<IndexWriteOffPoint>.Term("is_valid", true);
            var result =
                await
                    _client.SearchAsync<IndexWriteOffPoint>(s => s.Index(_config.IndexName).Query(container && container2).Take(0));
            long d = result.Total;
            return Convert.ToInt32(d);

        }
    }
}
