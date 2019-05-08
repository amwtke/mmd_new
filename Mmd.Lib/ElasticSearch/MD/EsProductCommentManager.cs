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
    public static class EsProductCommentManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsProductCommentManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsProductCommentConfig _config = null;
        static EsProductCommentManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsProductCommentConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsProductCommentConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexProductComment>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexProductComment>(client, _config.IndexName, new IndexSettings()
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

        public static IndexProductComment GenObject(ProductComment obj)
        {
            if (obj != null && !obj.pcid.Equals(Guid.Empty))
            {
                IndexProductComment ret = new IndexProductComment();
                ret.Id = obj.pcid.ToString();
                ret.pid = obj.pid.ToString();
                ret.uid = obj.uid.ToString();
                ret.mid = obj.mid.ToString();
                if (obj.u_age != null) ret.u_age = obj.u_age.Value;
                if (obj.u_skin != null) ret.u_skin = obj.u_skin.Value;
                if (obj.score != null) ret.score = obj.score.Value;
                if (obj.comment != null) ret.comment = obj.comment;
                if (obj.comment_reply != null) ret.comment_reply = obj.comment_reply;
                if (obj.imglist != null) ret.imglist = obj.imglist;
                if (obj.isessence != null) ret.isessence = obj.isessence.Value;
                if (obj.praise_count != null) ret.praise_count = obj.praise_count.Value;
                if (obj.timestamp != null) ret.timestamp = obj.timestamp.Value;
                if (obj.timestamp_reply != null) ret.timestamp_reply = obj.timestamp_reply;
                if (!string.IsNullOrEmpty(obj.imglist)) ret.imglist = obj.imglist;
                ret.KeyWords = obj.comment;
                return ret;
            }
            return null;
        }

        public static async Task<bool> AddOrUpdateAsync(IndexProductComment obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexProductComment>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexProductComment l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexProductComment>((u) =>
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

        public static async Task<IndexProductComment> GetByPcidAsync(Guid pcid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexProductComment>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(pcid))));
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
        public static async Task<Tuple<int, List<IndexProductComment>>> GetByUidAsync(Guid Uid, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                var result = await _client.SearchAsync<IndexProductComment>(s => s.Query(q => q.Term(t => t.OnField("uid").Value(Uid)))
                .SortDescending("timestamp").Skip(from).Take(size));
                if (result.Total >= 1)
                {
                    var list = result.Documents.ToList();
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, list);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexProductComment>());
        }

        public static async Task<Tuple<int, List<IndexProductComment>, List<KeyItem>, List<KeyItem>>> SearchAsync(string queryStr, Guid pid, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_smart", keywords);
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索
                var container = Query<IndexProductComment>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                .OnFields(new[] { "KeyWords" })
                .Analyzer("ik_smart"));
                //pid
                var pidContainer = Query<IndexProductComment>.Term("pid", pid);
                container = container && pidContainer;
                var result = await _client.SearchAsync<IndexProductComment>(s => s.Index(_config.IndexName).Query(container)
                 .Aggregations(a => a.Terms("u_age", sa => sa.Field(g => g.u_age).Size(100))
                                     .Terms("u_skin", sb => sb.Field(g => g.u_skin).Size(10)))
                .SortDescending("isessence").SortDescending("timestamp").Skip(from).Take(size));
                var list = result.Documents.ToList();
                int totalCount = (int)result.Total;
                var agg_age = result.Aggs.Terms("u_age").Items.ToList();
                var agg_skin = result.Aggs.Terms("u_skin").Items.ToList();
                return Tuple.Create(totalCount, list, agg_age, agg_skin);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }
        public static async Task<Tuple<int, List<IndexProductComment>>> GetByPidAsync(Guid pid, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;
                var result = await _client.SearchAsync<IndexProductComment>(s => s.Query(q => q.Term(t => t.OnField("pid").Value(pid)))
                .SortDescending("isessence").SortDescending("timestamp").Skip(from).Take(size));
                if (result.Total > 0)
                {
                    int totalCount = (int)result.Total;
                    return Tuple.Create(totalCount, result.Documents.ToList());
                }
            }
            catch (Exception ex)
            {
                LogError(new Exception($"fun:GetByPidAsync,ex:{ex.Message}"));
            }
            return Tuple.Create(0, new List<IndexProductComment>());
        }

        public static async Task<IndexProductComment> GetByPidAndUidAsync(Guid pid, Guid uid)
        {
            try
            {
                var pidContainer = Query<IndexProductComment>.Term("pid", pid);
                var uidContainer = Query<IndexProductComment>.Term("uid", uid);
                pidContainer = pidContainer && uidContainer;
                var result = await _client.SearchAsync<IndexProductComment>(s => s.Index(_config.IndexName).Query(pidContainer));
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
        /// 获取总条目和平均分
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static async Task<Tuple<long, double>> GetByTotalAndAvgScoreAsync(Guid pid)
        {
            var pidContainer = Query<IndexProductComment>.Term("pid", pid);
            var result = await _client.SearchAsync<IndexProductComment>(s => s.Index(_config.IndexName).Query(pidContainer)
            .Aggregations(a => a.Average("avgScore", sa => sa.Field(g => g.score)))
            );
            var total = result.Total;
            var avgScore = result.Aggs.Average("avgScore").Value;
            return Tuple.Create(total, avgScore == null ? 0 : avgScore.Value);
        }

        public static async Task<bool> DelComment(Guid pcid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexProductComment>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(pcid))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexProductComment>((u) =>
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
    }
}
