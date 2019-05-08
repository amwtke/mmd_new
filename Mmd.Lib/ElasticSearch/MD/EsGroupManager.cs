using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB;
using MD.Model.Index.MD;
using Nest;
using MD.Model.DB.Code;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsGroupManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsGroupManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsGroupConfig _config = null;
        static EsGroupManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsGroupConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsGroupConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexGroup>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexGroup>(client, _config.IndexName, new IndexSettings()
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

        public static async Task<IndexGroup> GenObject(Guid gid)
        {
            using (var repo = new BizRepository())
            {
                Group gObject = await repo.GroupGetGroupById(gid);
                Product pObject = await repo.GetProductByPidAsync(gObject.pid);

                if (gObject != null && pObject != null && !gObject.gid.Equals(Guid.Empty))
                {
                    IndexGroup ret = new IndexGroup()
                    {
                        advertise_pic_url = gObject.advertise_pic_url,
                        biz_type = gObject.biz_type,
                        description = gObject.description,
                        group_headpic_dir = gObject.group_headpic_dir,
                        Id = gObject.gid.ToString(),
                        last_update_user = gObject.last_update_user.ToString(),
                        title = gObject.title,
                        aaid = gObject.aaid.ToString(),
                        mid = gObject.mid.ToString(),
                        pid = gObject.pid.ToString(),
                        ltid = gObject.ltid.ToString(),
                        commission = gObject.Commission
                    };

                    if (gObject.group_end_time != null) ret.group_end_time = gObject.group_end_time.Value;
                    if (gObject.group_price != null) ret.group_price = gObject.group_price.Value;
                    if (gObject.group_start_time != null) ret.group_start_time = gObject.group_start_time.Value;
                    if (gObject.last_update_time != null) ret.last_update_time = gObject.last_update_time.Value;
                    if (gObject.time_limit != null) ret.time_limit = gObject.time_limit.Value;
                    if (gObject.status != null) ret.status = gObject.status.Value;
                    if (gObject.waytoget != null) ret.waytoget = gObject.waytoget.Value;
                    if (gObject.isshowpting != null) ret.isshowpting = gObject.isshowpting.Value;
                    if (gObject.origin_price != null) ret.origin_price = gObject.origin_price.Value;
                    if (gObject.person_quota != null) ret.person_quota = gObject.person_quota.Value;
                    if (gObject.product_quota != null) ret.product_quota = gObject.product_quota.Value;
                    if (gObject.product_setting_count != null) ret.product_setting_count = gObject.product_setting_count.Value;
                    if (pObject.p_no != null) ret.p_no = pObject.p_no.Value;
                    if (gObject.group_type != null) ret.group_type = gObject.group_type.Value;
                    ret.KeyWords = ret.title + "," + ret.description + "," + pObject.p_no;
                    return ret;
                }
            }
            return null;
        }

        public static IndexGroup GenObject_TB(Guid gid)
        {
            using (var repo = new BizRepository())
            {
                Group gObject = repo.GroupGetGroupById_TB(gid);
                if (gObject == null)
                    return null;
                Product pObject = repo.GetProductByPid(gObject.pid);

                if (pObject != null && !gObject.gid.Equals(Guid.Empty))
                {
                    IndexGroup ret = new IndexGroup()
                    {
                        advertise_pic_url = gObject.advertise_pic_url,
                        biz_type = gObject.biz_type,
                        description = gObject.description,
                        group_headpic_dir = gObject.group_headpic_dir,
                        Id = gObject.gid.ToString(),
                        last_update_user = gObject.last_update_user.ToString(),
                        title = gObject.title,
                        aaid = gObject.aaid.ToString(),
                        mid = gObject.mid.ToString(),
                        pid = gObject.pid.ToString(),
                        commission = gObject.Commission
                    };

                    if (gObject.group_end_time != null) ret.group_end_time = gObject.group_end_time.Value;
                    if (gObject.group_price != null) ret.group_price = gObject.group_price.Value;
                    if (gObject.group_start_time != null) ret.group_start_time = gObject.group_start_time.Value;
                    if (gObject.last_update_time != null) ret.last_update_time = gObject.last_update_time.Value;
                    if (gObject.time_limit != null) ret.time_limit = gObject.time_limit.Value;
                    if (gObject.status != null) ret.status = gObject.status.Value;
                    if (gObject.waytoget != null) ret.waytoget = gObject.waytoget.Value;
                    if (gObject.isshowpting != null) ret.isshowpting = gObject.isshowpting.Value;
                    if (gObject.origin_price != null) ret.origin_price = gObject.origin_price.Value;
                    if (gObject.person_quota != null) ret.person_quota = gObject.person_quota.Value;
                    if (gObject.product_quota != null) ret.product_quota = gObject.product_quota.Value;
                    if (gObject.product_setting_count != null) ret.product_setting_count = gObject.product_setting_count.Value;
                    if (pObject.p_no != null) ret.p_no = pObject.p_no.Value;
                    if (gObject.group_type != null) ret.group_type = gObject.group_type.Value;
                    ret.KeyWords = ret.title + "," + ret.description + "," + pObject.p_no;
                    return ret;
                }
            }
            return null;
        }

        public static async Task<bool> AddOrUpdateAsync(IndexGroup obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexGroup l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexGroup>((u) =>
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
        /// 用于迁移数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool AddOrUpdate(ElasticClient client, IndexGroup obj)
        {
            try
            {
                var result = client.Search<IndexGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexGroup l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = client.Update<IndexGroup>((u) =>
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

        public static async Task<bool> DeleteAsync(IndexGroup obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexGroup>((u) =>
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

        public static async Task<IndexGroup> GetByGidAsync(Guid gid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(gid.ToString()))));
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
        public static IndexGroup GetByGid(Guid gid)
        {
            try
            {
                var result = _client.Search<IndexGroup>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(gid.ToString()))));
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

        public static async Task<Tuple<int, List<Guid>>> SearchAsnyc(string queryStr, int status, Guid mid, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexGroup>.MatchAll();
                }
                else
                {
                    container = Query<IndexGroup>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索

                var statusContainer = Query<IndexGroup>.Term("status", status);
                var midContainer = Query<IndexGroup>.Term("mid", mid.ToString());
                container = container && statusContainer && midContainer;

                //search
                var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(container).SortDescending("last_update_time")
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

                    int totalCount = (int)result.Total;//await GetSearchTotalCountAsync(queryStr, mid,status);
                    return Tuple.Create(totalCount, ret);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<Guid>());
        }

        public static async Task<Tuple<int, List<IndexGroup>>> SearchAsnyc2(string queryStr, int status, Guid mid, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexGroup>.MatchAll();
                }
                else
                {
                    container = Query<IndexGroup>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                .OnFields(new[] { "KeyWords" })
                .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                var statusContainer = Query<IndexGroup>.Term("status", status);
                var midContainer = Query<IndexGroup>.Term("mid", mid.ToString());
                container = container && statusContainer && midContainer;

                //search
                var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(container).SortDescending("last_update_time").Skip(from).Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;//await GetSearchTotalCountAsync(queryStr, mid, status);
                    return Tuple.Create(totalCount, list);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexGroup>());
        }


        public static async Task<Tuple<int, List<IndexGroup>>> GetByMidAsync(Guid mid, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                var midContainer = Query<IndexGroup>.Term("mid", mid.ToString());
                var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(midContainer).SortDescending("last_update_time").Skip(from).Take(size));
                if (result == null)
                    return null;
                int totalCount = (int)result.Total;
                var list = result.Documents.ToList();
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(EsGroupManager), ex);
            }
        }

        public static async Task<Tuple<int, List<IndexGroup>>> GetByMidAsync(Guid mid, List<int> status, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                var midContainer = Query<IndexGroup>.Term("mid", mid.ToString());
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    var stContainer = Query<IndexGroup>.Term("status", s);
                    statusContainer = statusContainer || stContainer;
                }
                midContainer = midContainer && statusContainer;

                var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(midContainer).SortDescending("last_update_time").Skip(from).Take(size));
                if (result == null)
                    return null;
                int totalCount = (int)result.Total;
                var list = result.Documents.ToList();
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(EsGroupManager), new Exception($"fun:GetByMidAsync(Guid mid, List<int> status, int pageIndex, int pageSize),ex:{ex.Message}"));
                return null;
            }
        }
        public static async Task<Tuple<int, List<IndexGroup>>> GetFXGroupByMidAsync(Guid mid, List<int> status, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                var midContainer = Query<IndexGroup>.Term("mid", mid.ToString());
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    var stContainer = Query<IndexGroup>.Term("status", s);
                    statusContainer = statusContainer || stContainer;
                }
                midContainer = midContainer && statusContainer;
                var CommContainer = Query<IndexGroup>.Range(r => r.Greater(0).OnField(f => f.commission));
                midContainer = midContainer && CommContainer;
                var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(midContainer).SortDescending("last_update_time").Skip(from).Take(size));
                if (result == null)
                    return null;
                int totalCount = (int)result.Total;
                var list = result.Documents.ToList();
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(EsGroupManager), new Exception($"fun:GetByMidAsync(Guid mid, List<int> status, int pageIndex, int pageSize),ex:{ex.Message}"));
                return null;
            }
        }

        public static async Task<Tuple<long, List<string>>> GetByMidAsync(Guid mid, List<int> status, double timeStart, double timeEnd)
        {
            var midContainer = Query<IndexGroup>.Term("mid", mid.ToString());
            QueryContainer statusContainer = null;
            foreach (var s in status)
            {
                var stContainer = Query<IndexGroup>.Term("status", s);
                statusContainer = statusContainer || stContainer;
            }
            var timeContainer = Query<IndexGroup>.Range(r => r.OnField(f => f.last_update_time).GreaterOrEquals(timeStart).LowerOrEquals(timeEnd));
            var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(midContainer && statusContainer && timeContainer)
            .Aggregations(a => a.Terms("gids", sa => sa.Field(g => g.Id).Size(9999999))));
            var agg = result.Aggs.Terms("gids");
            long count = agg.Items.Count;
            List<string> gids = agg.Items.Select(a => a.Key).ToList();
            return Tuple.Create(count, gids);
            //var list = result.Documents.ToList();
            //List<string> gids = new List<string>();
            //foreach (var item in list)
            //{
            //    gids.Add(item.Id);
            //}
            //return Tuple.Create(result.Total, gids);
        }

        public static Tuple<int, List<IndexGroup>> GetByMid(Guid mid, List<int> status, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                var midContainer = Query<IndexGroup>.Term("mid", mid.ToString());
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    var stContainer = Query<IndexGroup>.Term("status", s);
                    statusContainer = statusContainer || stContainer;
                }
                midContainer = midContainer && statusContainer;

                var result = _client.Search<IndexGroup>(s => s.Index(_config.IndexName).Query(midContainer).SortDescending("last_update_time").Skip(from).Take(size));
                if (result == null)
                    return null;
                int totalCount = (int)result.Total;
                var list = result.Documents.ToList();
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(EsGroupManager), ex);
            }
        }

        public static async Task<long> GetCountByMidAsync(Guid mid, List<int> status, double timeStart, double timeEnd)
        {
            var midContainer = Query<IndexGroup>.Term("mid", mid.ToString());
            QueryContainer statusContainer = null;
            foreach (var s in status)
            {
                var stContainer = Query<IndexGroup>.Term("status", s);
                statusContainer = stContainer || stContainer;
            }
            var timeContainer = Query<IndexGroup>.Range(r => r.GreaterOrEquals(timeStart).OnField(f => f.last_update_time));
            timeContainer = timeContainer && Query<IndexGroup>.Range(r => r.LowerOrEquals(timeEnd).OnField(f => f.last_update_time));
            var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(midContainer && statusContainer && timeContainer).Take(0));
            return result.Total;
        }

        public static async Task<Tuple<int, List<IndexGroup>>> getGroupsAsync(List<Guid> midList, List<int> status, int pageIndex = 1, int pageSize = 20, double? f = null, double? t = null)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                QueryContainer midListContainer = null;//总条件
                foreach (var mid in midList)
                {
                    var midContainer = Query<IndexGroup>.Term("mid", mid);
                    midListContainer = midListContainer || midContainer;
                }
                //按状态查询
                if (status != null && status.Count > 0)
                {
                    QueryContainer statusContainer = null;
                    foreach (var s in status)
                    {
                        var stContainer = Query<IndexGroup>.Term("status", s);
                        statusContainer = statusContainer || stContainer;
                    }
                    midListContainer = midListContainer && statusContainer;
                }
                //按时间段查询
                if (f != null && t != null)
                {
                    var timeContainer = Query<IndexGroup>.Range(r => r.OnField("group_start_time").GreaterOrEquals(f).LowerOrEquals(t));
                    midListContainer = midListContainer && timeContainer;
                }

                var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(midListContainer).SortDescending("last_update_time").Skip(from).Take(size));
                int totalCount = (int)result.Total;
                var list = result.Documents.ToList();
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(EsGroupManager), ex);
            }
        }
        /// <summary>
        /// 根据商品pid、团状态查询
        /// </summary>
        /// <param name="pid">pid</param>
        /// <param name="status">status</param>
        /// <param name="isPTing">true:返回正在拼团中的第一个团，false:返回默认第一个团</param>
        /// <returns>返回根据last_update_time倒叙的第一个团</returns>
        public static async Task<IndexGroup> GetByPidAsync(Guid pid, List<int> status, bool isPTing)
        {
            try
            {
                var pidContainer = Query<IndexGroup>.Term("pid", pid);
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    var stContainer = Query<IndexGroup>.Term("status", s);
                    statusContainer = statusContainer || stContainer;
                }
                pidContainer = pidContainer && statusContainer;

                var result = await _client.SearchAsync<IndexGroup>(s => s.Index(_config.IndexName).Query(pidContainer).SortDescending("last_update_time"));
                if (result.Total >= 1)
                {
                    if (isPTing)
                    {
                        List<IndexGroup> retIndexGroup = new List<IndexGroup>();
                        using (var repoCode = new AttRepository())
                        {
                            foreach (var indexgroup in result.Documents)
                            {
                                if (indexgroup.group_type == (int)EGroupTypes.抽奖团)
                                {
                                    //如果是抽奖团，需要把已开奖的排除
                                    var attname = await repoCode.AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.lucky_status.ToString());
                                    var attvalue = await repoCode.AttValueGetAsync(Guid.Parse(indexgroup.Id), attname.attid);
                                    if (attvalue.value == ((int)EGroupLuckyStatus.已开奖).ToString())
                                        continue;
                                }
                                retIndexGroup.Add(indexgroup);
                            }
                        }
                        return retIndexGroup.FirstOrDefault();
                    }
                    else
                    {
                        return result.Documents.FirstOrDefault();
                    }

                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(EsGroupManager), ex);
            }
            return null;
        }
    }
}
