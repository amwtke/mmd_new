using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
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
    public static class EsAct_usersignManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsAct_usersignManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsAct_usersignConfig _config = null;
        static EsAct_usersignManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;
                _client = ESHeper.GetClient<EsAct_usersignConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }
                _config = MdConfigurationManager.GetConfig<EsAct_usersignConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexAct_usersign>(_client, _config.IndexName, new IndexSettings()
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
        public static async Task<IndexAct_usersign> GenObjectAsync(Guid usid)
        {
            using (var acit = new ActivityRepository())
            {
                var dbus = await acit.GetUserSignByIdAsync(usid);
                if (dbus == null || dbus.usid.Equals(Guid.Empty))
                    return null;
                IndexAct_usersign index_us = new IndexAct_usersign()
                {
                    Id = dbus.usid.ToString(),
                    uid = dbus.uid.ToString(),
                    openid = dbus.openid,
                    sid = dbus.sid.ToString(),
                    mid = dbus.mid.ToString(),
                    status = dbus.status,
                    signTime = dbus.signTime,
                    writeoffer = dbus.writeoffer.ToString(),
                    writeoffTime = dbus.writeoffTime
                };
                return index_us;
            }
        }

        public static async Task<bool> AddOrUpdateAsync(IndexAct_usersign obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_usersign>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                IndexAct_usersign l = obj;
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexAct_usersign>((u) =>
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
        public static async Task<IndexAct_usersign> GetBysidAsync(Guid sid, string openid)
        {
            try
            {

                QueryContainer container = null;
                var sidContainer = Query<IndexAct_usersign>.Term("sid", sid.ToString());
                container = container && sidContainer;
                var openidContainer = Query<IndexAct_usersign>.Term("openid", openid);
                container = container && openidContainer;


                var result = await _client.SearchAsync<IndexAct_usersign>(s => s.Index(_config.IndexName).Query(container)
                .Skip(0).Take(1));
                var list = result.Documents.ToList();
                if (list.Count > 0)
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
        public static async Task<IndexAct_usersign> GetByUsidAsync(Guid usid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_usersign>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(usid.ToString()))));
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

        public static async Task<long> GetCountBySidAsync(Guid sid)
        {
            try
            {
                var result = await _client.SearchAsync<IndexAct_usersign>(s => s.Query(q => q.Term(t => t.OnField("sid").Value(sid.ToString()))));
                return result.Total;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return 0;
        }
    }
}
