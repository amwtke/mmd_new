using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "blacklist")]
    public class IndexBlacklist
    {
        /// <summary>
        /// 主键
        /// </summary>
        [ElasticProperty(Name = "Id", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string Id { get; set; }

        /// <summary>
        /// 商家mid
        /// </summary>
        [ElasticProperty(Name = "mid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string mid { get; set; }

        /// <summary>
        /// 被禁止的用户
        /// </summary>
        [ElasticProperty(Name = "uid", Type = FieldType.String, Index = FieldIndexOption.NotAnalyzed)]
        public string uid { get; set; }

        /// <summary>
        /// 开放时间
        /// </summary>
        [ElasticProperty(Name = "opentimestamp", Type = FieldType.Double, Index = FieldIndexOption.NotAnalyzed)]
        public double opentimestamp { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [ElasticProperty(Name = "createtime", Type = FieldType.Double, Index = FieldIndexOption.NotAnalyzed)]
        public double createtime { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        [ElasticProperty(Name = "type", Type = FieldType.Integer, Index = FieldIndexOption.NotAnalyzed)]
        public int type { get; set; }
    }
    public enum EBlacklistType
    {
        美美社区发帖 = 1
    }
}
