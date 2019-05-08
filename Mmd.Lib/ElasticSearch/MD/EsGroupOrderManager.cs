using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Log;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using Nest;
using MD.Lib.Util;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsGroupOrderManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsGroupOrderManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsGroupOrderConfig _config = null;
        static EsGroupOrderManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsGroupOrderConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsGroupOrderConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexGroupOrder>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexGroupOrder>(client, _config.IndexName, new IndexSettings()
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="keyWords">可以不填</param>
        /// <returns></returns>
        public static IndexGroupOrder GenObject(GroupOrder obj, string keyWords)
        {
            if (obj != null && !obj.goid.Equals(Guid.Empty))
            {
                IndexGroupOrder ret = new IndexGroupOrder()
                {
                    Id = obj.goid.ToString(),
                    create_date = obj.create_date,
                    go_no = obj.go_no,
                    gid = obj.gid.ToString(),
                    leader = obj.leader.ToString(),
                    pid = obj.pid.ToString(),
                    price = obj.price,
                    status = obj.status,
                    user_left = obj.user_left,
                    go_price = obj.go_price,
                    expire_date = obj.expire_date,
                    KeyWords = keyWords
                };
                return ret;
            }
            return null;
        }

        public static IndexGroupOrder GenObject(GroupOrder obj)
        {
            if (obj != null && !obj.goid.Equals(Guid.Empty))
            {
                string keyWords = "";
                var index = GetById(obj.goid);
                if (index != null)
                {
                    keyWords = index.KeyWords;
                }

                IndexGroupOrder ret = new IndexGroupOrder()
                {
                    Id = obj.goid.ToString(),
                    create_date = obj.create_date,
                    go_no = obj.go_no,
                    gid = obj.gid.ToString(),
                    leader = obj.leader.ToString(),
                    pid = obj.pid.ToString(),
                    price = obj.price,
                    status = obj.status,
                    user_left = obj.user_left,
                    go_price = obj.go_price,
                    expire_date = obj.expire_date,
                    KeyWords = keyWords
                };
                return ret;
            }
            return null;
        }

        public static async Task<IndexGroupOrder> GenObjectAsync(GroupOrder obj)
        {
            if (obj != null && !obj.goid.Equals(Guid.Empty))
            {
                string keyWords = "";
                var index = await GetByIdAsync(obj.goid);
                if (index != null)
                {
                    keyWords = index.KeyWords;
                }

                IndexGroupOrder ret = new IndexGroupOrder()
                {
                    Id = obj.goid.ToString(),
                    create_date = obj.create_date,
                    go_no = obj.go_no,
                    gid = obj.gid.ToString(),
                    leader = obj.leader.ToString(),
                    pid = obj.pid.ToString(),
                    price = obj.price,
                    status = obj.status,
                    user_left = obj.user_left,
                    go_price = obj.go_price,
                    expire_date = obj.expire_date,
                    KeyWords = keyWords
                };
                return ret;
            }
            return null;
        }


        public static async Task<bool> AddOrUpdateAsync(IndexGroupOrder obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexGroupOrder l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexGroupOrder>((u) =>
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
        /// 用于导数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool AddOrUpdate(ElasticClient client,IndexGroupOrder obj)
        {
            try
            {
                var result = client.Search<IndexGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexGroupOrder l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = client.Update<IndexGroupOrder>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                var resoponse = client.Index(l, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static bool AddOrUpdate(IndexGroupOrder obj)
        {
            try
            {
                var result = _client.Search<IndexGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexGroupOrder l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = _client.Update<IndexGroupOrder>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                var resoponse = _client.Index(l, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static async Task<bool> DeleteAsync(IndexGroupOrder obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexGroupOrder>((u) =>
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

        public static async Task<IndexGroupOrder> GetByIdAsync(Guid goid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(goid))));
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

        public static IndexGroupOrder GetById(Guid goid)
        {
            try
            {
                var result = _client.Search<IndexGroupOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(goid))));
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

        public static Tuple<int, List<IndexGroupOrder>> GetByGid(Guid gid, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                QueryContainer qc = Query<IndexGroupOrder>.Term("gid", gid);

                var result = _client.Search<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(qc).SortDescending("create_date")
               .Skip(from).Take(size));

                var list = result.Documents.ToList();

                if (list.Count > 0)
                {
                    return Tuple.Create((int)result.Total, list);
                }
                return Tuple.Create((int)result.Total, new List<IndexGroupOrder>());
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        public static async Task<int> GetByGidCountAsync(List<int> status, Guid gid)
        {
            try
            {
                //主搜索
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    if (statusContainer == null)
                        statusContainer = Query<IndexGroupOrder>.Term("status", s);
                    else
                        statusContainer = statusContainer || Query<IndexGroupOrder>.Term("status", s);
                }

                var gidContainer = Query<IndexGroupOrder>.Term("gid", gid.ToString());

                //search
                var result =
                    await
                        _client.SearchAsync<IndexGroupOrder>(
                            s => s.Index(_config.IndexName).Query(statusContainer && gidContainer));

                int totalCount = (int)result.Total;
                return totalCount;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return 0;
        }

        /// <summary>
        /// 获取团下的团订单。
        /// </summary>
        /// <param name="gid"></param>
        /// <returns>总数+list</returns>
        public static async Task<Tuple<int, List<IndexGroupOrder>>> GetByGidAsync(Guid gid, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                QueryContainer qc = Query<IndexGroupOrder>.Term("gid", gid);

                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(qc).SortDescending("create_date")
                .Skip(from).Take(size));

                var list = result.Documents.ToList();

                if (list.Count > 0)
                {
                    return Tuple.Create((int)result.Total, list);
                }
                return Tuple.Create((int)result.Total, new List<IndexGroupOrder>());
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        public static async Task<Tuple<int, List<IndexGroupOrder>>> GetByGidAsync(Guid gid, EGroupOrderStatus Estatus, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                int status = (int)Estatus;

                QueryContainer qc = Query<IndexGroupOrder>.Term("gid", gid);
                QueryContainer statusQc = Query<IndexGroupOrder>.Term("status", status);
                qc = qc && statusQc;

                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(qc).SortDescending("create_date")
                .Skip(from).Take(size));

                var list = result.Documents.ToList();

                if (list.Count > 0)
                {
                    return Tuple.Create((int)result.Total, list);
                }
                return Tuple.Create((int)result.Total, new List<IndexGroupOrder>());
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        public static async Task<Tuple<int, List<IndexGroupOrder>>> GetByGidAsync2(Guid gid, List<int> statuses, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                QueryContainer qc = Query<IndexGroupOrder>.Term("gid", gid);
                QueryContainer statusQc = null;
                foreach (var s in statuses)
                {
                    if (statusQc == null)
                    {
                        statusQc = Query<IndexGroupOrder>.Term("status", s);
                    }
                    else
                    {
                        statusQc = statusQc || Query<IndexGroupOrder>.Term("status", s);
                    }
                }

                qc = qc && statusQc;

                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(qc).SortDescending("create_date")
                .Skip(from).Take(size));

                var list = result.Documents.ToList();

                if (list.Count > 0)
                {
                    return Tuple.Create((int)result.Total, list);
                }
                return Tuple.Create((int)result.Total, new List<IndexGroupOrder>());
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        /// <summary>
        /// 获取团订单信息(过期时间必须大于当前时间！)
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="Estatus"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="SortStatus">0：倒叙，1：正序</param>
        /// <returns></returns>
        public static Tuple<int, List<IndexGroupOrder>> GetByGid(Guid gid, EGroupOrderStatus Estatus, int pageIndex, int pageSize, int SortStatus)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                int status = (int)Estatus;
                double timeNow = CommonHelper.GetUnixTimeNow();

                QueryContainer qc = Query<IndexGroupOrder>.Term("gid", gid);
                QueryContainer statusQc = Query<IndexGroupOrder>.Term("status", status);
                QueryContainer time = Query<IndexGroupOrder>.Range(s => s.OnField("expire_date").Greater(timeNow));


                qc = qc && statusQc && time;
                ISearchResponse<IndexGroupOrder> result = null;
                if (SortStatus == 0)
                {
                    result = _client.Search<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(qc).SortDescending("create_date")
                    .Skip(from).Take(size));
                }
                else
                {
                    result = _client.Search<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(qc).SortAscending("create_date")
                    .Skip(from).Take(size));
                }

                var list = result.Documents.ToList();

                if (list.Count > 0)
                {
                    return Tuple.Create((int)result.Total, list);
                }
                return Tuple.Create((int)result.Total, new List<IndexGroupOrder>());
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        /// <summary>
        /// 通用的
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="Estatus"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static Tuple<int, List<IndexGroupOrder>> GetByGid2(Guid gid, EGroupOrderStatus Estatus, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                int status = (int)Estatus;
                double timeNow = CommonHelper.GetUnixTimeNow();

                QueryContainer qc = Query<IndexGroupOrder>.Term("gid", gid);
                QueryContainer statusQc = Query<IndexGroupOrder>.Term("status", status);

                qc = qc && statusQc;
                ISearchResponse<IndexGroupOrder> result = null;

                result =
                    _client.Search<IndexGroupOrder>(
                        s => s.Index(_config.IndexName).Query(qc).SortAscending("create_date")
                            .Skip(from).Take(size));

                var list = result.Documents.ToList();

                if (list.Count > 0)
                {
                    return Tuple.Create((int)result.Total, list);
                }
                return Tuple.Create((int)result.Total, new List<IndexGroupOrder>());
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }
        public static Tuple<int, List<IndexGroupOrder>> GetByGid2(Guid gid, List<int> statuses, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                QueryContainer qc = Query<IndexGroupOrder>.Term("gid", gid);
                QueryContainer statusQc = null;
                foreach (var s in statuses)
                {
                    if (statusQc == null)
                    {
                        statusQc = Query<IndexGroupOrder>.Term("status", s);
                    }
                    else
                    {
                        statusQc = statusQc || Query<IndexGroupOrder>.Term("status", s);
                    }
                }
                qc = qc && statusQc;

                var result = _client.Search<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(qc).SortDescending("create_date")
                .Skip(from).Take(size));

                var list = result.Documents.ToList();

                if (list.Count > 0)
                {
                    return Tuple.Create((int)result.Total, list);
                }
                return Tuple.Create((int)result.Total, new List<IndexGroupOrder>());
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        public static async Task<Tuple<int, List<Guid>>> SearchAsnyc(string queryStr, int status, Guid gid, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexGroupOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexGroupOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索

                var statusContainer = Query<IndexGroupOrder>.Term("status", status);
                var midContainer = Query<IndexGroupOrder>.Term("gid", gid.ToString());
                container = container && statusContainer && midContainer;

                //search
                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(container).SortDescending("create_date")
                //.SortDescending("AccessCount")
                .Skip(from).Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    List<Guid> ret = new List<Guid>();
                    foreach (var p in list)
                    {
                        ret.Add(Guid.Parse(p.Id));
                    }

                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, ret);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<Guid>());
        }

        public static async Task<Tuple<int, List<Guid>>> SearchAsnyc(string queryStr, int status, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexGroupOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexGroupOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索

                var statusContainer = Query<IndexGroupOrder>.Term("status", status);
                //var midContainer = Query<IndexGroupOrder>.Term("gid", gid.ToString());
                container = container && statusContainer; //&& midContainer;

                //search
                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(container).SortDescending("create_date")
                //.SortDescending("AccessCount")
                .Skip(from).Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    List<Guid> ret = new List<Guid>();
                    foreach (var p in list)
                    {
                        ret.Add(Guid.Parse(p.Id));
                    }

                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, ret);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<Guid>());
        }

        public static async Task<Tuple<int, List<IndexGroupOrder>>> GetBypid(Guid pid, int status, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                QueryContainer qc = Query<IndexGroupOrder>.Term("pid", pid);
                QueryContainer statusQc = null;
                statusQc = Query<IndexGroupOrder>.Term("status", status);
                qc = qc && statusQc;

                var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(qc).SortDescending("create_date").Skip(from).Take(pageSize));
                int totalCount = (int)result.Total;
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    return Tuple.Create(totalCount, list);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexGroupOrder>());
        }

        public static async Task<long> GetCountByGidsAsync(List<string> gids, List<int> status, double timeStart, double timeEnd)
        {
            QueryContainer statusContainer = null;
            foreach (var s in status)
            {
                var stContainer = Query<IndexGroupOrder>.Term("status", s);
                statusContainer = statusContainer || stContainer;
            }
            QueryContainer gidsContainer = null;
            foreach (var gid in gids)
            {
                var stContainer = Query<IndexGroupOrder>.Term("gid", gid);
                gidsContainer = gidsContainer || stContainer;
            }
            var timeContainer = Query<IndexGroupOrder>.Range(r => r.OnField(f => f.create_date).GreaterOrEquals(timeStart).LowerOrEquals(timeEnd));
            var result = await _client.SearchAsync<IndexGroupOrder>(s => s.Index(_config.IndexName).Query(statusContainer && gidsContainer && timeContainer));
            return result.Total;
        }
    }
}
