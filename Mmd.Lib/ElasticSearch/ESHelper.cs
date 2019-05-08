using MD.Lib.Log;
using MD.Configuration;
using MD.Model.Configuration.ElasticSearch;
using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch
{
    public static class ESHeper
    {
        static object _syncObject = new object();
        static Dictionary<Type, ElasticClient> _dic = new Dictionary<Type, ElasticClient>();
        static ConcurrentDictionary<string,ElasticClient> _dic2 = new ConcurrentDictionary<string, ElasticClient>();
        public static ElasticClient GetClient<T>() where T : IESIndexInterface, new()
        {
            Type t = typeof(T);
            ElasticClient _client=null;
            if (_dic.TryGetValue(t, out _client))
                return _client;

            if (_client == null)
            {
                lock (_syncObject)
                {
                    if (_client == null)
                    {
                        try
                        {
                            string Address = MdConfigurationManager.GetConfig<T>().RemoteAddress;
                            string Port = MdConfigurationManager.GetConfig<T>().RemotePort;


                            var node = new Uri(@"http://" + Address + ":" + Port);

                            var settings = new ConnectionSettings(
                                node
                                );

                            _client = new ElasticClient(settings);
                            _dic[t] = _client;
                            return _client;
                        }
                        catch (Exception ex)
                        {
                            MDLogger.LogErrorAsync(typeof(ESHeper), ex);
                            throw ex;
                        }
                    }
                }
            }
            return _client;
        }

        public static ElasticClient GetClient(string host,string port)
        {
            ElasticClient _client = null;

            if (_dic2.TryGetValue(host, out _client))
                return _client;

            if (_client == null)
            {
                lock (_syncObject)
                {
                    if (_client == null)
                    {
                        try
                        {
                            string Address = host;
                            string Port = port;


                            var node = new Uri(@"http://" + Address + ":" + Port);

                            var settings = new ConnectionSettings(
                                node
                                );

                            _client = new ElasticClient(settings);
                            _dic2[host] = _client;
                            return _client;
                        }
                        catch (Exception ex)
                        {
                            MDLogger.LogErrorAsync(typeof(ESHeper), ex);
                            throw ex;
                        }
                    }
                }
            }
            return _client;
        }

        public static bool BeSureMapping<T>(ElasticClient client,string indexName, IndexSettings bizSettings) where T : class
        {
            IGetMappingResponse mapping = client.GetMapping<T>();
            if (mapping != null && (mapping.Mappings == null || mapping.Mappings.Count != 0)) return true;
            var response = client.CreateIndex(
                c => c.Index(indexName).InitializeUsing(bizSettings).AddMapping<T>(m => m.MapFromAttributes()));
            if (response.Acknowledged != true)
            {
                var put = new PutMappingRequest<T>();
                var mr = client.Map<T>(p => p.Index(indexName).MapFromAttributes());
                return mr.Acknowledged;
            }            
            return response.Acknowledged;
        }

        public static async Task<string> AnalyzeQueryString(ElasticClient client, string indexName, string analyzerName, string QueryStr)
        {
            var analyzeRequest = new AnalyzeRequest(QueryStr); analyzeRequest.Analyzer = analyzerName; analyzeRequest.Text = QueryStr;
            analyzeRequest.IndexQueryString = indexName;
            var response = await client.AnalyzeAsync(analyzeRequest);
            if (response?.Tokens == null)
                return null;
            string keys = "";
            foreach (var v in response.Tokens)
            {
                keys += (v.Token + " ");
            }
            keys = keys.Trim();
            return keys;
        }

        public static string AnalyzeQueryString_TB(ElasticClient client, string indexName, string analyzerName, string QueryStr)
        {
            var analyzeRequest = new AnalyzeRequest(QueryStr); analyzeRequest.Analyzer = analyzerName; analyzeRequest.Text = QueryStr;
            analyzeRequest.IndexQueryString = indexName;
            var response = client.Analyze(analyzeRequest);
            if (response?.Tokens == null)
                return null;
            string keys = "";
            foreach (var v in response.Tokens)
            {
                keys += (v.Token + " ");
            }
            keys = keys.Trim();
            return keys;
        }
    }
}
