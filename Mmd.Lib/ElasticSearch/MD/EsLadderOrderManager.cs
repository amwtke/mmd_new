using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB.Activity;
using MD.Model.Index.MD;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch.MD
{
    public static class EsLadderOrderManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsLadderOrderManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsLadderOrderConfig _config = null;
        static EsLadderOrderManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsLadderOrderConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsLadderOrderConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexLadderOrder>(_client, _config.IndexName, new IndexSettings()
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
        public static async Task<bool> AddOrUpdateAsync(IndexLadderOrder obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexLadderOrder>((u) =>
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
        public static IndexLadderOrder GenObject(LadderOrder ladderorder)
        {
            if (ladderorder == null)
                return null;
            IndexLadderOrder indexladderorder = new IndexLadderOrder()
            {
                Id = ladderorder.oid.ToString(),
                o_no = ladderorder.o_no,
                mid = ladderorder.mid.ToString(),
                goid = ladderorder.goid.ToString(),
                gid = ladderorder.gid.ToString(),
                waytoget = ladderorder.waytoget,
                paytime = ladderorder.paytime,
                order_price = ladderorder.order_price,
                status = ladderorder.status,
                buyer = ladderorder.buyer.ToString(),
                default_writeoff_point = ladderorder.default_writeoff_point.ToString(),
                writeoff_point = ladderorder.writeoff_point.ToString(),
                writeoffer = ladderorder.writeoffer.ToString(),
                writeoffday = ladderorder.writeoffday,
                KeyWords = ""
            };
            return indexladderorder;
        }
        public static async Task<int> GetOrderCountByGidAsync(Guid gid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderOrder>(s => s.Query(q => q.Term(t => t.OnField("gid").Value(gid.ToString()))).Take(0));
                return (int)result.Total;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return 0;
        }
        public static async Task<int> GetOrderCountByGoidAsync(Guid goid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderOrder>(s => s.Query(q => q.Term(t => t.OnField("goid").Value(goid))).Take(0));
                return (int)result.Total;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return 0;
        }
        /// <summary>
        /// 是否开或参团记录
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="buyer"></param>
        /// <returns></returns>
        public static async Task<IndexLadderOrder> GetOrderByBuyerAsync(Guid gid, Guid buyer)
        {
            try
            {
                var gidContainer = Query<IndexLadderOrder>.Term("gid", gid);
                var buyerContainer = Query<IndexLadderOrder>.Term("buyer", buyer);
                gidContainer = gidContainer && buyerContainer;
                var result = await _client.SearchAsync<IndexLadderOrder>(s => s.Index(_config.IndexName).Query(gidContainer));
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

        public static async Task<Tuple<int, List<IndexLadderOrder>>> GetByGoidAsnyc(Guid goid, List<int> status, int pageIndex, int pageSize)
        {
            try
            {
                int from = (pageIndex - 1) * pageSize;
                int size = pageSize;

                var goidContainer = Query<IndexLadderOrder>.Term("goid", goid);
                QueryContainer statusContainer = null;
                foreach (var s in status)
                {
                    var stContainer = Query<IndexGroup>.Term("status", s);
                    statusContainer = statusContainer || stContainer;
                }
                goidContainer = goidContainer && statusContainer;

                var result = await _client.SearchAsync<IndexLadderOrder>(s => s.Index(_config.IndexName).Query(goidContainer).SortAscending("paytime").Skip(from).Take(size));
                if (result == null)
                    return null;
                int totalCount = (int)result.Total;
                var list = result.Documents.ToList();
                return Tuple.Create(totalCount, list);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Tuple.Create(0, new List<IndexLadderOrder>());
        }

        public static async Task<IndexLadderOrder> GetByIdAsync(Guid oid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLadderOrder>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(oid))));
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
    }
}
