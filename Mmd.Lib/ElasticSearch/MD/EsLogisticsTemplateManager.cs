using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Model.Configuration.ElasticSearch.MD;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch.MD
{
    public class EsLogisticsTemplateManager
    {
        static readonly object LockObject = new object();

        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(EsLogisticsTemplateManager), ex);
        }
        static readonly ElasticClient _client = null;
        static readonly EsLogisticsTemplateConfig _config = null;
        static EsLogisticsTemplateManager()
        {
            if (_client != null && _config != null) return;

            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<EsLogisticsTemplateConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<EsLogisticsTemplateConfig>();

                init();
            }
        }

        static void init()
        {
            if (!ESHeper.BeSureMapping<IndexLogisticsTemplate>(_client, _config.IndexName, new IndexSettings()
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
            if (!ESHeper.BeSureMapping<IndexLogisticsTemplate>(client, _config.IndexName, new IndexSettings()
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

        public static IndexLogisticsTemplate GenObject(Logistics_Template lt)
        {
            if (lt != null)
            {
                IndexLogisticsTemplate indexlr = new IndexLogisticsTemplate()
                {
                    Id = lt.ltid.ToString(),
                    name = lt.name,
                    mid = lt.mid.ToString(),
                    items = GenItems(lt.items)
                };
                return indexlr;
            }
            return null;
        }

        public static List<LogisticsTemplateItem> GenItems(List<Logistics_TemplateItem> list)
        {
            if (list != null)
            {
                List<LogisticsTemplateItem> res = new List<LogisticsTemplateItem>();
                foreach (var item in list)
                {
                    LogisticsTemplateItem obj = new LogisticsTemplateItem();
                    obj.id = item.id.ToString();
                    obj.first_fee = item.first_fee;
                    obj.first_amount = item.first_amount;
                    obj.additional_fee = item.additional_fee;
                    obj.additional_amount = item.additional_amount;
                    obj.regions = item.regions.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    res.Add(obj);
                }
                return res;
            }
            return new List<LogisticsTemplateItem>();
        }

        public static async Task<bool> AddOrUpdateAsync(IndexLogisticsTemplate obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLogisticsTemplate>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));

                IndexLogisticsTemplate l = obj;
                //更新
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.UpdateAsync<IndexLogisticsTemplate>((u) =>
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

        public static async Task<bool> DeleteAsync(IndexLogisticsTemplate obj)
        {
            try
            {
                var result = await _client.SearchAsync<IndexLogisticsTemplate>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(obj.Id))));
                if (result.Total >= 1)
                {
                    string _id = result.Hits.First().Id;
                    var r = await _client.DeleteAsync<IndexLogisticsTemplate>((u) =>
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
        /// 根据模板ID和区域获取邮费
        /// </summary>
        /// <param name="ltid">模板ID</param>
        /// <param name="code">区域编码</param>
        /// <returns>返回运费，单位：分;
        /// 值为-100表示不在配送区域内；
        /// -200模板不存在，-500为Error
        /// </returns>
        public static async Task<int> GetFeeByCode(Guid ltid,string code)
        {
            try
            {
                if (code.Length == 9)
                {
                    var result = await _client.SearchAsync<IndexLogisticsTemplate>(s => s.Query(q => q.Term(t => t.OnField("Id").Value(ltid.ToString()))));
                    if (result.Total >= 1)
                    {
                        string province = code.Substring(0, 3);
                        string city = code.Substring(0, 6);
                        IndexLogisticsTemplate temp = result.Documents.FirstOrDefault();
                        foreach (var item in temp.items)
                        {
                            List<string> regions = item.regions;
                            if (regions.Contains(province))
                                return item.first_fee;
                            if (regions.Contains(city))
                                return item.first_fee;
                            if (regions.Contains(code))
                                return item.first_fee;
                        }
                        return -100; //不在配送区域
                    }
                    return -200; //模板不存在
                }
                return -300;  //区域编码错误
            }
            catch (Exception ex)
            {
                LogError(ex);
                return -500;   
            }
        }

        private class RegionComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (x == null)
                    return y == null;
                return x == y;
            }

            public int GetHashCode(string obj)
            {
                if (obj == null)
                    return 0;
                return obj.GetHashCode();
            }
        }
    }
}
