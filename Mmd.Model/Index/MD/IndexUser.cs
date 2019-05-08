using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "user")]
    public class IndexUser
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "openid", Type = FieldType.String)]
        public string openid { get; set; }//可能是主键

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "wx_appid", Type = FieldType.String)]
        public string wx_appid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }//每个商铺openid可能不同

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "name", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "sex", Type = FieldType.Integer)]
        public int? sex { get; set; }//可能来自微信

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "cell_phone", Type = FieldType.Long)]
        public long? cell_phone { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "address", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string address { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "email", Type = FieldType.String)]
        public string email { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "b_year", Type = FieldType.Integer)]
        public int? b_year { get; set; }//出生年月日

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "b_month", Type = FieldType.Integer)]
        public int? b_month { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "b_day", Type = FieldType.Integer)]
        public int? b_day { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mmd_account", Type = FieldType.String)]
        public string mmd_account { get; set; }//美美哒自己的账号

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mmd_password", Type = FieldType.String)]
        public string mmd_password { get; set; }//md5加密密码

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mmd_salt", Type = FieldType.String)]
        public string mmd_salt { get; set; }//密码盐

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "membership_card", Type = FieldType.String)]
        public string membership_card { get; set; }//会员卡号

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "wcard", Type = FieldType.String)]
        public string wcard { get; set; }//微卡号

        /// <summary>
        /// 出生日期时间戳
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "age", Type = FieldType.Integer)]
        public int age { get; set; }

        /// <summary>
        /// 肤质代码
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "skin", Type = FieldType.Integer)]
        public int skin { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "backimg", Type = FieldType.String)]
        public string backimg { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "register_time", Type = FieldType.Double)]
        public double? register_time { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
