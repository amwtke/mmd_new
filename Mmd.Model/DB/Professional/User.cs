using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB
{
    [Serializable]
    /*用户信息表。
        用户通过公众号授权进来就有openid。
        openid默认应该是全局唯一的，但是为了系统的完整性，
        openid没有做主键，也不会在关联的时候使用。*/
    [Table("User")]
    public class User
    {
        [Key]
        public Guid uid { get; set; }
        public string openid { get; set; }//可能是主键
        public string wx_appid { get; set; }
        public Guid mid { get; set; }//每个商铺openid可能不同
        public string name { get; set; }
        public int? sex { get; set; }//可能来自微信
        public long? cell_phone { get; set; }
        public string address { get; set; }
        public string email { get; set; }
        public int? b_year { get; set; }//出生年月日
        public int? b_month { get; set; }
        public int? b_day { get; set; }
        public string mmd_account { get; set; }//美美哒自己的账号
        public string mmd_password { get; set; }//md5加密密码
        public string mmd_salt { get; set; }//密码盐
        public string membership_card { get; set; }//会员卡号
        public string wcard { get; set; }//微卡号

        public int? age { get; set; }
        public int? skin { get; set; }
        public string backimg { get; set; }

        public double? register_time { get; set; }
    }
}
