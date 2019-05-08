using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.Util;
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
   public class EsProductCommentPraiseManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsProductGrassManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsProductCommentPraiseConfig _config = null;
        static EsProductCommentPraiseManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsProductCommentPraiseConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsProductCommentPraiseConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexProductCommentPraise>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexProductCommentPraise>(client, _config.IndexName, new IndexSettings()
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

        public static IndexProductCommentPraise GenObject(Guid pcid, Guid uid)
        {
            if (!pcid.Equals(Guid.Empty) || !uid.Equals(Guid.Empty))
            {
                IndexProductCommentPraise pcp = new IndexProductCommentPraise()
                {
                    Id = Guid.NewGuid().ToString(),
                    pcid = pcid.ToString(),
                    uid = uid.ToString(),
                    timestamp = CommonHelper.GetUnixTimeNow()
                };
                return pcp;
            }
            return null;
        }
        public static async Task<bool> AddOrUpdateAsync(IndexProductCommentPraise obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexProductCommentPraise>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexProductCommentPraise l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexProductCommentPraise>((u) =>
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

        public static async Task<IndexProductCommentPraise> GetByPcidAndUidAsync(Guid pcid, Guid uid)
        {
            try
            {
                var pidContainer = Query<IndexProductCommentPraise>.Term("pcid", pcid);
                var uidContainer = Query<IndexProductCommentPraise>.Term("uid", uid);
                pidContainer = pidContainer && uidContainer;
                var result = await _client.SearchAsync<IndexProductCommentPraise>(s => s.Index(_config.IndexName).Query(pidContainer));
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
