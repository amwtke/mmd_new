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
    public static class EsSupplyManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsSupplyManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsSupplyConfig _config = null;
        static EsSupplyManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsSupplyConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsSupplyConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexSupply>(_client, _config.IndexName, new IndexSettings()
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

        public static async Task<IndexSupply> GenObject(Guid sid)
        {
            try
            {
                using (var repo = new BizRepository())
                {
                    var supply = await repo.GetSupplyBySidAsync(sid);
                    if (supply != null && !supply.sid.Equals(Guid.Empty))
                    {
                        IndexSupply indexsupply = new IndexSupply()
                        {
                            advertise_pic_1 = supply.advertise_pic_1,
                            advertise_pic_2 = supply.advertise_pic_2,
                            advertise_pic_3 = supply.advertise_pic_3,
                            brand = supply.brand.Value,
                            category = supply.category.Value,
                            description = supply.description,
                            group_price = supply.group_price.Value,
                            Id = supply.sid.ToString(),
                            market_price = supply.market_price.Value,
                            name = supply.name,
                            pack = supply.pack,
                            quota_max = supply.quota_max.Value,
                            quota_min = supply.quota_min.Value,
                            standard = supply.standard,
                            status = supply.status.Value,
                            supply_price = supply.supply_price.Value,
                            s_no = supply.s_no.Value,
                            timestamp = supply.timestamp.Value,
                        };
                        if (supply.headpic_dir != null) indexsupply.headpic_dir = supply.headpic_dir;
                        indexsupply.KeyWords = indexsupply.name + "," + indexsupply.description;
                        return indexsupply;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw ex;
            }
        }

        public static async Task<bool> AddOrUpdateAsync(IndexSupply obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexSupply>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexSupply l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexSupply>((u) =>
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

        public static async Task<bool> DeleteAsync(IndexSupply obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexSupply>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexSupply>((u) =>
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

        public static async Task<IndexSupply> GetByGidAsync(Guid sid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexSupply>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(sid.ToString()))));
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

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="queryStr">关键字</param>
        /// <param name="category">分类ID</param>
        /// <param name="brand">品牌ID</param>
        /// <param name="pageIndex">pageIndex</param>
        /// <param name="pageSize">pageSize</param>
        /// <returns></returns>
        public static async Task<Tuple<int, List<Guid>>> SearchAsnyc(string queryStr, int pageIndex, int pageSize, List<int> status, int? category=null, int? brand=null)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexSupply>.MatchAll();
                }
                else
                {
                    container = Query<IndexSupply>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                //按状态查询
                QueryContainer statusContainer = null;
                if (status != null && status.Count > 0)
                {
                    foreach (var s in status)
                    {
                        var statusContainer2 = Query<IndexSupply>.Term("status", s);
                        statusContainer = statusContainer || statusContainer2;
                    }
                    container = container && statusContainer;
                }

                //按分类查询
                if (category != null && category >= 0)
                {
                    var categoryContainer = Query<IndexSupply>.Term("category", category);
                    container = container && categoryContainer;
                }
                //按品牌查询
                if (brand != null && brand >= 0)
                {
                    var brandContainer = Query<IndexSupply>.Term("brand", brand);
                    container = container && brandContainer;
                }

                //search
                var result = await _client.SearchAsync<IndexSupply>(s => s.Index(_config.IndexName).Query(container).SortDescending("timestamp")
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
        public static async Task<Tuple<int, List<IndexSupply>>> SearchAsnyc2(string queryStr, int pageIndex, int pageSize, List<int> status, int? category = null, int? brand = null)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexSupply>.MatchAll();
                }
                else
                {
                    container = Query<IndexSupply>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                //按状态查询
                QueryContainer statusContainer = null;
                if (status != null && status.Count > 0)
                {
                    foreach (var s in status)
                    {
                        var statusContainer2 = Query<IndexSupply>.Term("status", s);
                        statusContainer = statusContainer || statusContainer2;
                    }
                    container = container && statusContainer;
                }

                //按分类查询
                if (category != null && category >= 0)
                {
                    var categoryContainer = Query<IndexSupply>.Term("category", category);
                    container = container && categoryContainer;
                }
                //按品牌查询
                if (brand != null && brand >= 0)
                {
                    var brandContainer = Query<IndexSupply>.Term("brand", brand);
                    container = container && brandContainer;
                }

                //search
                var result = await _client.SearchAsync<IndexSupply>(s => s.Index(_config.IndexName).Query(container).SortDescending("timestamp")
                .Skip(from).Take(size));
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
            return Tuple.Create(0, new List<IndexSupply>());
        }
    }
}
