using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Model.Configuration.ElasticSearch;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.Index;
using MD.Model.Index.MD;
using Nest;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsProductManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsProductManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsProductConfig _config = null;
        static EsProductManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsProductConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsProductConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexProduct>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexProduct>(client, _config.IndexName, new IndexSettings()
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
        /// keywords=p_no+","+name
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static async Task<IndexProduct> GenObject(Guid pid)
        {
            using (var repo = new BizRepository())
            {
                Product pObject = await repo.GetProductByPidAsync(pid);
                if (pObject != null && !pObject.pid.Equals(Guid.Empty))
                {
                    IndexProduct ret = new IndexProduct();

                    ret.Id = pObject.pid.ToString();

                    if (pObject.p_no != null) ret.p_no = pObject.p_no.Value;

                    if (pObject.category != null) ret.category = pObject.category.Value;

                    ret.description = pObject.description;

                    ret.mid = pObject.mid.ToString();

                    if (pObject.price != null) ret.price = pObject.price.Value;

                    ret.name = pObject.name;

                    if (pObject.timestamp != null) ret.timestamp = pObject.timestamp.Value;

                    if (pObject.status != null) ret.status = pObject.status.Value;

                    if (pObject.avgScore != null) ret.avgScore = pObject.avgScore.Value;
                    if (pObject.scorePeopleCount != null) ret.scorePeopleCount = pObject.scorePeopleCount.Value;
                    if (pObject.grassCount != null) ret.grassCount = pObject.grassCount.Value;

                    ret.standard = pObject.standard;
                    ret.advertise_pic_1 = pObject.advertise_pic_1;
                    ret.advertise_pic_2 = pObject.advertise_pic_2;
                    ret.advertise_pic_3 = pObject.advertise_pic_3;
                    ret.aaid = pObject.aaid.ToString();
                    ret.last_update_user = pObject.last_update_user.ToString();

                    ret.KeyWords = ret.p_no + "," + ret.name;
                    return ret;
                }
            }
            return null;
        }
        public static IndexProduct GenObject(Product pObject)
        {
            if (pObject != null && !pObject.pid.Equals(Guid.Empty))
            {
                IndexProduct ret = new IndexProduct();

                ret.Id = pObject.pid.ToString();

                if (pObject.p_no != null) ret.p_no = pObject.p_no.Value;

                if (pObject.category != null) ret.category = pObject.category.Value;

                ret.description = pObject.description;

                ret.mid = pObject.mid.ToString();

                if (pObject.price != null) ret.price = pObject.price.Value;

                ret.name = pObject.name;

                if (pObject.timestamp != null) ret.timestamp = pObject.timestamp.Value;

                if (pObject.status != null) ret.status = pObject.status.Value;

                if (pObject.avgScore != null) ret.avgScore = pObject.avgScore.Value;
                if (pObject.scorePeopleCount != null) ret.scorePeopleCount = pObject.scorePeopleCount.Value;
                if (pObject.grassCount != null) ret.grassCount = pObject.grassCount.Value;

                ret.standard = pObject.standard;
                ret.advertise_pic_1 = pObject.advertise_pic_1;
                ret.advertise_pic_2 = pObject.advertise_pic_2;
                ret.advertise_pic_3 = pObject.advertise_pic_3;
                ret.aaid = pObject.aaid.ToString();
                ret.last_update_user = pObject.last_update_user.ToString();

                ret.KeyWords = ret.p_no + "," + ret.name;
                return ret;
            }
            return null;
        }

        public static async Task<Tuple<int, List<Guid>>> Search(string queryStr, Guid mid, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                var container = Query<IndexProduct>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                .OnFields(new[] { "KeyWords" })
                .Analyzer("ik_smart"));

                var statusContainer = Query<IndexProduct>.Term("status", (int)EProductStatus.已添加);
                var midContainer = Query<IndexProduct>.Term("mid", mid.ToString());
                container = container && statusContainer && midContainer;

                //search
                var result = await _client.SearchAsync<IndexProduct>(s => s.Index(_config.IndexName).Query(container).SortDescending("timestamp")
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

                    int totalCount = (int)result.Total;//await GetSearchTotalCount(queryStr, mid);
                    return Tuple.Create(totalCount, ret);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<Guid>());
        }

        public static async Task<Tuple<int, List<IndexProduct>>> SearchList(string queryStr, Guid mid, int pageIndex, int pageSize, string SortDescending, int? category = null)
        {
            try
            {

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                //主搜索
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexProduct>.MatchAll();
                }
                else
                {
                    container = Query<IndexProduct>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }
                //mid
                var midContainer = Query<IndexProduct>.Term("mid", mid.ToString());
                //状态
                var statusContainer = Query<IndexProduct>.Term("status", (int)EProductStatus.已添加);
                //category
                if (category != null || category >= 0)
                {
                    var categoryContainer = Query<IndexProduct>.Term("category", category);
                    container = container && categoryContainer;
                }
                container = container && statusContainer && midContainer;
                //search
                var result = await _client.SearchAsync<IndexProduct>(s => s.Index(_config.IndexName).Query(container).SortDescending(SortDescending).Skip(from).Take(size));
                var list = result.Documents.ToList();
                int totalCount = (int)result.Total;
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexProduct>());
        }

        public static async Task<bool> AddOrUpdateAsync(IndexProduct obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexProduct>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexProduct l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexProduct>((u) =>
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
                LogError(new Exception($"fun:AddOrUpdateAsync,id:{obj.Id},name:{obj.name}", ex));
            }
            return false;
        }

        /// <summary>
        /// 用于数据迁移
        /// </summary>
        /// <param name="client"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool AddOrUpdate(ElasticClient client, IndexProduct obj)
        {
            try
            {
                var result = client.Search<IndexProduct>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexProduct l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = client.Update<IndexProduct>((u) =>
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

        /// <summary>
        /// 物理删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync(Guid pid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexProduct>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(pid))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexProduct>((u) =>
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

        public static async Task<IndexProduct> GetByPidAsync(Guid pid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexProduct>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(pid))));
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

        public static IndexProduct GetByPid(Guid pid)
        {
            try
            {
                var result = _client.Search<IndexProduct>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(pid))));
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

        public static async Task<int> GetCountByMidAsync(Guid mid, double timeStart, double timeEnd)
        {
            var container = Query<IndexProduct>.Term("mid", mid.ToString());
            var statusContainer = Query<IndexProduct>.Term("status", (int)EProductStatus.已添加);
            var timeContainer = Query<IndexProduct>.Range(r => r.GreaterOrEquals(timeStart).OnField(f => f.timestamp));
            timeContainer = timeContainer && Query<IndexProduct>.Range(r => r.LowerOrEquals(timeEnd).OnField(f => f.timestamp));
            var result =
                await
                    _client.SearchAsync<IndexProduct>(s => s.Index(_config.IndexName).Query(container && statusContainer && timeContainer).Take(0));
            return Convert.ToInt32(result.Total);
        }
    }
}
