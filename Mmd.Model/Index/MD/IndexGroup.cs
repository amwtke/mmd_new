using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "group")]
    public class IndexGroup
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }//团的uuid

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }//商铺号

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "aaid", Type = FieldType.String)]
        public string aaid { get; set; }//团购的宣传图文

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pid", Type = FieldType.String)]
        public string pid { get; set; }//商品编号

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "group_headpic_dir", Type = FieldType.String)]
        public string group_headpic_dir { get; set; }//团购活动的主题图片

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "title", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string title { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "description", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik_max_word", SearchAnalyzer = "ik_smart")]
        public string description { get; set; }

        /// <summary>
        /// 商品剩余库存
        /// </summary>
        [ElasticProperty(Name = "product_quota", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int product_quota { get; set; }

        [ElasticProperty(Name = "product_setting_count", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int product_setting_count { get; set; }//商品设定总额

        /// <summary>
        /// 参团人数(几人团)
        /// </summary>
        [ElasticProperty(Name = "person_quota", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int person_quota { get; set; }


        [ElasticProperty(Name = "time_limit", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double time_limit { get; set; }//

        [ElasticProperty(Name = "waytoget", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int waytoget { get; set; }//取货方式

        [ElasticProperty(Name = "ltid", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string ltid { get; set; } //运费模板

        [ElasticProperty(Name = "isShowPTing", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int? isshowpting { get; set; }

        [ElasticProperty(Name = "origin_price", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int origin_price { get; set; }

        [ElasticProperty(Name = "group_price", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int group_price { get; set; }

        [ElasticProperty(Name = "status", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int status { get; set; }//商家发布团的状态

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "biz_type", Type = FieldType.String)]
        public string biz_type { get; set; }//业务类型，跟商铺的权限相关,关联m_biz表

        [ElasticProperty(Name = "last_update_time", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double last_update_time { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "last_update_user", Type = FieldType.String)]
        public string last_update_user { get; set; }

        [ElasticProperty(Name = "advertise_pic_url", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.String)]
        public string advertise_pic_url { get; set; }

        [ElasticProperty(Name = "group_start_time", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double group_start_time { get; set; }

        [ElasticProperty(Name = "group_end_time", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Double)]
        public double group_end_time { get; set; }

        [ElasticProperty(Name = "p_no", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Long)]
        public long p_no { get; set; }

        [ElasticProperty(Name = "group_type", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int group_type { get; set; }

        [ElasticProperty(Name = "commission", Index = FieldIndexOption.NotAnalyzed, Type = FieldType.Integer)]
        public int commission { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }

    }
}
