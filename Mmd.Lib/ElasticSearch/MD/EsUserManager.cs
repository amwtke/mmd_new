using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB;
using MD.Model.Index.MD;
using Nest;
using MD.Lib.Util;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsUserManager
    {
        static readonly object LockObject = new object();

        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsUserManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsUserConfig _config = null;
        static EsUserManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsUserConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsUserConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexUser>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexUser>(client, _config.IndexName, new IndexSettings()
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

        public static IndexUser GenObject(User obj)
        {
            if (obj != null && !obj.uid.Equals(Guid.Empty) && !string.IsNullOrEmpty(obj.openid))
            {
                IndexUser ret = new IndexUser();
                ret.Id = obj.uid.ToString();
                ret.address = obj.address;
                ret.b_day = obj.b_day;
                ret.b_month = obj.b_month;
                ret.b_year = obj.b_year;
                ret.cell_phone = obj.cell_phone;
                ret.email = obj.email;
                ret.membership_card = obj.membership_card;
                ret.mid = obj.mid.ToString();
                ret.mmd_account = obj.mmd_account;
                ret.mmd_password = obj.mmd_password;
                ret.mmd_salt = obj.mmd_salt;
                ret.name = obj.name;
                ret.openid = obj.openid;
                ret.sex = obj.sex;
                ret.wcard = obj.wcard;
                ret.wx_appid = obj.wx_appid;
                ret.register_time = obj.register_time;
                ret.KeyWords = ret.name + "_" + ret.address;
                ret.backimg = obj.backimg;
                if (obj.age != null) ret.age = obj.age.Value;
                if (obj.skin != null) ret.skin = obj.skin.Value;
                return ret;
            }
            return null;
        }

        /// <summary>
        /// 根据openid来判断
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static async Task<bool> AddOrUpdateAsync(IndexUser obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("openid").Value(obj.openid))));

                IndexUser l = obj;
                //更新
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexUser>((u) =>
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
        /// <param name="Client"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool AddOrUpdate(ElasticClient Client, IndexUser obj)
        {
            try
            {
                var result = Client.Search<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("openid").Value(obj.openid))));

                IndexUser l = obj;
                //更新
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = Client.Update<IndexUser>((u) =>
                    {
                        u.Id(_id);
                        u.Doc(obj);
                        u.Index(_config.IndexName);
                        return u;
                    });
                    return r.IsValid;
                }
                var resoponse = Client.Index(l, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return false;
        }

        public static async Task<bool> DeleteAsync(string openId)
        {
            try
            {
                var result = await _client.SearchAsync<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("openid").Value(openId))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexUser>((u) =>
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

        public static async Task<bool> DeleteAsync(Guid uid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(uid))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexUser>((u) =>
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

        public static async Task<IndexUser> GetByIdAsync(Guid uid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(uid))));
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

        public static IndexUser GetById(Guid uid)
        {
            try
            {
                var result = _client.Search<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(uid))));
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

        public static async Task<IndexUser> GetByOpenIdAsync(string openId)
        {
            try
            {
                var result = await _client.SearchAsync<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("openid").Value(openId))));
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

        public static IndexUser GetByOpenId(string openId)
        {
            try
            {
                var result = _client.Search<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("openid").Value(openId))));
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
        /// 可以查name与address字段.查询一个商铺底下的所有用户信息.
        /// </summary>
        /// <param name="queryStr"></param>
        /// <param name="mid"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, List<Guid>>> SearchAsnyc(string queryStr, Guid mid, int pageIndex, int pageSize)
        {
            try
            {
                string keywords = $"{queryStr.Trim()}";
                keywords = await ESHeper.AnalyzeQueryString(_client, _config.IndexName, "ik_max_word", keywords);
                QueryContainer container;
                if (string.IsNullOrEmpty(keywords))
                {
                    container = Query<IndexUser>.MatchAll();
                }
                else
                {
                    container = Query<IndexUser>.QueryString(q => q.Query(keywords).DefaultOperator(Operator.And)
                        .OnFields(new[] { "KeyWords" })
                        .Analyzer("ik_smart"));
                }

                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                //主搜索

                var midContainer = Query<IndexUser>.Term("mid", mid.ToString());
                container = container && midContainer;

                //search
                var result = await _client.SearchAsync<IndexUser>(s => s.Index(_config.IndexName).Query(container).SortDescending("register_time")
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

        public static async Task<List<Guid>> GetRobot()
        {
            List<Guid> guidList = new List<Guid>();
            try
            {
                var result = await _client.SearchAsync<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("mid").Value("11111111-1111-1111-1111-111111111111"))));
                if (result.Total >= 1)
                {
                    foreach (var user in result.Documents)
                    {
                        guidList.Add(Guid.Parse(user.Id));
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return guidList;
        }

        public static async Task<List<IndexUser>> GetByMidAsync(Guid mid,int size)
        {
            try
            {
                var result = await _client.SearchAsync<IndexUser>(s => s.Query(q => q.Term(t => t.OnField("mid").Value(mid))).Size(size));
                if (result.Total >= 1)
                {
                    return result.Documents.ToList();
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        public static async Task<List<IndexUser>> GetByMidAsync(Guid mid,DateTime timeStart, DateTime timeEnd)
        {
            var midContainer = Query<IndexUser>.Term(b => b.mid, mid.ToString());
            double start = CommonHelper.ToUnixTime(timeStart);
            double end = CommonHelper.ToUnixTime(timeEnd);
            var timeContainer = Query<IndexUser>.Range(r => r.OnField(b => b.register_time).GreaterOrEquals(start).LowerOrEquals(end));
            var result = await _client.SearchAsync<IndexUser>(s => s
                                .Index(_config.IndexName)
                                .Query(midContainer && timeContainer)
                                .Size(100000)
                            );      
            return result.Documents.ToList();
        }

        public static async Task<Bucket<HistogramItem>> GetDateHistogram(DateTime timeStart, DateTime timeEnd, Guid mid)
        {
            var midContainer = Query<IndexUser>.Term(b => b.mid, mid.ToString());
            double start = CommonHelper.ToUnixTime(timeStart);
            double end = CommonHelper.ToUnixTime(timeEnd);
            var timeContainer = Query<IndexUser>.Range(r => r.OnField(b => b.register_time).GreaterOrEquals(start).LowerOrEquals(end));
            var result = await _client.SearchAsync<IndexUser>(s => s
                                .Index(_config.IndexName)
                                .Query(midContainer && timeContainer)
                                .Aggregations(a => a
                                    .DateHistogram("my_date_histogram", h => h
                                        .Field(p => p.register_time)
                                        .Interval("day")
                                        .Format("yyyy-MM-dd")
                                        .MinimumDocumentCount(0)
                                    )
                                )
                            );

            var agg = result.Aggs.DateHistogram("my_date_histogram");
            return agg;
        }
    }
}
