using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB.Professional;
using MD.Model.Index.MD;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsCommunityBizManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsCommunityBizManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsCommunityBizConfig _config = null;
        static EsCommunityBizManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsCommunityBizConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsCommunityBizConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexCommunityBiz>(_client, _config.IndexName, new IndexSettings()
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
        /// 增加一条点赞或关注的记录
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="from_uid">来源</param>
        /// <param name="bizId">业务id</param>
        /// <param name="type">类型：关注或点赞</param>
        /// <returns></returns>
        public static async Task<bool> AddBizAsync(IndexCommunityBiz biz)
        {
            try
            {
                if (biz == null || string.IsNullOrEmpty(biz.Id))
                    return false;
                var tempcommunity = await GetCommunityBizAsync(Guid.Parse(biz.uid), Guid.Parse(biz.from_id), Guid.Parse(biz.bizid), biz.biztype);
                if (tempcommunity == null)
                {
                    var resoponse = await _client.IndexAsync(biz, (i) => { i.Index(_config.IndexName); return i; });
                    return resoponse.Created;
                }
                else
                {
                    if (tempcommunity.isread == 1)
                    {
                        string _id = tempcommunity.Id;
                        var r = await _client.UpdateAsync<IndexCommunityBiz>((u) =>
                        {
                            u.Id(_id);
                            u.Doc(biz);
                            u.Index(_config.IndexName);
                            return u;
                        });
                        return r.IsValid;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static async Task<IndexCommunityBiz> GetCommunityBizAsync(Guid uid, Guid from_id, Guid bizid, int biztype)
        {
            try
            {
                QueryContainer Container = null;
                if (uid != null && !uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                    Container = Container && uidContainer;
                }
                if (from_id != null && !from_id.Equals(Guid.Empty))
                {
                    var fromIdContainer = Query<IndexCommunityBiz>.Term("from_id", from_id);
                    Container = Container && fromIdContainer;
                }
                if (bizid != null && !bizid.Equals(Guid.Empty))
                {
                    var bizIdContainer = Query<IndexCommunityBiz>.Term("bizid", bizid);
                    Container = Container && bizIdContainer;
                }
                var typeContainer = Query<IndexCommunityBiz>.Term("biztype", biztype);
                Container = Container && typeContainer;
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container));
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

        public static async Task<Tuple<int, List<IndexCommunityBiz>>> GetCommunityBizListAsync(Guid mid, Guid uid, Guid from_id, Guid bizid, int biztype, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                QueryContainer Container = null;
                if (mid != null && !mid.Equals(Guid.Empty))
                {
                    var midContainer = Query<IndexCommunityBiz>.Term("mid", mid);
                    Container = Container && midContainer;
                }
                if (uid != null && !uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                    Container = Container && uidContainer;
                }
                if (from_id != null && !from_id.Equals(Guid.Empty))
                {
                    var fromIdContainer = Query<IndexCommunityBiz>.Term("from_id", from_id);
                    Container = Container && fromIdContainer;
                }
                if (bizid != null && !bizid.Equals(Guid.Empty))
                {
                    var bizIdContainer = Query<IndexCommunityBiz>.Term("bizid", bizid);
                    Container = Container && bizIdContainer;
                }
                var typeContainer = Query<IndexCommunityBiz>.Term("biztype", biztype);
                Container = Container && typeContainer;
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container).SortAscending(b => b.isread).SortDescending(b => b.timestamp).Take(from).Size(size));
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
            return Tuple.Create(0, new List<IndexCommunityBiz>());
        }

        public static async Task<Tuple<int, List<IndexCommunityBiz>>> GetCommunityBizListAsync(Guid uid, Guid from_id, Guid bizid, List<int> biztype, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                QueryContainer Container = null;
                if (uid != null && !uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                    Container = Container && uidContainer;
                }
                if (from_id != null && !from_id.Equals(Guid.Empty))
                {
                    var fromIdContainer = Query<IndexCommunityBiz>.Term("from_id", from_id);
                    Container = Container && fromIdContainer;
                }
                if (bizid != null && !bizid.Equals(Guid.Empty))
                {
                    var bizIdContainer = Query<IndexCommunityBiz>.Term("bizid", bizid);
                    Container = Container && bizIdContainer;
                }
                if (biztype != null && biztype.Count > 0)
                {
                    QueryContainer typeContainer = null;
                    foreach (var item in biztype)
                    {
                        var container = Query<IndexCommunityBiz>.Term("biztype", item);
                        typeContainer = container || typeContainer;
                    }
                    Container = Container && typeContainer;
                }
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container).SortAscending(b => b.isread).SortDescending(b => b.timestamp).Take(from).Size(size));
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
            return Tuple.Create(0, new List<IndexCommunityBiz>());
        }

        public static async Task<bool> UpdateCommunityAsync(Guid uid, List<int> type)
        {
            try
            {
                QueryContainer Container = null;
                var uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                var readContainer = Query<IndexCommunityBiz>.Term("isread", 0);
                Container = Container && uidContainer && readContainer;
                if (type != null && type.Count > 0)
                {
                    QueryContainer typeContainer = null;
                    foreach (var item in type)
                    {
                        var container = Query<IndexCommunityBiz>.Term("biztype", item);
                        typeContainer = typeContainer || container;
                    }
                    Container = Container && typeContainer;
                }
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container).Size(100));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    foreach (var biz in list)
                    {
                        string _id = biz.Id;
                        biz.isread = 1;
                        var r = await _client.UpdateAsync<IndexCommunityBiz>((u) =>
                        {
                            u.Id(_id);
                            u.Doc(biz);
                            u.Index(_config.IndexName);
                            return u;
                        });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        public static async Task<Tuple<int, List<string>>> GetFrom_idsByUidAsync(string uid, int biztype, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                var uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                var typeContainer = Query<IndexCommunityBiz>.Term("biztype", biztype);
                QueryContainer Container = uidContainer && typeContainer;
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container).Take(from).Size(size));
                var list = result.Documents.Select(p => p.from_id).ToList();
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
            return Tuple.Create(0, new List<string>());
        }

        public static async Task<Tuple<int, List<string>>> GetUidsByFrom_IdAsync(string from_id, int biztype, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                var uidContainer = Query<IndexCommunityBiz>.Term("from_id", from_id);
                var typeContainer = Query<IndexCommunityBiz>.Term("biztype", biztype);
                QueryContainer Container = uidContainer && typeContainer;
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container).Take(from).Size(size));
                int totalCount = (int)result.Total;
                if (totalCount > 0)
                {
                    var uidList = result.Documents.Select(p => p.uid).ToList();
                    return Tuple.Create(totalCount, uidList);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<string>());
        }
        public static async Task<Tuple<int, List<KeyItem>>> GetByAggBizTypeAsync(Guid mid, Guid uid, int? isRead, int? biztype)
        {
            try
            {
                QueryContainer Container = null;
                if (mid != null && !mid.Equals(Guid.Empty))
                {
                    var midContainer = Query<IndexCommunityBiz>.Term("mid", mid);
                    Container = Container && midContainer;
                }
                if (uid != null && !uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                    Container = Container && uidContainer;
                }
                if (biztype != null)
                {
                    var typeContainer = Query<IndexCommunityBiz>.Term("biztype", biztype);
                    Container = Container && typeContainer;
                }
                if (isRead != null)
                {
                    var isreadContainer = Query<IndexCommunityBiz>.Term("isread", isRead);
                    Container = Container && isreadContainer;
                }
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container)
                .Aggregations(b => b.Terms("biztypeGroupBy", bb => bb.Field(bbb => bbb.biztype))));
                if (result.Total > 0)
                {
                    var agg = result.Aggs.Terms("biztypeGroupBy").Items.ToList();
                    return Tuple.Create(result.Aggs.Terms("biztypeGroupBy").Items.Count, agg);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<KeyItem>());
        }

        /// <summary>
        /// 根据mid查询的结果group by uid，取出所有uid出现的次数(排行榜)
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="uids"></param>
        /// <param name="biztype"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, List<KeyItem>>> GetByAggUidAsync(Guid mid, List<string> uids, int biztype, int pageIndex, int pageSize, int? f = null, int? t = null)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                QueryContainer Container = null;
                if (uids != null && uids.Count > 0)
                {
                    QueryContainer uidContainer = null;
                    foreach (var uid in uids)
                    {
                        if (uidContainer == null)
                        {
                            uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                        }
                        else
                        {
                            uidContainer = uidContainer || Query<IndexCommunityBiz>.Term("uid", uid);
                        }
                    }
                    Container = Container && uidContainer;
                }
                var midContainer = Query<IndexCommunityBiz>.Term("mid", mid);
                var typeContainer = Query<IndexCommunityBiz>.Term("biztype", biztype);
                Container = Container && midContainer && typeContainer;
                if (f != null && t != null)
                {
                    var timeContainer = Query<IndexCommunityBiz>.Range(r => r.OnField("timestamp").GreaterOrEquals(f).LowerOrEquals(t));
                    Container = Container && timeContainer;
                }
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container)
                .Aggregations(a => a.Terms("uids", aa => aa.Field(aaa => aaa.uid).Size(10000))));
                if (result.Total > 0)
                {
                    var agg = result.Aggs.Terms("uids").Items.Skip(from).Take(size).ToList();
                    return Tuple.Create(result.Aggs.Terms("uids").Items.Count, agg);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<KeyItem>());
        }

        /// <summary>
        /// 根据mid,biztype的条件查询后分组，查到uid的用户次数及总排名
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="uid"></param>
        /// <param name="biztype"></param>
        /// <returns>Item1:uid出现的次数,item2:索引值+1</returns>
        public static async Task<Tuple<int, int>> GetByAggUidAsync(Guid mid, string uid, int biztype, int? from = null, int? to = null)
        {
            try
            {
                QueryContainer Container = null;
                var midContainer = Query<IndexCommunityBiz>.Term("mid", mid);
                var typeContainer = Query<IndexCommunityBiz>.Term("biztype", biztype);
                Container = midContainer && typeContainer;
                if (from != null && to != null)
                {
                    var timeContainer = Query<IndexCommunityBiz>.Range(r => r.OnField("timestamp").GreaterOrEquals(from).LowerOrEquals(to));
                    Container = Container && timeContainer;
                }
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container)
                .Aggregations(a => a.Terms("uids", aa => aa.Field(aaa => aaa.uid).Size(1000000))));
                if (result.Total > 0)
                {
                    var agg = result.Aggs.Terms("uids").Items;
                    if (agg != null)
                    {
                        var user = agg.Where(p => p.Key == uid).FirstOrDefault();//user的groupby结果
                        if (user != null)
                        {
                            int userindex = agg.IndexOf(user) + 1;
                            return Tuple.Create((int)user.DocCount, userindex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, 0);
        }

        public static async Task<bool> DelBizsAsync(Guid uid, Guid from_id, Guid bizId, int type)
        {
            try
            {

                 QueryContainer Container = null;
                if (uid != null && !uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                    Container = Container && uidContainer;
                }
                if (from_id != null && !from_id.Equals(Guid.Empty))
                {
                    var fromIdContainer = Query<IndexCommunityBiz>.Term("from_id", from_id);
                    Container = Container && fromIdContainer;
                }
                if (bizId != null && !bizId.Equals(Guid.Empty))
                {
                    var bizIdContainer = Query<IndexCommunityBiz>.Term("bizid", bizId);
                    Container = Container && bizIdContainer;
                }
                var typeContainer = Query<IndexCommunityBiz>.Term("biztype", type);
                Container = Container && typeContainer;
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Query(Container));
                if (result.Total >= 1)
                {
                    var r = await _client.DeleteByQueryAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(qq=> Container));
                    return r.IsValid;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }
        public static async Task<bool> DelBizAsync(Guid uid, Guid from_uid, Guid bizId, int type)
        {
            try
            {
                var uidContainer = Query<IndexCommunityBiz>.Term("uid", uid);
                var fromIdContainer = Query<IndexCommunityBiz>.Term("from_id", from_uid);
                var bizIdContainer = Query<IndexCommunityBiz>.Term("bizid", bizId);
                var typeContainer = Query<IndexCommunityBiz>.Term("biztype", type);
                var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Query(uidContainer && fromIdContainer && bizIdContainer && typeContainer));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexCommunityBiz>((u) =>
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

        /// <summary>
        /// 获取未读的通知的数量
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, int, int>> GetBizNReadCountAsync(Guid uid)
        {
            var Container = Query<IndexCommunityBiz>.Term(b => b.isread, 0);
            var uidContainer = Query<IndexCommunityBiz>.Term(b => b.uid, uid.ToString());
            var result = await _client.SearchAsync<IndexCommunityBiz>(s => s.Index(_config.IndexName).Query(Container && uidContainer).Aggregations(a => a.Terms("type", aa => aa.Field(aaa => aaa.biztype))));
            var agg = result.Aggs.Terms("type");
            if (agg != null && agg.Items.Count > 0)
            {
                var items = result.Aggs.Terms("type");
                var Favour = items.Items.Where(a => a.Key == ((int)EComBizType.Favour).ToString()).FirstOrDefault();
                var Reply = items.Items.Where(a => a.Key == ((int)EComBizType.Reply).ToString()).FirstOrDefault();
                var Subscribe = items.Items.Where(a => a.Key == ((int)EComBizType.Subscribe).ToString()).FirstOrDefault();
                int countFavour = Favour == null ? 0 : (int)Favour.DocCount;
                int countReply = Favour == null ? 0 : (int)Reply.DocCount;
                int countSubscribe = Favour == null ? 0 : (int)Subscribe.DocCount;
                return Tuple.Create(countFavour, countSubscribe, countReply);
            }
            return Tuple.Create(0, 0, 0);
        }
    }
}
