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
using MD.Lib.Util;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsNoticeBoardManager
    {
        public const string MMPTMid = "11111111-1111-1111-1111-111111111111";
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsNoticeBoardManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsNoticeBoardConfig _config = null;
        static EsNoticeBoardManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsNoticeBoardConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsNoticeBoardConfig>();

                init();
            }
        }
        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexNoticeBoard>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexNoticeBoard>(client, _config.IndexName, new IndexSettings()
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

        #region biz
        /// <summary>
        /// 从数据库中读取NoticeBoard重新赋值给IndexNoticeBoard
        /// </summary>
        /// <param name="nid">主键</param>
        /// <returns></returns>
        public static async Task<IndexNoticeBoard> GenObjectAsync(Guid nid)
        {
            using (var repo = new BizRepository())
            {
                NoticeBoard gObject = await repo.GetNoticeBoardAsync(nid);

                if (gObject != null && !gObject.nid.Equals(Guid.Empty))
                {
                    IndexNoticeBoard ret = new IndexNoticeBoard()
                    {
                        Id = gObject.nid.ToString(),
                        mid = gObject.mid.ToString(),
                        title = gObject.title,
                        status = gObject.status,
                        timestamp = gObject.timestamp,
                        hits_count = gObject.hits_count,
                        praise_count = gObject.praise_count,
                        transmit_count = gObject.transmit_count,
                        category = gObject.notice_category
                    };

                    if (gObject.source != null)
                        ret.source = gObject.source;
                    if (gObject.tag_1 != null)
                        ret.tag_1 = gObject.tag_1;
                    if (gObject.tag_2 != null)
                        ret.tag_2 = gObject.tag_2;
                    if (gObject.tag_3 != null)
                        ret.tag_3 = gObject.tag_3;
                    if (gObject.thumb_pic != null)
                        ret.thumb_pic = gObject.thumb_pic;
                    if (gObject.description != null)
                        ret.description = gObject.description;

                    ret.KeyWords = ret.title + "," + CommonHelper.ReplaceHtmlStr(ret.description);
                    return ret;
                }
            }
            return null;
        }

        /// <summary>
        /// 增加和修改ES
        /// </summary>
        /// <param name="notice">IndexNoticeBoard实体</param>
        /// <returns></returns>
        public static async Task<bool> AddOrUpdateAsync(IndexNoticeBoard notice)
        {
            try
            {
                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(notice.Id))));
                IndexNoticeBoard l = notice;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexNoticeBoard>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(notice);
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
        /// <param name="notice"></param>
        /// <returns></returns>
        public static bool AddOrUpdate(ElasticClient client, IndexNoticeBoard notice)
        {
            try
            {
                var result = client.Search<IndexNoticeBoard>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(notice.Id))));
                IndexNoticeBoard l = notice;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = client.Update<IndexNoticeBoard>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(notice);
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

        public static bool AddOrUpdate(IndexNoticeBoard notice)
        {
            try
            {
                var result = _client.Search<IndexNoticeBoard>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(notice.Id))));
                IndexNoticeBoard l = notice;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = _client.Update<IndexNoticeBoard>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(notice);
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

        /// <summary>
        /// 从ES中删除
        /// </summary>
        /// <param name="obj">IndexNoticeBoard实体</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync(IndexNoticeBoard obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexNoticeBoard>((u) =>
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


        public static async Task<IndexNoticeBoard> GetByidAsync(Guid nid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(nid.ToString()))));
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
        /// 总后台用于查询列表的总方法
        /// </summary>
        /// <param name="queryStr">q</param>
        /// <param name="pageCount">pageCount</param>
        /// <param name="size">size</param>
        /// <param name="status">status</param>
        /// <returns></returns>
        public static async Task<Tuple<int, List<Guid>>> SearchAsnyc(string queryStr, int pageCount, int size, int status)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexNoticeBoard>.MatchAll();
                }
                else
                {
                    container = Query<IndexNoticeBoard>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }
                int from = (pageCount - 1) * size;

                //主搜索
                var statusContainer = Query<IndexNoticeBoard>.Term("status", status);
                container = container && statusContainer;//取两个搜索结果交集

                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Index(_config.IndexName).Query(container).SortDescending("timestamp").Skip(from).Take(size));
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

        public static async Task<Tuple<int, List<Guid>>> SearchByMidAsnyc(Guid mid, string queryStr, int pageCount, int size, int status)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexNoticeBoard>.MatchAll();
                }
                else
                {
                    keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                    container = Query<IndexNoticeBoard>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }
                int from = (pageCount - 1) * size;

                if (mid != Guid.Empty)
                {
                    var midContainer = Query<IndexNoticeBoard>.Term("mid", mid.ToString());
                    container = container && midContainer;//取两个搜索结果交集
                }

                //主搜索
                var statusContainer = Query<IndexNoticeBoard>.Term("status", status);
                container = container && statusContainer;//取两个搜索结果交集

                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Index(_config.IndexName).Query(container).SortDescending("timestamp").Skip(from).Take(size));
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

        /// <summary>
        /// 用于微信端页面展示的调用
        /// </summary>
        /// <param name="pageCount">pageCount</param>
        /// <param name="size">size</param>
        /// <returns>当前页数据集合</returns>
        public static async Task<Tuple<int, List<IndexNoticeBoard>>> GetNoticeBoardAsync(Guid mid, int category, int pageCount, int size)
        {
            try
            {
                QueryContainer Container = Query<IndexNoticeBoard>.Terms("mid", new string[] { MMPTMid, mid.ToString() });
                var statusContainer = Query<IndexNoticeBoard>.Term("status", 1);
                Container = Container && statusContainer;
                if (category > 0)
                {
                    QueryContainer categoryContainer = Query<IndexNoticeBoard>.Term("category", category);
                    Container = Container && categoryContainer;
                }
                int from = (pageCount - 1) * size;
                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Index(_config.IndexName).Query(Container).SortDescending("timestamp").Skip(from).Take(size));
                var list = result.Documents.ToList();
                int totalCount = (int)result.Total;
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexNoticeBoard>());
        }
        /// <summary>
        ///  用于微信端页面展示的调用
        /// </summary>
        /// <param name="nid">主键</param>
        /// <returns>一条数据详情</returns>
        public static async Task<IndexNoticeBoard> GetNoticeBoardAsync(Guid nid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(nid.ToString()))));
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
        /// 根据点击量倒叙排序
        /// </summary>
        /// <param name="size">前N条记录</param>
        /// <returns></returns>
        public static async Task<List<IndexNoticeBoard>> GetNoticeBoardSortHisAsync(Guid mid, int size)
        {
            try
            {
                QueryContainer container = Query<IndexNoticeBoard>.Term("status", 1);
                QueryContainer midontainer = Query<IndexNoticeBoard>.Terms("mid", new string[] { MMPTMid, mid.ToString() });
                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Index(_config.IndexName).Query(container && midontainer).SortDescending("hits_count").Take(size));
                var list = result.Documents.ToList();
                return list;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }


        public static async Task<List<IndexNoticeBoard>> GetNoticeBoardList(Guid mid, int size, string sort)
        {
            try
            {
                QueryContainer container = Query<IndexNoticeBoard>.Term("status", 1);
                QueryContainer midontainer = Query<IndexNoticeBoard>.Terms("mid", new string[] { MMPTMid, mid.ToString() });
                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Index(_config.IndexName).Query(container && midontainer).SortDescending(sort).SortDescending("timestamp").Take(size));
                var list = result.Documents.ToList();
                return list;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        /// <summary>
        /// 测试用，查询所有的数据
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static async Task<List<IndexNoticeBoard>> GetAllNoticeBoardAsync()
        {
            try
            {
                var result = await _client.SearchAsync<IndexNoticeBoard>(s => s.Index(_config.IndexName).Size(1000000000));
                return result.Documents.ToList();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }
        #endregion
    }
}
