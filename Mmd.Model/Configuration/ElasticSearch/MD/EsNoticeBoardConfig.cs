using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;
namespace MD.Model.Configuration.ElasticSearch.MD
{
    [MDConfig("ElasticSearch", "NoticeBoard")]
    public class EsNoticeBoardConfig : IESIndexInterface
    {
        [MDKey("IndexName")]
        public string IndexName { get; set; }

        [MDKey("NumberOfShards")]
        public string NumberOfShards { get; set; }

        [MDKey("NumberOfReplica")]
        public string NumberOfReplica { get; set; }
    }
}
