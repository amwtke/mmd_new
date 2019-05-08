using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using Nest;
using MD.Lib.Util;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsOrderManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsOrderManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsOrderConfig _config = null;
        static EsOrderManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsOrderConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsOrderConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexOrder>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexOrder>(client, _config.IndexName, new IndexSettings()
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

        public static IndexOrder GenObject(Order obj, string keyWords)
        {
            if (obj != null && !obj.oid.Equals(Guid.Empty))
            {
                IndexOrder ret = new IndexOrder()
                {
                    Id = obj.oid.ToString(),
                    actual_pay = obj.actual_pay,
                    buyer = obj.buyer.ToString(),
                    post_company = obj.post_company,
                    default_writeoff_point = obj.default_writeoff_point.ToString(),
                    extral_info = obj.extral_info,
                    goid = obj.goid.ToString(),
                    order_price = obj.order_price,
                    mid = obj.mid.ToString(),
                    o_no = obj.o_no,
                    paytime = obj.paytime,
                    post_number = obj.post_number,
                    status = obj.status,
                    upid = obj.upid.ToString(),
                    waytoget = obj.waytoget,
                    writeoffday = obj.writeoffday,
                    writeoffer = obj.writeoffer.ToString(),
                    gid = obj.gid.ToString(),
                    name = obj.name,
                    cellphone = obj.cellphone,
                    postaddress = obj.postaddress,
                    post_price = obj.post_price == null ? 0 : obj.post_price.Value,
                    KeyWords = keyWords,
                };
                return ret;
            }
            return null;
        }

        public static IndexOrder GenObject(Order obj)
        {
            if (obj != null && !obj.oid.Equals(Guid.Empty))
            {
                var keyWords = "";
                var index = GetById(obj.oid);
                if (index != null)
                {
                    keyWords = index.KeyWords;
                }

                IndexOrder ret = new IndexOrder()
                {
                    Id = obj.oid.ToString(),
                    actual_pay = obj.actual_pay,
                    buyer = obj.buyer.ToString(),
                    post_company = obj.post_company,
                    default_writeoff_point = obj.default_writeoff_point.ToString(),
                    extral_info = obj.extral_info,
                    goid = obj.goid.ToString(),
                    order_price = obj.order_price,
                    mid = obj.mid.ToString(),
                    o_no = obj.o_no,
                    paytime = obj.paytime,
                    post_number = obj.post_number,
                    status = obj.status,
                    upid = obj.upid.ToString(),
                    waytoget = obj.waytoget,
                    writeoffday = obj.writeoffday,
                    writeoffer = obj.writeoffer.ToString(),
                    gid = obj.gid.ToString(),
                    name = obj.name,
                    cellphone = obj.cellphone,
                    postaddress = obj.postaddress,
                    post_price = obj.post_price == null ? 0 : obj.post_price.Value,
                    KeyWords = keyWords,
                };
                return ret;
            }
            return null;
        }


        public static async Task<IndexOrder> GenObjectAsync(Order obj)
        {
            if (obj != null && !obj.oid.Equals(Guid.Empty))
            {
                var keyWords = "";
                var index = await GetByIdAsync(obj.oid);
                if (index != null)
                {
                    keyWords = index.KeyWords;
                }

                IndexOrder ret = new IndexOrder()
                {
                    Id = obj.oid.ToString(),
                    actual_pay = obj.actual_pay,
                    buyer = obj.buyer.ToString(),
                    post_company = obj.post_company,
                    default_writeoff_point = obj.default_writeoff_point.ToString(),
                    extral_info = obj.extral_info,
                    goid = obj.goid.ToString(),
                    order_price = obj.order_price,
                    mid = obj.mid.ToString(),
                    o_no = obj.o_no,
                    paytime = obj.paytime,
                    post_number = obj.post_number,
                    status = obj.status,
                    upid = obj.upid.ToString(),
                    waytoget = obj.waytoget,
                    writeoffday = obj.writeoffday,
                    writeoffer = obj.writeoffer.ToString(),
                    gid = obj.gid.ToString(),
                    name = obj.name,
                    cellphone = obj.cellphone,
                    postaddress = obj.postaddress,
                    post_price = obj.post_price == null ? 0 : obj.post_price.Value,
                    KeyWords = keyWords,
                };
                if (obj.shipmenttime != null) ret.shipmenttime = obj.shipmenttime;
                return ret;
            }
            return null;
        }

        public static async Task<bool> AddOrUpdateAsync(IndexOrder obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexOrder l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexOrder>((u) =>
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
        /// <param name="client"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool AddOrUpdate(ElasticClient client, IndexOrder obj)
        {
            try
            {
                var result = client.Search<IndexOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexOrder l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = client.Update<IndexOrder>((u) =>
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

        public static bool AddOrUpdate(IndexOrder obj)
        {
            try
            {
                var result = _client.Search<IndexOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexOrder l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = _client.Update<IndexOrder>((u) =>
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

        public static async Task<bool> DeleteAsync(Guid oid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(oid))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexOrder>((u) =>
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

        public static async Task<IndexOrder> GetByIdAsync(Guid oid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(oid))));
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

        public static IndexOrder GetById(Guid oid)
        {
            try
            {
                var result = _client.Search<IndexOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(oid))));
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

        public static async Task<IndexOrder> GetByOtnAsync(string otn)
        {
            try
            {
                var result = await _client.SearchAsync<IndexOrder>(s => s.Query(q => q.Term(t => t.OnField("o_no").Value(otn))));
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


        public static async Task<List<IndexOrder>> GetByGidAsync(Guid gid, EOrderStatus status)
        {
            try
            {
                var container = Query<IndexOrder>.Term("status", (int)status);
                var gidContainer = Query<IndexOrder>.Term("gid", gid.ToString());
                container = container && gidContainer;

                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container));
                return result.Documents.ToList();
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsOrderManager), ex);
            }

        }

        public static async Task<Tuple<int, List<IndexOrder>>> GetByGidAsync(Guid gid, List<int> status, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                QueryContainer container = null;
                //主搜索
                if (status != null && status.Count > 0)
                {
                    QueryContainer statusContainer = null;
                    foreach (var s in status)
                    {
                        if (statusContainer == null)
                        {
                            statusContainer = Query<IndexOrder>.Term("status", s);
                        }
                        else
                        {
                            statusContainer = statusContainer || Query<IndexOrder>.Term("status", s);
                        }
                    }
                    container = container && statusContainer;
                }

                var gidContainer = Query<IndexOrder>.Term("gid", gid.ToString());
                container = container && gidContainer;
                //排除机器人
                QueryContainer robotContainer = Query<IndexOrder>.Term("mid", "11111111-1111-1111-1111-111111111111");
                container = container && !robotContainer;

                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("paytime").Skip(from).Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsOrderManager), ex);
            }
            return Tuple.Create(0, new List<IndexOrder>());
        }

        public static async Task<int> GetByGidCountAsync(Guid gid, EOrderStatus status)
        {
            try
            {
                var container = Query<IndexOrder>.Term("status", (int)status);
                var gidContainer = Query<IndexOrder>.Term("gid", gid.ToString());
                container = container && gidContainer;

                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container));
                return (int)result.Total;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsOrderManager), ex);
            }

        }

        public static async Task<List<IndexOrder>> GetByGoidAsync(Guid goid)
        {
            try
            {
                var container = Query<IndexOrder>.Term("goid", goid.ToString());

                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container));
                return result.Documents.ToList();
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsOrderManager), ex);
            }
        }

        public static List<IndexOrder> GetByGoid(Guid goid)
        {
            try
            {
                var container = Query<IndexOrder>.Term("goid", goid.ToString());

                var result =
                        _client.Search<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container));
                return result.Documents.ToList();
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsOrderManager), ex);
            }
        }

        /// <summary>
        /// 根据Gid获取该团的订单数量
        /// </summary>
        /// <param name="queryStr">查询关键字</param>
        /// <param name="status">状态</param>
        /// <param name="gid">团Gid</param>
        /// <returns></returns>
        public static async Task<int> GetByGidCountAsync(string queryStr, List<int> status, Guid gid)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }
                //排除机器人
                QueryContainer uidContainer = Query<IndexOrder>.Term("mid", "11111111-1111-1111-1111-111111111111");
                QueryContainer uidNotContainer = Query<IndexOrder>.Bool(t => t.MustNot(uidContainer));
                //主搜索
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    if (statusContainer == null)
                        statusContainer = Query<IndexOrder>.Term("status", s);
                    else
                        statusContainer = statusContainer || Query<IndexOrder>.Term("status", s);
                }

                container = container && statusContainer;

                var goidContainer = Query<IndexOrder>.Term("gid", gid.ToString());
                container = container && goidContainer;

                //search
                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container && uidNotContainer));

                int totalCount = (int)result.Total;
                return totalCount;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return 0;
        }

        public static int GetByGidCount(string queryStr, List<int> status, Guid gid)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = ESHeper.AnalyzeQueryString_TB(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }
                //排除机器人
                QueryContainer uidContainer = Query<IndexOrder>.Term("mid", "11111111-1111-1111-1111-111111111111");
                QueryContainer uidNotContainer = Query<IndexOrder>.Bool(t => t.MustNot(uidContainer));
                //主搜索
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    if (statusContainer == null)
                        statusContainer = Query<IndexOrder>.Term("status", s);
                    else
                        statusContainer = statusContainer || Query<IndexOrder>.Term("status", s);
                }

                container = container && statusContainer;

                var goidContainer = Query<IndexOrder>.Term("gid", gid.ToString());
                container = container && goidContainer;

                //search
                var result =
                    _client.Search<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container && uidNotContainer));

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
        /// 搜索一个mid的所有订单。
        /// </summary>
        /// <param name="queryStr"></param>
        /// <param name="mid"></param>
        /// <param name="waytoget">提货方式,null代表搜所有</param>
        /// <param name="status">null表示搜索所有</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, List<IndexOrder>>> SearchAsnyc(string qdate, string queryStr, Guid mid, int? waytoget,
            List<int> statuses,
            int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //qdate时间解析
                double begintime = CommonHelper.ToUnixTime(Convert.ToDateTime(qdate.Substring(0, 10)));
                double endtime = CommonHelper.ToUnixTime(Convert.ToDateTime(qdate.Substring(qdate.Length - 10, 10)).AddDays(1));
                QueryContainer qdateContainer;

                //主搜索
                QueryContainer statusContainer = null;
                if (statuses != null && statuses.Count > 0)
                {
                    qdateContainer = Query<IndexOrder>.Range(o => o.OnField("paytime").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                    foreach (var s in statuses)
                    {
                        if (statusContainer == null)
                            statusContainer = Query<IndexOrder>.Term("status", s);
                        statusContainer = statusContainer || Query<IndexOrder>.Term("status", s);
                    }
                    container = container && qdateContainer && statusContainer;
                }
                else
                {
                    qdateContainer = Query<IndexOrder>.Range(o => o.OnField("paytime").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                    container = container & qdateContainer;
                }

                if (waytoget != null)
                {
                    var wtgContainer = Query<IndexOrder>.Term("waytoget", waytoget);
                    container = container && wtgContainer;
                }

                var midContainer = Query<IndexOrder>.Term("mid", mid);
                container = container && midContainer;

                //search
                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("paytime")
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
            return Tuple.Create(0, new List<IndexOrder>());
        }

        public static async Task<Tuple<int, List<IndexOrder>>> SearchAsnyc2(Guid? writeoffpoint, Guid writeoffer, string qdate, string queryStr, Guid mid, int? waytoget,
            int? status,
            int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                //keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    keywords = "*" + keywords + "*";
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //qdate时间解析
                double begintime = CommonHelper.ToUnixTime(Convert.ToDateTime(qdate.Substring(0, 10)));
                double endtime = CommonHelper.ToUnixTime(Convert.ToDateTime(qdate.Substring(qdate.Length - 10, 10)).AddDays(1));
                QueryContainer qdateContainer;
                QueryContainer pointContainer = null;
                //主搜索
                if (status != null)
                {
                    if (status == (int)EOrderStatus.已成团未提货)
                    {
                        qdateContainer = Query<IndexOrder>.Range(o => o.OnField("paytime").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                        //writeoffpoint按预约门店查询
                        if (writeoffpoint != null && writeoffpoint != Guid.Empty)
                            pointContainer = Query<IndexOrder>.Term("default_writeoff_point", writeoffpoint);
                    }
                    else
                    {
                        if (waytoget == (int)EWayToGet.物流)
                            qdateContainer = Query<IndexOrder>.Range(o => o.OnField("paytime").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                        else
                            qdateContainer = Query<IndexOrder>.Range(o => o.OnField("writeoffday").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                        //writeoffpoint按实际核销门店查询
                        if (writeoffpoint != null && writeoffpoint != Guid.Empty)
                            pointContainer = Query<IndexOrder>.Term("extral_info", writeoffpoint);
                        if (writeoffer != Guid.Empty)
                            pointContainer = pointContainer && Query<IndexOrder>.Term("writeoffer", writeoffer);
                    }
                    var statusContainer = Query<IndexOrder>.Term("status", status);
                    container = container && statusContainer & qdateContainer && pointContainer;
                }
                else
                {
                    qdateContainer = Query<IndexOrder>.Range(o => o.OnField("paytime").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                    container = container & qdateContainer;
                }

                if (waytoget != null)
                {
                    var wtgContainer = Query<IndexOrder>.Term("waytoget", waytoget);
                    container = container && wtgContainer;
                }

                var midContainer = Query<IndexOrder>.Term("mid", mid);
                container = container && midContainer;

                //search
                var result = status == null || status != (int)EOrderStatus.拼团成功
                    ? await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("paytime")
                                .Skip(from).Take(size))
                    : await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("writeoffday")
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
            return Tuple.Create(0, new List<IndexOrder>());
        }

        public static async Task<Tuple<int, List<Guid>>> SearchByGoidAsnyc(string queryStr, int status, Guid goid, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索

                var statusContainer = Query<IndexOrder>.Term("status", status);
                var midContainer = Query<IndexOrder>.Term("goid", goid.ToString());
                container = container && statusContainer && midContainer;

                //search
                var result = await _client.SearchAsync<IndexOrder>(s => s.Index(_config.IndexName).Query(container).SortDescending("paytime")
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

        public static async Task<Tuple<int, List<IndexOrder>>> SearchByGoidAsnyc2(string queryStr, List<int> status, Guid goid, int pageIndex, int pageSize, bool isDescending = true)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    if (statusContainer == null)
                        statusContainer = Query<IndexOrder>.Term("status", s);
                    else
                        statusContainer = statusContainer || Query<IndexOrder>.Term("status", s);
                }

                container = container && statusContainer;

                var goidContainer = Query<IndexOrder>.Term("goid", goid.ToString());
                container = container && goidContainer;

                //search
                var result = isDescending ?
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("paytime")
                                .Skip(from).Take(size)) :
                                 await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortAscending("paytime")
                                .Skip(from).Take(size))
                                ;
                var list = result.Documents.ToList();

                int totalCount = (int)result.Total;
                return Tuple.Create(totalCount, list);

            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexOrder>());
        }


        public static async Task<Tuple<int, List<IndexOrder>>> SearchAsnyc2(string queryStr, Guid mid, Guid uid,
            int? waytoget,
            int? status,
            int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                if (status != null)
                {
                    var statusContainer = Query<IndexOrder>.Term("status", status);
                    container = container && statusContainer;
                }

                if (waytoget != null&&waytoget>=0)
                {
                    var wtgContainer = Query<IndexOrder>.Term("waytoget", waytoget);
                    container = container && wtgContainer;
                }


                var uidContainer = Query<IndexOrder>.Term("buyer", uid);
                container = container && uidContainer;


                var midContainer = Query<IndexOrder>.Term("mid", mid);
                container = container && midContainer;

                //search
                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("paytime")
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
            return Tuple.Create(0, new List<IndexOrder>());
        }

        /// <summary>
        /// 出报表用
        /// </summary>
        /// <param name="queryStr"></param>
        /// <param name="mid"></param>
        /// <param name="uid"></param>
        /// <param name="waytoget"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, List<IndexOrder>>> Search(Guid? writeoffpoint, Guid writeoffer, string qdate, string queryStr, Guid mid, int? waytoget, int? status)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                //keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    keywords = "*" + keywords + "*";
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }
                //qdate时间解析
                double begintime = CommonHelper.ToUnixTime(Convert.ToDateTime(qdate.Substring(0, 10)));
                double endtime = CommonHelper.ToUnixTime(Convert.ToDateTime(qdate.Substring(qdate.Length - 10, 10)).AddDays(1));
                QueryContainer qdateContainer;
                QueryContainer pointContainer = null;
                //主搜索
                if (status != null)
                {
                    if (status == (int)EOrderStatus.已成团未提货)
                    {
                        qdateContainer = Query<IndexOrder>.Range(o => o.OnField("paytime").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                        //writeoffpoint按预约门店查询
                        if (writeoffpoint != null && writeoffpoint != Guid.Empty)
                            pointContainer = Query<IndexOrder>.Term("default_writeoff_point", writeoffpoint);
                    }
                    else
                    {
                        if (waytoget == (int)EWayToGet.物流)
                            qdateContainer = Query<IndexOrder>.Range(o => o.OnField("paytime").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                        else
                            qdateContainer = Query<IndexOrder>.Range(o => o.OnField("writeoffday").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                        //writeoffpoint按实际核销门店查询
                        if (writeoffpoint != null && writeoffpoint != Guid.Empty)
                            pointContainer = Query<IndexOrder>.Term("extral_info", writeoffpoint);
                        if (writeoffer != Guid.Empty)
                            pointContainer = pointContainer && Query<IndexOrder>.Term("writeoffer", writeoffer);
                    }
                    var statusContainer = Query<IndexOrder>.Term("status", status);
                    container = container && qdateContainer && statusContainer && pointContainer;
                }
                else
                {
                    qdateContainer = Query<IndexOrder>.Range(o => o.OnField("paytime").GreaterOrEquals(begintime).LowerOrEquals(endtime));
                    container = container & qdateContainer;
                }

                if (waytoget != null)
                {
                    var wtgContainer = Query<IndexOrder>.Term("waytoget", waytoget);
                    container = container && wtgContainer;
                }

                var midContainer = Query<IndexOrder>.Term("mid", mid);
                container = container && midContainer;

                //search
                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("paytime").Take(100000));
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
            return Tuple.Create(0, new List<IndexOrder>());
        }

        /// <summary>
        /// 获取最新的10个已付款订单
        /// </summary>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public static async Task<List<IndexOrder>> Top10(string queryStr, List<int> status)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                //主搜索
                if (status != null && status.Count > 0)
                {
                    QueryContainer statusContainer = null;
                    foreach (var s in status)
                    {
                        if (statusContainer == null)
                        {
                            statusContainer = Query<IndexOrder>.Term("status", s);
                        }
                        else
                        {
                            statusContainer = statusContainer || Query<IndexOrder>.Term("status", s);
                        }
                    }
                    container = container && statusContainer;
                }

                //if (waytoget != null)
                //{
                //    var wtgContainer = Query<IndexOrder>.Term("waytoget", waytoget);
                //    container = container && wtgContainer;
                //}

                //var midContainer = Query<IndexOrder>.Term("mid", mid);
                //container = container && midContainer;

                //search
                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("paytime"));
                var list = result.Documents.ToList();
                return list;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return new List<IndexOrder>();
        }

        public static async Task<List<IndexOrder>> SearchByStatusesAsnyc(string queryStr, List<int> status, int take)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexOrder>.MatchAll();
                }
                else
                {
                    container = Query<IndexOrder>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                //主搜索
                if (status != null && status.Count > 0)
                {
                    QueryContainer statusContainer = null;
                    foreach (var s in status)
                    {
                        if (statusContainer == null)
                        {
                            statusContainer = Query<IndexOrder>.Term("status", s);
                        }
                        else
                        {
                            statusContainer = statusContainer || Query<IndexOrder>.Term("status", s);
                        }
                    }
                    container = container && statusContainer;
                }

                var result =
                    await
                        _client.SearchAsync<IndexOrder>(
                            s => s.Index(_config.IndexName).Query(container).SortDescending("paytime").Take(take));
                var list = result.Documents.ToList();
                return list;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return new List<IndexOrder>();
        }

        public static async Task<int> GetOrderCountAsync(Guid mid, List<int> status, double timeStart, double timeEnd)
        {
            var midContainer = Query<IndexOrder>.Term("mid", mid.ToString());
            QueryContainer statusContainer = null;
            foreach (var s in status)
            {
                var stContainer = Query<IndexOrder>.Term("status", s);
                statusContainer = statusContainer || stContainer;
            }
            var timeContainer = Query<IndexOrder>.Range(r => r.OnField(f => f.paytime).GreaterOrEquals(timeStart).LowerOrEquals(timeEnd));
            var result = await _client.SearchAsync<IndexOrder>(s => s.Index(_config.IndexName).Query(midContainer && statusContainer && timeContainer).Take(0));
            return (int)result.Total;
        }

        public static async Task<Dictionary<string, int>> GetOrderCountGroupByWoid(Guid mid, int status, double timeStart, double timeEnd)
        {
            var midContainer = Query<IndexOrder>.Term("mid", mid.ToString());
            QueryContainer statusContainer = Query<IndexOrder>.Term("status", status);
            QueryContainer timeContainer = null;
            if (timeStart != 0 && timeEnd != 0 )
            {
                timeContainer = Query<IndexOrder>.Range(r => r.OnField(f => f.paytime).GreaterOrEquals(timeStart).LowerOrEquals(timeEnd));
            }
            var result = await _client.SearchAsync<IndexOrder>(s => s.Index(_config.IndexName).Query(midContainer && statusContainer && timeContainer).Take(0)
            .Aggregations(a => a.Terms("groupByWrite", sa => sa.Field(g => g.default_writeoff_point).Size(1000)))
            );
            if (result.Aggs != null && result.Aggs.Terms("groupByWrite") != null )
            {
                return result.Aggs.Terms("groupByWrite").Items.ToDictionary(t => t.Key, t => (int)t.DocCount);
            }
            return new Dictionary<string, int>();
        }

        /// <summary>
        /// 获取统计后的mid,ordercount
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="status"></param>
        /// <param name="timestart"></param>
        /// <param name="timeEnd"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, List<KeyItem>>> GetMid_OrderCountAsync(List<int> status, int pageIndex, int pageSize, double? timestart = null, double? timeEnd = null)
        {
            int from = (pageIndex - 1) * pageSize;
            int size = pageSize;
            QueryContainer Container = null;
            QueryContainer statusContainer = null;
            foreach (var s in status)
            {
                var stContainer = Query<IndexOrder>.Term("status", s);
                statusContainer = statusContainer || stContainer;
            }
            Container = Container && statusContainer;
            if (timestart != null && timeEnd != null)
            {
                var timeContainer = Query<IndexOrder>.Range(r => r.OnField(f => f.paytime).GreaterOrEquals(timestart).LowerOrEquals(timeEnd));
                Container = Container && timeContainer;
            }
            //排除机器人
            QueryContainer robotContainer = Query<IndexOrder>.Term("mid", "11111111-1111-1111-1111-111111111111");
            Container = Container && !robotContainer;

            var result = await _client.SearchAsync<IndexOrder>(s => s.Index(_config.IndexName).Query(Container)
             .Aggregations(a => a.Terms("mids", sa => sa.Field(g => g.mid).Size(10000))));

            var agg = result.Aggs.Terms("mids").Items.Skip(from).Take(size).ToList();
            return Tuple.Create(result.Aggs.Terms("mids").Items.Count, agg);
        }


        /// <summary>
        /// 获取成交订单数量和金额
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="status"></param>
        /// <param name="timeStart"></param>
        /// <param name="timeEnd"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, decimal>> GetOrderCountAndAmountAsync(Guid mid, List<int> status, double timeStart, double timeEnd)
        {
            try
            {
                var midContainer = Query<IndexOrder>.Term("mid", mid.ToString());
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    var stContainer = Query<IndexOrder>.Term("status", s);
                    statusContainer = statusContainer || stContainer;
                }
                var timeContainer = Query<IndexOrder>.Range(r => r.OnField(f => f.paytime).GreaterOrEquals(timeStart).LowerOrEquals(timeEnd));
                //排除机器人
                QueryContainer uidContainer = Query<IndexOrder>.Term("mid", "11111111-1111-1111-1111-111111111111");
                QueryContainer uidNotContainer = Query<IndexOrder>.Bool(t => t.MustNot(uidContainer));
                var result = await _client.SearchAsync<IndexOrder>(s => s.Index(_config.IndexName).Query(midContainer && statusContainer && timeContainer && uidNotContainer)
                .Aggregations(a => a.Sum("amount", am => am.Field(o => o.order_price)))
                );
                var list = result.Documents.ToList();
                if (list != null && list.Count > 0)
                {
                    int length = (int)result.Total;
                    decimal amountSum = 0;
                    if (result.Aggs.Sum("amount") != null)
                        amountSum = Convert.ToDecimal(result.Aggs.Sum("amount").Value / 100.00);
                    return Tuple.Create(length, amountSum);
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogError(typeof(EsOrderManager), ex);
            }
            return Tuple.Create(0, Convert.ToDecimal(0.00));
        }


        public static Tuple<int, decimal> GetOrderCountAndAmountByGid(Guid gid, List<int> status, double timeStart, double timeEnd)
        {
            try
            {
                var midContainer = Query<IndexOrder>.Term("gid", gid.ToString());
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    var stContainer = Query<IndexOrder>.Term("status", s);
                    statusContainer = statusContainer || stContainer;
                }
                var timeContainer = Query<IndexOrder>.Range(r => r.OnField(f => f.paytime).GreaterOrEquals(timeStart).LowerOrEquals(timeEnd));
                //排除机器人
                QueryContainer robotContainer = Query<IndexOrder>.Term("mid", "11111111-1111-1111-1111-111111111111");

                var result = _client.Search<IndexOrder>(s => s.Index(_config.IndexName).Query(midContainer && statusContainer && timeContainer && !robotContainer)
                .Aggregations(a => a.Sum("amount", am => am.Field(o => o.order_price)))
                );
                var list = result.Documents.ToList();
                if (list != null && list.Count > 0)
                {
                    int length = (int)result.Total;
                    decimal amountSum = 0;
                    if(result.Aggs.Sum("amount") != null)
                        amountSum = Convert.ToDecimal(result.Aggs.Sum("amount").Value / 100.00);
                    return Tuple.Create(length, amountSum);
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogError(typeof(EsOrderManager), ex);
            }
            return Tuple.Create(0, Convert.ToDecimal(0));
        }

        public static async Task<decimal> GetAmountAsync(string Gid, List<int> status)
        {
            var gidContainer = Query<IndexOrder>.Term("gid", Gid);
            QueryContainer statusContainer = null;
            foreach (var s in status)
            {
                var stContainer = Query<IndexOrder>.Term("status", s);
                statusContainer = statusContainer || stContainer;
            }
            var result = await _client.SearchAsync<IndexOrder>(s => s.Index(_config.IndexName).Query(gidContainer && statusContainer)
          .Aggregations(a => a.Sum("amount", am => am.Field(o => o.order_price)))
          );
            decimal amountSum = Convert.ToDecimal(result.Aggs.Sum("amount").Value / 100.00);
            return amountSum;
        }

        public static async Task<IndexOrder> GetOrderByUid(string uid)
        {
            var uidContainer = Query<IndexOrder>.Term(o=>o.buyer, uid);
            var result = await _client.SearchAsync<IndexOrder>(s => s.Index(_config.IndexName).Query(uidContainer).SortDescending("paytime")
          );
            var list = result.Documents;
            if (list != null && list.Count() > 0)
            {
                return list.ToList()[0];
            }
            return null;
        }
    }
}

