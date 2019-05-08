using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration;
using MD.Model.Index;
using MD.Model.Index.MD;
using MD.Model.MQ;
using Nest;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsBizLogStatistics
    {
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsBizLogStatistics), ex);
        }
        static ElasticClient _client = null;
        static LogESConfig _config = null;
        static EsBizLogStatistics()
        {
            _client = ESHeper.GetClient<LogESConfig>();
            if (_client == null)
            {
                var err = new Exception("_client没有正确初始化！");
                LogError(err);
                throw err;
            }

            _config = MdConfigurationManager.GetConfig<LogESConfig>();
            if (_config == null)
            {
                var err = new Exception("配置没有正确初始化！");
                LogError(err);
                throw err;
            }

            init();
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<BizIndex>(_client, _config.IndexName, new IndexSettings()
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

        #region mid

        public static void AddMidBizViewLog(string mid, string openId, Guid uid)
        {
            try
            {
                var message = mid;
                MDLogger.LogBiz(typeof(EsBizLogStatistics),
                    new BizMQ(ELogBizModuleType.MidView.ToString(), openId, uid, message, mid, null, null));
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(EsBizLogStatistics), new Exception($"AddMidBizViewLog" + ex.Message));
            }
        }

        #endregion

        #region gid
        public static void AddGidBizViewLog(string gid, string openId, Guid uid)
        {
            try
            {
                string message = gid;
                MDLogger.LogBiz(typeof(EsBizLogStatistics),
                    new BizMQ(ELogBizModuleType.GidView.ToString(), openId, uid, message, gid, null, null));
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"AddGidBizViewLog" + ex.Message));
            }
        }

        /// <summary>
        /// 统计活动分享
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="openId"></param>
        /// <param name="uid"></param>
        /// <param name="message"></param>
        public static void AddGroupShareViewLog(string gid, string openId, Guid uid,string message,string mid)
        {
            try
            {
                //string message = gid;
                MDLogger.LogBiz(typeof(EsBizLogStatistics),
                    new BizMQ(ELogBizModuleType.GroupShare.ToString(), openId, uid, message, gid, mid, null));
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(EsBizLogStatistics), new Exception($"AddPayBizViewLog" + ex.Message));
            }
        }
        #endregion

        /// <summary>
        /// 微信支付记录
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="openId"></param>
        /// <param name="uid"></param>
        /// <param name="mid"></param>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        public static void AddPayBizViewLog(string gid, string openId, Guid uid,Guid mid, double longitude, double latitude)
        {
            try
            {
                MDLogger.LogBiz(typeof(EsBizLogStatistics),
                    new BizMQ(ELogBizModuleType.PayView.ToString(), openId, uid, openId, gid, mid.ToString(), null,new Coordinate { Lon = longitude,Lat = latitude }));
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(EsBizLogStatistics), new Exception($"AddPayBizViewLog" + ex.Message));
                //throw new MDException(typeof(EsBizLogStatistics), new Exception($"AddPayBizViewLog" + ex.Message));
            }
        }

        /// <summary>
        /// 记录微信用户关注
        /// </summary>
        /// <param name="subType"></param>
        /// <param name="mp_id"></param>
        /// <param name="openId"></param>
        /// <param name="mid"></param>
        /// <param name="appid"></param>
        public static void AddSubBizViewLog(ELogBizModuleType subType,string mp_id, string openId, Guid mid,string appid)
        {
            try
            {
                MDLogger.LogBiz(typeof(EsBizLogStatistics),
                    new BizMQ(subType.ToString(), openId, mid, mp_id, appid, null, null));
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"AddSubBizViewLog" + ex.Message));
            }
        }

        public static async Task<Tuple<int, List<BizIndex>>> SearchBizViewAsnyc(ELogBizModuleType type, Guid bizId,
            Guid uid, double? from = null, double? to = null, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                int fromIndex = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                QueryContainer container = Query<BizIndex>.QueryString(q => q.Query($"ModelName:{type.ToString()}"));


                //条件
                // bizid
                if (!bizId.Equals(Guid.Empty))
                {
                    var unUsedContainer = Query<BizIndex>.Term("UnUsed1", bizId.ToString());
                    container = container && unUsedContainer;
                }

                //uid
                if (!uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<BizIndex>.Term("UserUuid", uid.ToString());
                    container = container && uidContainer;
                }

                //timestamp
                if (from != null && to != null)
                {
                    string f = CommonHelper.FromUnixTime(from.Value).ToString("O");
                    string t = CommonHelper.FromUnixTime(to.Value).ToString("O");

                    var timeContainer = Query<BizIndex>.Range(r => r.OnField("TimeStamp").GreaterOrEquals(f).LowerOrEquals(t));
                    container = container && timeContainer;
                }

                //search
                var result =
                    await
                        _client.SearchAsync<BizIndex>(
                            s =>
                                s.Index(_config.IndexName).Type("biz")
                                    .Query(container)
                                    .SortDescending("TimeStamp")
                                    .Skip(fromIndex)
                                    .Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
                return Tuple.Create((int)result.Total, new List<BizIndex>());
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"SearchBizViewAsnyc,from:{from},to:{to}" + ex.Message));
            }
        }

        public static async Task<Tuple<int, List<BizIndex>>> SearchBizViewAsnyc(ELogBizModuleType type, Guid bizId,
            Guid uid,Guid mid, double? from = null, double? to = null, int pageIndex = 1, int pageSize = 10,string message = null)
        {
            try
            {
                int fromIndex = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                QueryContainer container = Query<BizIndex>.QueryString(q => q.Query($"ModelName:{type.ToString()}"));


                //条件
                // bizid
                if (!bizId.Equals(Guid.Empty))
                {
                    var unUsedContainer = Query<BizIndex>.Term("UnUsed1", bizId.ToString());
                    container = container && unUsedContainer;
                }
                if (!mid.Equals(Guid.Empty))
                {
                    var unUsed2Container = Query<BizIndex>.Term("UnUsed2", mid.ToString());
                    container = container && unUsed2Container;
                }
                //uid
                if (!uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<BizIndex>.Term("UserUuid", uid.ToString());
                    container = container && uidContainer;
                }
                if (!string.IsNullOrEmpty(message))
                {
                    //var messageContainer = Query<BizIndex>.QueryString(q => q.Query($"Message:{message}"));
                    var messageContainer = Query<BizIndex>.Term("Message",message);
                    container = container && messageContainer;
                }
                //timestamp
                if (from != null && to != null)
                {
                    string f = CommonHelper.FromUnixTime(from.Value).ToString("O");
                    string t = CommonHelper.FromUnixTime(to.Value).ToString("O");

                    var timeContainer = Query<BizIndex>.Range(r => r.OnField("TimeStamp").GreaterOrEquals(f).LowerOrEquals(t));
                    container = container && timeContainer;
                }

                //search
                var result =
                    await
                        _client.SearchAsync<BizIndex>(
                            s =>
                                s.Index(_config.IndexName).Type("biz")
                                    .Query(container)
                                    .SortDescending("TimeStamp")
                                    .Skip(fromIndex)
                                    .Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
                return Tuple.Create((int)result.Total, new List<BizIndex>());
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"SearchBizViewAsnyc,from:{from},to:{to}" + ex.Message));
            }
        }

        public static Tuple<int, List<BizIndex>> SearchBizView(ELogBizModuleType type, Guid bizId,
            Guid uid, double? from = null, double? to = null, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                int fromIndex = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                QueryContainer container = Query<BizIndex>.QueryString(q => q.Query($"ModelName:{type.ToString()}"));


                //条件
                // bizid
                if (!bizId.Equals(Guid.Empty))
                {
                    var unUsedContainer = Query<BizIndex>.Term("UnUsed1", bizId.ToString());
                    container = container && unUsedContainer;
                }

                //uid
                if (!uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<BizIndex>.Term("UserUuid", uid.ToString());
                    container = container && uidContainer;
                }

                //timestamp
                if (from != null && to != null)
                {
                    string f = CommonHelper.FromUnixTime(from.Value).ToString("O");
                    string t = CommonHelper.FromUnixTime(to.Value).ToString("O");

                    var timeContainer = Query<BizIndex>.Range(r => r.OnField("TimeStamp").GreaterOrEquals(f).LowerOrEquals(t));
                    container = container && timeContainer;
                }

                //search
                var result =

                        _client.Search<BizIndex>(
                            s =>
                                s.Index(_config.IndexName).Type("biz")
                                    .Query(container)
                                    .SortDescending("TimeStamp")
                                    .Skip(fromIndex)
                                    .Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
                return Tuple.Create((int)result.Total, new List<BizIndex>());
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"SearchBizView,from:{from},to:{to}" + ex.Message));
            }
        }

        public static Tuple<int, List<BizIndex>> SearchBizByModelNameView(string ModelName, Guid bizId,
            Guid uid, double? from = null, double? to = null, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                int fromIndex = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                QueryContainer container = Query<BizIndex>.QueryString(q => q.Query($"ModelName:{ModelName}"));


                //条件
                // bizid
                if (!bizId.Equals(Guid.Empty))
                {
                    var unUsedContainer = Query<BizIndex>.Term("UnUsed1", bizId.ToString());
                    container = container && unUsedContainer;
                }

                //uid
                if (!uid.Equals(Guid.Empty))
                {
                    var uidContainer = Query<BizIndex>.Term("UserUuid", uid.ToString());
                    container = container && uidContainer;
                }

                //timestamp
                if (from != null && to != null)
                {
                    string f = CommonHelper.FromUnixTime(from.Value).ToString("O");
                    string t = CommonHelper.FromUnixTime(to.Value).ToString("O");

                    var timeContainer = Query<BizIndex>.Range(r => r.OnField("TimeStamp").GreaterOrEquals(f).LowerOrEquals(t));
                    container = container && timeContainer;
                }

                //search
                var result =
                        _client.Search<BizIndex>(
                            s =>
                                s.Index(_config.IndexName).Type("biz")
                                    .Query(container)
                                    .SortDescending("TimeStamp")
                                    .Skip(fromIndex)
                                    .Take(size));
                var list = result.Documents.ToList();
                if (list.Count > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
                return Tuple.Create((int)result.Total, new List<BizIndex>());
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"SearchBizView,from:{from},to:{to}" + ex.Message));
            }
        }

        public class BizOpenidComparer : IEqualityComparer<BizIndex>
        {
            public bool Equals(BizIndex x, BizIndex y)
            {
                if (x == null)
                    return y == null;
                return x.OpenId == y.OpenId;
            }

            public int GetHashCode(BizIndex obj)
            {
                if (obj == null)
                    return 0;
                return obj.OpenId.GetHashCode();
            }
        }

        public static async Task<Bucket<KeyItem>> GetMTopN(int n)
        {
            try
            {
                var result = await _client.SearchAsync<BizIndex>(s => s
                    .Index(_config.IndexName).Type("biz")
                    .QueryString("ModelName:MidView")
                    .Aggregations(a => a
                        .Terms("uuid", sa => sa
                            .Field(p => p.UnUsed1).Size(n)
                        )
                    )
                    );

                var agg = result.Aggs.Terms("uuid");
                return agg;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"GetMTopN" + ex.Message));
            }
        }

        public static async Task<Bucket<KeyItem>> GetGAccessTopN(int n)
        {
            try
            {
                var result = await _client.SearchAsync<BizIndex>(s => s
                    .Index(_config.IndexName).Type("biz")
                    .QueryString("ModelName:GidView")
                    .Aggregations(a => a
                        .Terms("uuid", sa => sa
                            .Field(p => p.UnUsed1).Size(n)
                        )
                    )
                    );

                var agg = result.Aggs.Terms("uuid");
                return agg;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"GetGAccessTopN" + ex.Message));
            }
        }

        public static async Task<long> GetTotalCount(ELogBizModuleType type)
        {
            try
            {
                var result = await _client.SearchAsync<BizIndex>(s => s
                    .Index(_config.IndexName).Type("biz")
                    .QueryString($"ModelName:{type.ToString()}")
                    );

                var agg = result.Total;
                return agg;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"GetTotalCount" + ex.Message));
            }
        }

        public static async Task<long> GetActiveUserCount(double from, double to)
        {
            try
            {
                DateTime fDateTime = CommonHelper.FromUnixTime(from);
                DateTime tDateTime = CommonHelper.FromUnixTime(to);

                var timeContainer = Query<BizIndex>.Range(r => r.OnField("TimeStamp").GreaterOrEquals(CommonHelper.GetLoggerDateTime(fDateTime)).LowerOrEquals(CommonHelper.GetLoggerDateTime(tDateTime)));
                var modelContainer = Query<BizIndex>.Term(t => t.ModelName, "MidView");

                var result = await _client.SearchAsync<BizIndex>(s => s
                    .Index(_config.IndexName).Type("biz")
                    .Query(timeContainer && modelContainer)
                    //.QueryString($"ModelName:MidView")
                    //.Filter(f=>f.Range(r=>r.OnField("TimeStamp").GreaterOrEquals(CommonHelper.GetLoggerDateTime(fDateTime)).LowerOrEquals(CommonHelper.GetLoggerDateTime(tDateTime))))
                    .Aggregations(a => a.Terms("openid", sa => sa.Field(p => p.OpenId).Size(1000000)))
                    );

                var agg = result.Aggs.Terms("openid");
                return agg.Items.Count;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(EsBizLogStatistics), new Exception($"GetActiveUserCount,from:{from},to:{to}" + ex.Message));
            }
        }

        public static async Task<int> GetViewCount(Guid mid, double? timeStart = null, double? timeEnd = null)
        {
            try
            {
                var midContainer = Query<BizIndex>.Term("UnUsed1", mid.ToString());
                if (timeStart != null && timeEnd != null)
                {
                    string f = "", t = "";
                    if (timeStart.ToString().Length > 10 || timeEnd.ToString().Length > 10)//说明时间戳有问题，默认今天
                    {
                        f = DateTime.Today.ToString("O");
                        t = DateTime.Now.ToString("O");
                    }
                    else
                    {
                        f = CommonHelper.FromUnixTime(timeStart.Value).ToString("O");
                        t = CommonHelper.FromUnixTime(timeEnd.Value).ToString("O");
                    }
                    var timeContainer = Query<BizIndex>.Range(r => r.OnField("TimeStamp").GreaterOrEquals(f).LowerOrEquals(t));
                    midContainer = midContainer && timeContainer;

                    var result = await _client.SearchAsync<BizIndex>(s => s.Index(_config.IndexName).Query(midContainer).Take(0));
                    return (int)result.Total;
                }
                return 0;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(EsBizLogStatistics), new Exception($"GetViewCount,from:{timeStart},to:{timeEnd}" + ex.Message));
            }
        }

        public static async Task<Bucket<HistogramItem>> GetDateHistogram(DateTime timeStart, DateTime timeEnd,Guid mid,string ModelName)
        {
            var midContainer = Query<BizIndex>.Term(b=>b.UnUsed1, mid.ToString());
            var timeContainer = Query<BizIndex>.Range(r => r.OnField(b => b.TimeStamp).GreaterOrEquals(CommonHelper.GetLoggerDateTime(timeStart)).LowerOrEquals(CommonHelper.GetLoggerDateTime(timeEnd)));
            var modelContainer = Query<BizIndex>.Term(t => t.ModelName, ModelName);
            var result = await _client.SearchAsync<BizIndex>(s => s
                                .Index(_config.IndexName).Type("biz")
                                .Query(midContainer && modelContainer && timeContainer)
                                .Aggregations(a => a
                                    .DateHistogram("my_date_histogram", h => h
                                        .Field(p => p.TimeStamp)
                                        .Interval("day")
                                        .Format("yyyy-MM-dd")
                                        .MinimumDocumentCount(0)
                                    )
                                )
                            );
            var agg = result.Aggs.DateHistogram("my_date_histogram");
            return agg;
        }

        public static async Task<Bucket<HistogramItem>> GetGroupShareDateHistogram(DateTime timeStart, DateTime timeEnd, Guid mid,Guid gid, string message=null)
        {
            var midContainer = Query<BizIndex>.Term(b => b.UnUsed2, mid.ToString());
            if (!gid.Equals(Guid.Empty))
                midContainer = midContainer && Query<BizIndex>.Term(b => b.UnUsed1, gid.ToString());
            var timeContainer = Query<BizIndex>.Range(r => r.OnField(b => b.TimeStamp).GreaterOrEquals(CommonHelper.GetLoggerDateTime(timeStart)).LowerOrEquals(CommonHelper.GetLoggerDateTime(timeEnd)));
            var modelContainer = Query<BizIndex>.Term(t => t.ModelName, ELogBizModuleType.GroupShare.ToString());
            if (!string.IsNullOrEmpty(message))
                modelContainer = modelContainer && Query<BizIndex>.Term(t => t.Message, message);
            var result = await _client.SearchAsync<BizIndex>(s => s
                                .Index(_config.IndexName).Type("biz")
                                .Query(midContainer && modelContainer && timeContainer)
                                .Aggregations(a => a
                                    .DateHistogram("my_date_histogram", h => h
                                        .Field(p => p.TimeStamp)
                                        .Interval("day")
                                        .Format("yyyy-MM-dd")
                                        .MinimumDocumentCount(0)
                                        .Aggregations(b => b
                                        .Terms("group", sa => sa.Field(p => p.Message).Size(100000)))
                                    )
                                )
                            );
            var agg = result.Aggs.DateHistogram("my_date_histogram");
            return agg;
        }

        //public static Tuple<int,int> GetGroupShareCount(Guid gid)
        //{
        //    //主搜索
        //    QueryContainer container = Query<BizIndex>.Term("ModelName",ELogBizModuleType.GroupShare.ToString());
        //    QueryContainer gidContainer = Query<BizIndex>.Term("UnUsed1", gid.ToString());
        //    container = container && gidContainer;
        //    var result =
        //                _client.Search<BizIndex>(
        //                    s =>
        //                        s.Index(_config.IndexName).Type("biz")
        //                            .Query(container)
        //                            .Aggregations(a=>a.Terms("group",b=>b.Field(p=>p.Message).Size(10000))));
        //    var agg = result.Aggs.Terms("group");
        //    int groupIndex = 0;
        //    int groupJoin = 0;
        //    if (agg != null && agg.Items.Count > 0)
        //    {
        //        string index = ShareType.GroupIndex.ToString().ToLower();
        //        string join = ShareType.GroupJoin.ToString().ToLower();
        //        var itemIndex = agg.Items.Where(a => a.Key == index).FirstOrDefault();
        //        var itemJoin = agg.Items.Where(a => a.Key == join).FirstOrDefault();
        //        groupIndex = Convert.ToInt32(itemIndex == null ? 0 : itemIndex.DocCount);
        //        groupJoin = Convert.ToInt32(itemJoin == null ? 0 : itemJoin.DocCount);
        //    }
        //    return Tuple.Create(groupIndex, groupJoin);
        //}
        public static Tuple<int, int> GetGroupShareCount(Guid gid)
        {
            //主搜索
            QueryContainer container = Query<BizIndex>.Term("ModelName", ELogBizModuleType.GroupShare.ToString());
            QueryContainer gidContainer = Query<BizIndex>.Term("UnUsed1", gid.ToString());
            container = container && gidContainer;
            var result =
                        _client.Search<BizIndex>(
                            s =>
                                s.Index(_config.IndexName).Type("biz")
                                    .Query(container).Take(10000)); 
            int groupIndex = 0;
            int groupJoin = 0;
            var list = result.Documents.ToList();
            if (list != null && list.Count > 0)
            {
                string index = ShareType.GroupIndex.ToString();
                string join = ShareType.GroupJoin.ToString();
                groupIndex = list.Count(b => b.Message == index);
                groupJoin = list.Count(b => b.Message == join);
            }
            return Tuple.Create(groupIndex, groupJoin);
        }
        //public static async Task<Dictionary<string, long>> GetUserIndexShareCount(Guid gid)
        //{
        //    //主搜索
        //    QueryContainer container = Query<BizIndex>.Term("ModelName", ELogBizModuleType.GroupShare.ToString());
        //    QueryContainer gidContainer = Query<BizIndex>.Term("UnUsed1", gid.ToString());
        //    QueryContainer messageContainer = Query<BizIndex>.QueryString(q => q.Query($"Message:{ShareType.GroupIndex.ToString()}"));
        //    container = container && gidContainer && messageContainer;
        //    var result =
        //               await _client.SearchAsync<BizIndex>(
        //                    s =>s.Index(_config.IndexName).Type("biz")
        //                            .Query(container)
        //                            .Aggregations(a => a.Terms("group", b => b.Field(p => p.UserUuid).Size(1000))));
        //    var agg = result.Aggs.Terms("group");
        //    Dictionary<string, long> dic = new Dictionary<string, long>();
        //    if (agg != null && agg.Items.Count > 0)
        //    {
        //        dic = agg.Items.ToDictionary(item => item.Key, item => item.DocCount);
        //    }
        //    return dic;
        //}

        public static async Task<Dictionary<string, int>> GetUserIndexShareCount(Guid gid)
        {
            //主搜索
            QueryContainer container = Query<BizIndex>.Term("ModelName", ELogBizModuleType.GroupShare.ToString());
            QueryContainer gidContainer = Query<BizIndex>.Term("UnUsed1", gid.ToString());
            QueryContainer messageContainer = Query<BizIndex>.QueryString(q => q.Query($"Message:{ShareType.GroupIndex.ToString()}"));
            container = container && gidContainer && messageContainer;
            var result =
                       await _client.SearchAsync<BizIndex>(
                            s => s.Index(_config.IndexName).Type("biz")
                                    .Query(container)
                                    .Size(10000));
            var list = result.Documents.ToList();
            Dictionary<string, int> dic = new Dictionary<string, int>();
            if (list != null && list.Count > 0)
            {
                dic = list.GroupBy(b => b.UserUuid).ToDictionary(item => item.Key, item => item.Count());
            }
            return dic;
        }

        public static async Task<Bucket<HistogramItem>> GetHourHistogram(DateTime timeStart, DateTime timeEnd, Guid mid)
        {
            var midContainer = Query<BizIndex>.Term(b => b.UnUsed1, mid.ToString());
            var timeContainer = Query<BizIndex>.Range(r => r.OnField(b => b.TimeStamp).GreaterOrEquals(CommonHelper.GetLoggerDateTime(timeStart)).LowerOrEquals(CommonHelper.GetLoggerDateTime(timeEnd)));
            var result = await _client.SearchAsync<BizIndex>(s => s
                                .Index(_config.IndexName).Type("biz")
                                .Query(timeContainer && midContainer)
                                .Aggregations(a => a
                                    .DateHistogram("my_date_histogram", h => h
                                        .Field(p => p.TimeStamp)
                                        .Interval("hour")
                                        .Format("HH:mm:ss")
                                        .TimeZone("+08:00")
                                        .MinimumDocumentCount(0)
                                        
                                    )
                                )
                            );

            var agg = result.Aggs.DateHistogram("my_date_histogram");
            return agg;
        }

        public static async Task<Tuple<int, List<BizIndex>>> GetUsersNear(Guid mid,double lat, double lon)
        {
            var midContainer = Query<BizIndex>.Term(b => b.UnUsed2, mid.ToString());
            var modelContainer = Query<BizIndex>.Term(t => t.ModelName, ELogBizModuleType.PayView.ToString());
            var latContainer = Query<BizIndex>.Range(t => t.OnField(b => b.Location.Lat).GreaterOrEquals(lat - 0.1).LowerOrEquals(lat + 0.1));
            var lonContainer = Query<BizIndex>.Range(t => t.OnField(b => b.Location.Lon).GreaterOrEquals(lon - 0.1).LowerOrEquals(lon + 0.1));
            //var result = await _client.SearchAsync<BizIndex>(s=>s.Index(_config.IndexName).Type("biz")
            //                    .Query(midContainer && modelContainer).SortGeoDistance(b=>b.OnField("Location").Ascending().PinTo(lat,lon)));
            var result = await _client.SearchAsync<BizIndex>(s => s.Index(_config.IndexName).Type("biz")
                                .Query(midContainer && modelContainer && latContainer && lonContainer).Size(200).Source(so => so.Include(new string[] { "UserUuid", "Location" })));
            var list = result.Documents.ToList();
            if (list.Count > 0)
            {
                return Tuple.Create((int)result.Total, list);
            }
            return Tuple.Create(0,new List<BizIndex>());
        }
    }
}
