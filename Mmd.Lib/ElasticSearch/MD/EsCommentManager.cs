using MD.Configuration;
using MD.Lib.DB.Redis.MD;
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
    public static class EsCommentManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsCommentManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsCommentConfig _config = null;
        static EsCommentManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsCommentConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsCommentConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexComment>(_client, _config.IndexName, new IndexSettings()
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

        public static async Task<bool> addCommentAsync(IndexComment comment)
        {
            try
            {
                var resoponse = await _client.IndexAsync(comment, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static async Task<bool> AddReplyAsync(IndexComment index)
        {
            try
            {
                var r = await _client.UpdateAsync<IndexComment>((u) =>
                    {
                        u.Id(index.Id);
                        u.Doc(index);
                        u.Index(_config.IndexName);
                        return u;
                    });
                return r.IsValid;

            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static async Task<IndexComment> GetByIdAsync(Guid Id)
        {
            try
            {
                var result = await _client.SearchAsync<IndexComment>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(Id.ToString()))));
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

        public static async Task<Tuple<int, List<IndexComment>>> GetCommentByTopic_IdAsync(Guid from_mid, Guid topic_id, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                var topicidContainer = Query<IndexComment>.Term("topic_id", topic_id);
                if (from_mid != null && !from_mid.Equals(Guid.Empty))
                {
                    var from_midContainer = Query<IndexComment>.Term("from_mid", from_mid);
                    topicidContainer = topicidContainer && from_midContainer;
                }
                var result = await _client.SearchAsync<IndexComment>(s => s.Index(_config.IndexName).Query(topicidContainer).SortAscending("timestamp").Skip(from).Take(size));
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
            return Tuple.Create(0, new List<IndexComment>());
        }

        public static async Task<bool> AddOrUpdateAsync(IndexComment obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexComment>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexComment>((u) =>
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

        public static async Task<Tuple<int,List<IndexComment>>> GetReplylistByUidAsync(Guid uid,int bizType, int pageIndex, int pageSize)
        {
            try
            {
                var tupleBiz = await EsCommunityBizManager.GetCommunityBizListAsync(uid, Guid.Empty, Guid.Empty,new List<int> {(int)EComBizType.Comment,(int)EComBizType.Reply }, pageIndex, pageSize);
                var listBiz = tupleBiz.Item2;
                int totalCount = tupleBiz.Item1;
                //var uidContainer = Query<IndexComment>.Nested(c => c.Path("com_replys").Filter(r => r.Term("to_uid", uid.ToString())));
                //var result = await _client.SearchAsync<IndexComment>(s => s.Index(_config.IndexName).Query(uidContainer).SortDescending(c=>c.Com_Replys.Max(r=>r.timestamp)).Size(100));
                //var list = result.Documents.ToList();
                //if (list.Count > 0)
                //{
                //    int totalCount = (int)result.Total;
                //    return Tuple.Create(totalCount,list);
                //}
                if (listBiz != null && listBiz.Count > 0)
                {
                    List<string> listCommentId = listBiz.Where(a => a.biztype == (int)EComBizType.Reply).Select(b => b.extralid).ToList();
                    List<string> listReplyId = listBiz.Where(a => a.biztype == (int)EComBizType.Reply).Select(b => b.bizid).ToList();
                    List<string> listCommentId2 = listBiz.Where(a => a.biztype == (int)EComBizType.Comment).Select(b => b.bizid).ToList();
                    listCommentId = listCommentId.Concat(listCommentId2).ToList();
                    QueryContainer Container = null; 
                    listCommentId.ForEach(id =>
                    {
                        var commentContainer = Query<IndexComment>.Term(c => c.Id, id);
                        Container = Container || commentContainer;
                    });
                    var result = await _client.SearchAsync<IndexComment>(s => s.Index(_config.IndexName).Query(Container).Size(pageSize));
                    var list = result.Documents.ToList();
                    return Tuple.Create(totalCount, list);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexComment>());
        }
    }
}
