using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB.Code;

namespace MD.Model.DB
{
    [Serializable]
    [Table("Merchant")]
    public class Merchant//商家
    {
        [Key]
        public Guid mid { get; set; }//商家的uuid
        [Required(ErrorMessage = "联系人必填！")]
        public string contact_person { get; set; }//联系人
        [Required(ErrorMessage = "联系人地址必填！")]
        public string address { get; set; }//地址
        public string tel { get; set; }//电话

        [Required(ErrorMessage = "联系人手机号必填！")]
        [RegularExpression(@"^((13[0-9])|(15[0-9])|(18[0-9])|(17[0-9]))\d{8}$", ErrorMessage = "不是正确的手机号码！")]
        public long? cell_phone { get; set; }//手机
        /// <summary>
        /// 微信商铺ID
        /// </summary>
        [Required(ErrorMessage ="必填！")]
        public string wx_mch_id { get; set; }
        [Required(ErrorMessage ="必填！")]
        public string wx_apikey { get; set; }//微信支付API密钥
        [Required(ErrorMessage ="未上传p12证书！")]
        public string wx_p12_dir { get; set; }//微信支付p12证书服务器文件夹路径,用于退款
        public string wx_appid { get; set; }//微信公众号的appid
        public string wx_pay_dir { get; set; }//微信支付授权目录
        public string wx_jspay_dir { get; set; }//微信jssdk授权目录
        public string wx_biz_dir { get; set; }//接收微信消息的业务url,微信的主页
        public string wx_mp_id { get; set; }//商家公众号的原始id(如gh_eb5e3a772040)，在获取第三方授权后可以用
        public double? register_date { get; set; }//注册日期
        public string name { get; set; }//网站名称,显示在网站顶部,默认为公众号的名称

        [StringLength(64, ErrorMessage = "超过字符长度限制64个字")]
        public string title { get; set; }//分享朋友圈标题

        [StringLength(64, ErrorMessage = "超过字符长度限制64个字")]
        public string slogen { get; set; }//网站公告

        public string logo_url { get; set; }//logo的图片地址
        public string qr_url { get; set; }//公众号二维码位置
        [StringLength(400,ErrorMessage ="超过字符长度限制400个字")]
        public string brief_introduction { get; set; }//商家简介
        [StringLength(600,ErrorMessage = "超过字符长度限制600个字")]
        public string service_intro { get; set; }//售后服务介绍
        [Required(ErrorMessage = "经营范围必填！")]
        public string service_region { get; set; }//商家服务覆盖地区
        public int? order_quota { get; set; }//商家剩余订单
        public int? default_post_company { get; set; }//默认的快递公司编号
        //[ForeignKey("status")]
        //public virtual CodeMerchantStatus statusid { get; set; }//商户审核状态
        public int? status { get; set; }
        //[Display(Name="营业执照照片")]
        [Required(ErrorMessage="没有上传营业执照的图片！")]
        public string biz_licence_url { get; set; }//营业执照的上传图片的url

        public string advertise_pic_url { get; set; }//商家宣传画

        public string extension_1 { get; set; }

        public string extension_2 { get; set; }

        public string extension_3 { get; set; }
    }
}
