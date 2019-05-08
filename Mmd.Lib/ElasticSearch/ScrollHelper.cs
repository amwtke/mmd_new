using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using MD.Configuration;
using MD.Model.Configuration.ElasticSearch;
using Nest;

namespace MD.Lib.ElasticSearch
{
    public static class ScrollHelper
    {
        public static async Task<int> ProcessScrollAsync<ConfigObject, IndexObejct>(string indexName,
            Action<IndexObejct> process, int stepLength = 1000,string scrollTime="30s")
            where ConfigObject : IESIndexInterface, new()
            where IndexObejct : class, new()
        {
            ElasticClient client = ESHeper.GetClient<ConfigObject>();
            var config = MdConfigurationManager.GetConfig<ConfigObject>();

            int total = 0;

            if (client != null && config != null && process!=null)
            {
                var scanResults = await client.SearchAsync<IndexObejct>(s => s
                    .Index(indexName)
                    .From(0)
                    .Size(stepLength)
                    .MatchAll()
                    .SearchType(SearchType.Scan)
                    .Scroll(scrollTime)
                    );
                if (scanResults != null)
                {
                    var results = await client.ScrollAsync<IndexObejct>(scrollTime, scanResults.ScrollId);
                    while (results.Documents.Any())
                    {
                        foreach (IndexObejct o in results.Documents)
                        {
                            process(o);
                            total++;
                        }
                        results = await client.ScrollAsync<IndexObejct>(scrollTime, results.ScrollId);
                    }
                }
            }
            return total;
        }
    }
}
