using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.Pay;
using MD.Model.MQ;
using MD.Wechat.Controllers.PinTuanController.Pay;
using Microsoft.Ajax.Utilities;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.TenPayLibV3;

namespace MD.Wechat.Controllers.PinTuanController.Group
{
    public enum ETuan
    {
        /// <summary>
        /// 开团
        /// </summary>
        KT = 0,

        /// <summary>
        /// 参团
        /// </summary>
        CT = 1,
    }

    /// <summary>
    /// 微信支付的目录.http://wx3de24bcac3bb95cb.wx.mmpintuan.com/wxpay/
    /// </summary>
    public class wxpayController : Controller
    {
        // 正式支付目录
        public async Task<ActionResult> Index(string code,string state,string appid,string bizid)
        {

            //MDLogger.LogInfoAsync(typeof(wxpayController),$"设置cookie:{id}");
            if (string.IsNullOrEmpty(code))
            {
                return Content("您拒绝了授权！");
            }

            if (string.IsNullOrEmpty(appid))
            {
                return Content("appids是空！");
            }

            var userinfo = await WXComponentUserHelper.GetUserInfoAsync(appid, code);
            if (string.IsNullOrEmpty(userinfo?.Openid))
            {
                return Content("错误发生，openid没取到！");
            }

            Response.Cookies["bizid"].Value = bizid;
            Response.Cookies["bizid"].Expires = DateTime.Now.AddYears(1);

            Response.Cookies["appId"].Value = appid;
            Response.Cookies["appId"].Expires = DateTime.Now.AddYears(1);

            Response.Cookies["openid"].Value = userinfo.Openid;
            Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

            if (string.IsNullOrEmpty(state))
                return Content($"state is null or empty:state:{state}");

            return Redirect(state.Equals(ETuan.KT.ToString()) ? @"~/f2e/wxpay/pay.html" : @"~/f2e/wxpay/ctpay.html");

            //string paysign, timestamp, nonce;

            //using (var repo = new BizRepository())
            //{
            //    try
            //    {
            //        var mer = await repo.GetMerchantByAppidAsync(appid);
            //        if(mer==null || string.IsNullOrEmpty(mer.wx_appid) || string.IsNullOrEmpty(mer.wx_mch_id) || string.IsNullOrEmpty(mer.wx_apikey))
            //            throw new MDException(typeof(wxpayController),$"预支付失败！appid:{appid}");

            //        var preCode = WXPayHelper.GZHPay_GenPrePayCode(userinfo.Openid, appid, mer.wx_mch_id, "测试商品", 1,
            //        mer.wx_apikey, Request.UserHostAddress, out paysign, out timestamp, out nonce);

            //        if (string.IsNullOrEmpty(preCode))
            //            throw new MDException(typeof(wxpayController),$"预支付函数调用失败！appid:{appid}");

            //        ViewData["appId"] = appid;
            //        ViewData["timeStamp"] = timestamp;
            //        ViewData["nonceStr"] = nonce;
            //        ViewData["package"] = string.Format("prepay_id={0}", preCode);
            //        ViewData["paySign"] = paysign;
            //return View();
            //Response.Cookies["appId"].Value = appid;
            //Response.Cookies["appId"].Expires = DateTime.Now.AddMinutes(10);

            //Response.Cookies["appId"].Value = appid;
            //Response.Cookies["appId"].Expires = DateTime.Now.AddMinutes(10);

            //Response.Cookies["appId"].Value = appid;
            //Response.Cookies["appId"].Expires = DateTime.Now.AddMinutes(10);

            //Response.Cookies["appId"].Value = appid;
            //Response.Cookies["appId"].Expires = DateTime.Now.AddMinutes(10);
            //}
            //catch (Exception ex)
            //{
            //    throw new MDException(typeof(wxpayController),ex);
            //}
        }
        }

        /// <summary>
        /// http://wx3de24bcac3bb95cb.wx.mmpintuan.com/wxpay/test/
        /// </summary>
        /// <returns></returns>
    //    public async Task<ActionResult> test(string code, string state,string appid)
    //    {
    //        if (string.IsNullOrEmpty(code))
    //        {
    //            return Content("您拒绝了授权！");
    //        }

