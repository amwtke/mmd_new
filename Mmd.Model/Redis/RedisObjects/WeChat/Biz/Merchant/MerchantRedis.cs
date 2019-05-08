using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.RedisObjects.WeChat.Biz.Merchant
{
    [RedisHash("Biz.Merchant.hash")]
    [RedisDBNumber("1")]
    public class MerchantRedis
    {
        [RedisKey]
        [RedisHashEntry("mid")]
        public string mid { get; set; } //商家的uuid

        [RedisHashEntry("contact_person")]
        public string contact_person { get; set; } //联系人

        [RedisHashEntry("address")]
        public string address { get; set; } //地址

        [RedisHashEntry("tel")]
        public string tel { get; set; } //电话

        [RedisHashEntry("cell_phone")]
        public string cell_phone { get; set; } //手机

        [RedisHashEntry("wx_mch_id")]
        public string wx_mch_id { get; set; }

        [RedisHashEntry("wx_apikey")]
        public string wx_apikey { get; set; } //微信支付API密钥

        [RedisHashEntry("wx_p12_dir")]
        public string wx_p12_dir { get; set; } //微信支付p12证书服务器文件夹路径,用于退款

        [RedisHashEntry("wx_appid")]
        public string wx_appid { get; set; } //微信公众号的appid

        [RedisHashEntry("wx_pay_dir")]
        public string wx_pay_dir { get; set; } //微信支付授权目录

        [RedisHashEntry("wx_jspay_dir")]
        public string wx_jspay_dir { get; set; } //微信jssdk授权目录

        [RedisHashEntry("wx_biz_dir")]
        public string wx_biz_dir { get; set; } //接收微信消息的业务url,微信的主页

        [RedisHashEntry("wx_mp_id")]
        public string wx_mp_id { get; set; } //商家公众号的原始id(如gh_eb5e3a772040)，在获取第三方授权后可以用

        [RedisHashEntry("register_date")]
        public string register_date { get; set; } //注册日期

        [RedisHashEntry("name")]
        public string name { get; set; } //网站名称,显示在网站顶部,默认为公众号的名称

        [RedisHashEntry("title")]
        public string title { get; set; } //分享朋友圈标题

        [RedisHashEntry("slogen")]
        public string slogen { get; set; } //网站公告

        [RedisHashEntry("logo_url")]
        public string logo_url { get; set; } //logo的图片地址

        [RedisHashEntry("qr_url")]
        public string qr_url { get; set; } //公众号二维码位置

        [RedisHashEntry("brief_introduction")]
        public string brief_introduction { get; set; } //商家简介

        [RedisHashEntry("service_intro")]
        public string service_intro { get; set; } //售后服务介绍

        [RedisHashEntry("service_region")]
        public string service_region { get; set; } //商家服务覆盖地区

        [RedisHashEntry("order_quota")]
        public string order_quota { get; set; } //商家剩余订单

        [RedisHashEntry("default_post_company")]
        public string default_post_company { get; set; } //默认的快递公司编号

        [RedisHashEntry("status")]
        public string status { get; set; }

        [RedisHashEntry("biz_licence_url")]
        public string biz_licence_url { get; set; } //营业执照的上传图片的url

        [RedisHashEntry("advertise_pic_url")]
        public string advertise_pic_url { get; set; } //商家宣传画

        [RedisHashEntry("extension_1")]
        public string extension_1 { get; set; }

        [RedisHashEntry("extension_2")]
        public string extension_2 { get; set; }

        [RedisHashEntry("extension_3")]
        public string extension_3 { get; set; }
    }

    [RedisHash("Biz.Merchant.AppidMap.hash")]
    [RedisDBNumber("1")]
    public class MerchantAppidMapRedis
    {
        [RedisKey]
        [RedisHashEntry("appid")]
        public string appid { get; set; }

        [RedisHashEntry("mid")]
        public string mid { get; set; }
    }
}
