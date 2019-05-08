using MD.Configuration;
using MD.Lib.Log;
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
    public static class EsCommunityManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsCommunityManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsCommunityConfig _config = null;
        static EsCommunityManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsCommunityConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsCommunityConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexCommunity>(_client, _config.IndexName, new IndexSettings()
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

        public static IndexCommunity GenObject(Community comm)
        {
            if (comm == null || comm.cid.Equals(Guid.Empty))
                return null;
            IndexCommunity indexComm = new IndexCommunity()
            {
                Id = comm.cid.ToString(),
                mid = comm.mid.ToString(),
                uid = comm.uid.ToString(),
                hits = comm.hits,
                praises = comm.praises,
                transmits = comm.transmits,
                createtime = comm.createtime,
                lastupdatetime = comm.lastupdatetime,
                topic_type = comm.topic_type,
                status = comm.status
            };
            if (comm.flag != null) indexComm.flag = comm.flag.Value;
            if (!string.IsNullOrEmpty(comm.title)) indexComm.title = comm.title;
            if (!string.IsNullOrEmpty(comm.content)) indexComm.content = comm.content;
            if (!string.IsNullOrEmpty(comm.imgs)) indexComm.imgs = comm.imgs.Split('|').ToList();
            return indexComm;
        }
        public static async Task<bool> AddOrUpdateAsync(IndexCommunity obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexCommunity>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexCommunity>((u) =>
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
        public static async Task<IndexCommunity> GetByIdAsync(Guid Cid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexCommunity>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(Cid))));
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
        /// 获取文章列表
        /// </summary>
        /// <param name="mid">商家mid</param>
        /// <param name="uid">用户uid，可为guid.empty</param>
        /// <param name="topic_type">主题类型(MMSQ)</param>
        /// <param name="flag">文章标签</param>
        /// <param name="pageIndex">pageIndex</param>
        /// <param name="pageSize">pageSize</param>
        /// <param name="status">status</param>
        /// <param name="queryStr">queryStr</param>
        /// <returns>Item1:总行数，Item2:列表</returns>
        public static async Task<Tuple<int, List<IndexCommunity>>> GetListAsync(Guid mid, Guid uid, int topic_type, int pageIndex, int pageSize, int status, string queryStr = "", int flag = 0,string strSort= "createtime")
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                QueryContainer container = null;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexGroup>.MatchAll();
                }
                else
                {
                    keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                    container = Query<IndexGroup>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                .OnFields(new[] { "KeyWords" }).Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                if (mid != Guid.Empty)
                {
                    var midContainer = Query<IndexCommunity>.Term("mid", mid.ToString());
                    container = container && midContainer;
                }
                var statusContainer = Query<IndexCommunity>.Term("status", status);
                var typeContainer = Query<IndexCommunity>.Term("topic_type", topic_type);
                container = container && statusContainer && typeContainer;
                if (flag > 0)
                {
                    var flagContainer = Query<IndexCommunity>.Term("flag", flag);
                    container = container && flagContainer;
                }
                if (uid != Guid.Empty)
                {
                    var uidContainer = Query<IndexCommunity>.Term("uid", uid.ToString());
                    container = container && uidContainer;
                }
                //search
                var result = await _client.SearchAsync<IndexCommunity>(s => s.Index(_config.IndexName).Query(container)
                .SortDescending(strSort)
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
            return Tuple.Create(0, new List<IndexCommunity>());
        }

        /// <summary>
        /// Item1:照片总数，Item2:点赞总数
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="uid"></param>
        /// <param name="topic_type"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, int>> GetCountAsync(Guid mid, Guid uid, int topic_type, int status)
        {
            try
            {
                //主搜索
                var midContainer = Query<IndexCommunity>.Term("mid", mid.ToString());
                var statusContainer = Query<IndexCommunity>.Term("status", status);
                var typeContainer = Query<IndexCommunity>.Term("topic_type", topic_type);
                midContainer = midContainer && statusContainer && typeContainer;
                if (uid != Guid.Empty)
                {
                    var uidContainer = Query<IndexCommunity>.Term("uid", uid.ToString());
                    midContainer = midContainer && uidContainer;
                }
                //search
                var result = await _client.SearchAsync<IndexCommunity>(s => s.Index(_config.IndexName).Query(midContainer).Size(1000000));
                var list = result.Documents;
                if (list.Count() > 0)
                {
                    return Tuple.Create(list.Sum(p => p.imgs == null ? 0 : p.imgs.Count), list.Sum(p => p.praises));
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, 0);
        }

        public static async Task<Tuple<int, List<IndexCommunity>>> GetListAsync(List<string> uidList, int topic_type, int pageIndex, int pageSize, int status)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                QueryContainer uidContainer = null;
                //主搜索
                var statusContainer = Query<IndexCommunity>.Term("status", status);
                var typeContainer = Query<IndexCommunity>.Term("topic_type", topic_type);
                if (uidList != null && uidList.Count > 0)
                {
                    foreach (var uid in uidList)
                    {
                        var uContainer = Query<IndexCommunity>.Term("uid", uid);
                        uidContainer = uidContainer || uContainer;
                    }
                }
                uidContainer = statusContainer && typeContainer && uidContainer;
                //search
                var result = await _client.SearchAsync<IndexCommunity>(s => s.Index(_config.IndexName).Query(uidContainer).SortDescending("createtime").Skip(from).Take(size));
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
            return Tuple.Create(0, new List<IndexCommunity>());
        }

        //public static async Task<Tuple<int, int>> GetByAggUidAsync(Guid mid, string uid, int topic_type, int? from = null, int? to = null)
        //{
        //    try
        //    {
        //        QueryContainer Container = null;
        //        var midContainer = Query<IndexCommunity>.Term("mid", mid);
        //        var typeContainer = Query<IndexCommunity>.Term("topic_type", topic_type);
        //        Container = midContainer && typeContainer;
        //        if (from != null && to != null)
        //        {
        //            var timeContainer = Query<IndexCommunity>.Range(r => r.OnField("createtime").GreaterOrEquals(from).LowerOrEquals(to));
        //            Container = Container && timeContainer;
        //        }
        //        var result = await _client.SearchAsync<IndexCommunity>(s => s.Index(_config.IndexName).Query(Container)
        //        .Aggregations(a => a.Terms("uids", aa => aa.Aggregations(bb=>bb.Sum("SumPraises",bbb=>bbb.Field(bbbb=>bbbb.praises))).Field(aaa => aaa.uid).Size(100000))));
        //        if (result.Total > 0)
        //        {
        //            var agg = result.Aggs.Terms("uids").Items;
        //            if (agg != null)
        //            {
        //                var user = agg.Where(p => p.Key == uid).FirstOrDefault();//user的groupby结果
        //                if (user != null)
        //                {
        //                    int userindex = agg.IndexOf(user) + 1;
        //                    return Tuple.Create((int)user.DocCount, userindex);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogError(ex);
        //    }
        //    return Tuple.Create(0, 0);
        //}
    }
}