    //        if (string.IsNullOrEmpty(appid))
    //        {
    //            return Content("appids是空！");
    //        }

    //        var userinfo = await WXComponentUserHelper.GetUserInfoAsync(appid, code);
    //        if(userinfo==null || string.IsNullOrEmpty(userinfo.Openid)) 
    //        {
    //            return Content("错误发生，openid没取到！");
    //        }

    //        MDLogger.LogBizAsync(typeof(wxpayController),
    //new BizMQ("预支付-获取openid", userinfo.Openid, Guid.NewGuid(), $"获取openid为:{userinfo.Openid}"));

    //        string timeStamp = "";
    //        string nonceStr = "";

    //        string out_trade_no = GuidHelper.GuidTo16String() + DateTime.Now.ToString("yyyyMMddhhmmss"); 
    //        //创建支付应答对象
    //        RequestHandler packageReqHandler = new RequestHandler(null);
    //        //初始化
    //        packageReqHandler.Init();

    //        timeStamp = TenPayV3Util.GetTimestamp();
    //        nonceStr = TenPayV3Util.GetNoncestr();

    //        //设置package订单参数
    //        packageReqHandler.SetParameter("appid", appid);		  //公众账号ID
    //        packageReqHandler.SetParameter("mch_id", TestPayConfig.Mid);		  //商户号
    //        packageReqHandler.SetParameter("nonce_str", nonceStr);                    //随机字符串
    //        packageReqHandler.SetParameter("body","测试商品");    //商品信息
    //        packageReqHandler.SetParameter("out_trade_no", out_trade_no);		//商家订单号
    //        packageReqHandler.SetParameter("total_fee", "1");			        //商品金额,以分为单位(money * 100).ToString()
    //        packageReqHandler.SetParameter("spbill_create_ip", Request.UserHostAddress);   //用户的公网ip，不是商户服务器IP
    //        packageReqHandler.SetParameter("notify_url", "http://paycallback.mmpintuan.com");	    //接收财付通通知的URL
    //        packageReqHandler.SetParameter("trade_type", TenPayV3Type.JSAPI.ToString());	                    //交易类型
    //        packageReqHandler.SetParameter("openid", userinfo.Openid);	                    //用户的openId

    //        string sign = packageReqHandler.CreateMd5Sign("key", TestPayConfig.Key);
    //        packageReqHandler.SetParameter("sign", sign);	                    //签名

    //        string data = packageReqHandler.ParseXML();

    //        var result = TenPayV3.Unifiedorder(data);
    //        var res = XDocument.Parse(result);
    //        if (res.Element("xml").Element("prepay_id") == null)
    //        {
    //            throw new MDException(typeof(wxpayController),$"获取与支付码失败！返回的xml如下：{res.ToString()}");
    //        }
    //        string prepayId = res.Element("xml").Element("prepay_id").Value;

    //        MDLogger.LogBizAsync(typeof (wxpayController),
    //            new BizMQ("预支付", userinfo.Openid, Guid.NewGuid(), $"获取预支付码为:{prepayId}"));

    //        //设置支付参数
    //        string paySign = ""; 
    //        RequestHandler paySignReqHandler = new RequestHandler(null);
    //        paySignReqHandler.SetParameter("appId", appid);
    //        paySignReqHandler.SetParameter("timeStamp", timeStamp);
    //        paySignReqHandler.SetParameter("nonceStr", nonceStr);
    //        paySignReqHandler.SetParameter("package", string.Format("prepay_id={0}", prepayId));
    //        paySignReqHandler.SetParameter("signType", "MD5");
    //        paySign = paySignReqHandler.CreateMd5Sign("key", TestPayConfig.Key);

    //        ViewData["appId"] = appid;
    //        ViewData["timeStamp"] = timeStamp;
    //        ViewData["nonceStr"] = nonceStr;
    //        ViewData["package"] = string.Format("prepay_id={0}", prepayId);
    //        ViewData["paySign"] = paySign;

    //        return View();
    //    }
    //}
}