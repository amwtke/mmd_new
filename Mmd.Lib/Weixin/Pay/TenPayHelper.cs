using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.Exceptions.Pay;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration.WeChat;
using MD.Model.MQ;
using MD.Model.Redis.Objects.Messaging;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.Helpers;
using Senparc.Weixin.MP.TenPayLibV3;
using OAuthApi = Senparc.Weixin.Open.OAuthAPIs.OAuthApi;
using OAuthScope = Senparc.Weixin.Open.OAuthScope;

namespace MD.Lib.Weixin.Pay
{
    public static class WXPayHelper
    {
        #region util
        /// <summary>
        /// 30位.时间yyyyMMddhhmmss加上16位随机码。
        /// 用于生成要提交到微信的唯一码。
        /// </summary>
        /// <returns></returns>
        public static string GenWXOrderId()
        {
            return DateTime.Now.ToString("yyyyMMddhhmmss") + GuidHelper.GuidTo16String();
        }

        /// <summary>
        /// 微信支付的时间戳。从1970年到现在时间的秒数。
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        public static string GetNonce()
        {
            Random random = new Random();
            return MD5UtilHelper.GetMD5(random.Next(1000).ToString(), "GBK");
        }

        /// <summary>
        /// 测试支付的url
        /// </summary>
        /// <param name="gzh_appid"></param>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static string GenPayTestUrl(string gzh_appid, string id, string state = "")
        {
            string redirectURL = @"http://" + gzh_appid + @".wx.mmpintuan.com/wxpay/test/" + id;
            OAuthScope[] array = new OAuthScope[1];
            array[0] = OAuthScope.snsapi_userinfo;
            //array[1] = OAuthScope.snsapi_base;
            string ret = OAuthApi.GetAuthorizeUrl(gzh_appid, WXComponentHelper.GetConfigObject().AppId, redirectURL, state, array);
            return ret;
        }

        /// <summary>
        /// 根据appid生成支付授权url。
        /// </summary>
        /// <param name="gzh_appid"></param>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static string GenPayUrl(string gzh_appid, string bizid, string state = "")
        {
            string redirectURL = @"http://" + gzh_appid + @".wx.mmpintuan.com/wxpay?bizid=" + bizid;
            OAuthScope[] array = new OAuthScope[1];
            array[0] = OAuthScope.snsapi_userinfo;
            //array[1] = OAuthScope.snsapi_base;
            string ret = OAuthApi.GetAuthorizeUrl(gzh_appid, WXComponentHelper.GetConfigObject().AppId, redirectURL, state, array);
            return ret;
        }
        #endregion

        /// <summary>
        /// 公众号获取预支付码函数.out的参数都是给h5拉起微信支付页面使用。 
        /// </summary>
        /// <param name="userOpenid">用户的openid</param>
        /// <param name="gzhAppid">公众号appid</param>
        /// <param name="mch_id">商铺号，跟公众号appid对应</param>
        /// <param name="product_des">商品的简单描述</param>
        /// <param name="total_fee">支付价格,单位分</param>
        /// <param name="payKey">商铺的支付key</param>
        /// <param name="userHostIpAddress">发起支付用户的ip地址</param>
        /// <param name="payCallBackUrl">支付回调url</param>
        /// <returns></returns>
        public static async Task<Tuple<string,string,string,string>> GZHPay_GenPrePayCode(string userOpenid, string gzhAppid, string mch_id, string product_des,
            int total_fee, string out_trade_no, string payKey, string userHostIpAddress)
        {
            WxPayConfig config = MdConfigurationManager.GetConfig<WxPayConfig>();
            if (string.IsNullOrEmpty(config?.NotifyCallbackUrl))
                throw new MDException(typeof(WXPayHelper), "GZHPay_GenPrePayCode没有取到配置！");

            if (string.IsNullOrEmpty(gzhAppid) || string.IsNullOrEmpty(userOpenid) || string.IsNullOrEmpty(mch_id) ||
                string.IsNullOrEmpty(product_des) || total_fee <= 0 || string.IsNullOrEmpty(payKey) ||
                string.IsNullOrEmpty(userHostIpAddress))
            {
                throw new MDException(typeof(WXPayHelper),
                    "参数错误！" +
                    $"gzhAppid:{gzhAppid};userOpenid:{userOpenid};mch_id:{mch_id};product_des:{product_des};total_fee:{total_fee.ToString()};payCallBackUrl:{config.NotifyCallbackUrl};payKey:{payKey},userHostIpAddress:{userHostIpAddress}");
            }

            string timeStamp = "";
            string nonceStr = "";

            WatchStopper ws = new WatchStopper(typeof(WXPayHelper),"预支付计时！");

            //创建支付应答对象
            //ws.Restart("1-预支付申请");
            RequestHandler packageReqHandler = new RequestHandler(null);
            //初始化
            packageReqHandler.Init();

            timeStamp = GetTimeStamp();
            nonceStr = GetNonce(); ;

            //设置package订单参数
            packageReqHandler.SetParameter("appid", gzhAppid); //公众账号ID
            packageReqHandler.SetParameter("mch_id", mch_id); //商户号
            packageReqHandler.SetParameter("nonce_str", nonceStr); //随机字符串
            packageReqHandler.SetParameter("body", product_des); //商品信息
            packageReqHandler.SetParameter("out_trade_no", out_trade_no); //商家订单号
            packageReqHandler.SetParameter("total_fee", total_fee.ToString()); //商品金额,以分为单位(money * 100).ToString()
            packageReqHandler.SetParameter("spbill_create_ip", userHostIpAddress); //用户的公网ip，不是商户服务器IP
            packageReqHandler.SetParameter("notify_url", config.NotifyCallbackUrl); //接收财付通通知的URL
            packageReqHandler.SetParameter("trade_type", TenPayV3Type.JSAPI.ToString()); //交易类型
            packageReqHandler.SetParameter("openid", userOpenid); //用户的openId

            //MDLogger.LogInfoAsync(typeof(WXPayHelper),$"paykey:{payKey}");

            string sign = packageReqHandler.CreateMd5Sign("key", payKey);
            packageReqHandler.SetParameter("sign", sign); //签名

            string data = packageReqHandler.ParseXML();
            //ws.Stop();

            ws.Restart("1.1-预支付发包");
            var result = TenPayV3.Unifiedorder(data);
            var res = XDocument.Parse(result);

            ws.Stop();

            //将结果存入数据库
            //ws.Restart("2-预支付入库！");
            await MdWxPayUtil.SavePrepayResultAsync(gzhAppid,mch_id, out_trade_no,total_fee,res);
            //ws.Stop();

            //判断结果
            
            if (res.Element("xml").Element("prepay_id") == null)
            {
                if (res.Element("xml").Element("err_code_des") != null)
                {
                    string error_des = res.Element("xml").Element("err_code_des").Value;
                    if (string.IsNullOrEmpty(error_des))
                        return null;

                    if (error_des.Contains("该订单已支付"))
                        throw new PrePayDuplicatedException();
                }
                else
                {
                    if (res.Element("xml").Element("return_code") != null &&
                        res.Element("xml").Element("return_code").ToString().Contains("FAIL"))
                    {
                        throw new Exception(res.Element("xml").Element("return_msg").ToString());
                    }
                }
            }

            string prepayId = res.Element("xml").Element("prepay_id").Value;
            //MDLogger.LogBizAsync(typeof(WXPayHelper),new BizMQ("预支付", userOpenid, Guid.NewGuid(), $"获取预支付码为:{prepayId}"));

            //设置支付参数
            RequestHandler paySignReqHandler = new RequestHandler(null);
            paySignReqHandler.SetParameter("appId", gzhAppid);
            paySignReqHandler.SetParameter("timeStamp", timeStamp);
            paySignReqHandler.SetParameter("nonceStr", nonceStr);
            paySignReqHandler.SetParameter("package", string.Format("prepay_id={0}", prepayId));
            paySignReqHandler.SetParameter("signType", "MD5");
            var paySign = paySignReqHandler.CreateMd5Sign("key", payKey);

            //MDLogger.LogBizAsync(typeof(WXPayHelper),new BizMQ("预支付", userOpenid, Guid.NewGuid(), $"获取paySign为:{paySign}"));
            return Tuple.Create(prepayId, paySign, timeStamp, nonceStr);
        }

        //public static Tuple<string, string, string, string> GZHPay_GenPrePayCode_BT(string userOpenid, string gzhAppid, string mch_id, string product_des,
        //    int total_fee, string out_trade_no, string payKey, string userHostIpAddress)
        //{
        //    WxPayConfig config = MdConfigurationManager.GetConfig<WxPayConfig>();
        //    if (string.IsNullOrEmpty(config?.NotifyCallbackUrl))
        //        throw new MDException(typeof(WXPayHelper), "GZHPay_GenPrePayCode没有取到配置！");

        //    if (string.IsNullOrEmpty(gzhAppid) || string.IsNullOrEmpty(userOpenid) || string.IsNullOrEmpty(mch_id) ||
        //        string.IsNullOrEmpty(product_des) || total_fee <= 0 || string.IsNullOrEmpty(payKey) ||
        //        string.IsNullOrEmpty(userHostIpAddress))
        //    {
        //        throw new MDException(typeof(WXPayHelper),
        //            "参数错误！" +
        //            $"gzhAppid:{gzhAppid};userOpenid:{userOpenid};mch_id:{mch_id};product_des:{product_des};total_fee:{total_fee.ToString()};payCallBackUrl:{config.NotifyCallbackUrl};payKey:{payKey},userHostIpAddress:{userHostIpAddress}");
        //    }

        //    string timeStamp = "";
        //    string nonceStr = "";

        //    WatchStopper ws = new WatchStopper(typeof(WXPayHelper), "预支付计时！");

        //    //创建支付应答对象
        //    //ws.Restart("1-预支付申请");
        //    RequestHandler packageReqHandler = new RequestHandler(null);
        //    //初始化
        //    packageReqHandler.Init();

        //    timeStamp = GetTimeStamp();
        //    nonceStr = GetNonce(); ;

        //    //设置package订单参数
        //    packageReqHandler.SetParameter("appid", gzhAppid); //公众账号ID
        //    packageReqHandler.SetParameter("mch_id", mch_id); //商户号
        //    packageReqHandler.SetParameter("nonce_str", nonceStr); //随机字符串
        //    packageReqHandler.SetParameter("body", product_des); //商品信息
        //    packageReqHandler.SetParameter("out_trade_no", out_trade_no); //商家订单号
        //    packageReqHandler.SetParameter("total_fee", total_fee.ToString()); //商品金额,以分为单位(money * 100).ToString()
        //    packageReqHandler.SetParameter("spbill_create_ip", userHostIpAddress); //用户的公网ip，不是商户服务器IP
        //    packageReqHandler.SetParameter("notify_url", config.NotifyCallbackUrl); //接收财付通通知的URL
        //    packageReqHandler.SetParameter("trade_type", TenPayV3Type.JSAPI.ToString()); //交易类型
        //    packageReqHandler.SetParameter("openid", userOpenid); //用户的openId

        //    string sign = packageReqHandler.CreateMd5Sign("key", payKey);
        //    packageReqHandler.SetParameter("sign", sign); //签名

        //    string data = packageReqHandler.ParseXML();
        //    //ws.Stop();

        //    ws.Restart("1.1-预支付发包");
        //    var result = TenPayV3.Unifiedorder(data);
        //    var res = XDocument.Parse(result);

        //    ws.Stop();

        //    //将结果存入数据库
        //    //ws.Restart("2-预支付入库！");
        //    MdWxPayUtil.SavePrepayResult(gzhAppid, mch_id, out_trade_no, total_fee, res);
        //    //ws.Stop();

        //    //判断结果

        //    if (res.Element("xml").Element("prepay_id") == null)
        //    {
        //        string error_des = res.Element("xml").Element("err_code_des").Value;
        //        if (string.IsNullOrEmpty(error_des))
        //            return null;

        //        if (error_des.Contains("该订单已支付"))
        //            throw new PrePayDuplicatedException();
        //    }

        //    string prepayId = res.Element("xml").Element("prepay_id").Value;
        //    //MDLogger.LogBizAsync(typeof(WXPayHelper),new BizMQ("预支付", userOpenid, Guid.NewGuid(), $"获取预支付码为:{prepayId}"));

        //    //设置支付参数
        //    RequestHandler paySignReqHandler = new RequestHandler(null);
        //    paySignReqHandler.SetParameter("appId", gzhAppid);
        //    paySignReqHandler.SetParameter("timeStamp", timeStamp);
        //    paySignReqHandler.SetParameter("nonceStr", nonceStr);
        //    paySignReqHandler.SetParameter("package", string.Format("prepay_id={0}", prepayId));
        //    paySignReqHandler.SetParameter("signType", "MD5");
        //    var paySign = paySignReqHandler.CreateMd5Sign("key", payKey);

        //    //MDLogger.LogBizAsync(typeof(WXPayHelper),new BizMQ("预支付", userOpenid, Guid.NewGuid(), $"获取paySign为:{paySign}"));
        //    return Tuple.Create(prepayId, paySign, timeStamp, nonceStr);
        //}

        #region 查询订单
        /// <summary>
        /// transactionid与outtradeno只要填一个。
        /// </summary>
        /// <param name="gzhAppid"></param>
        /// <param name="gzhMchid"></param>
        /// <param name="gzhPaykey"></param>
        /// <param name="transactionId"></param>
        /// <param name="outTradeNo"></param>
        /// <returns></returns>
        public static XDocument GZHPay_QueryOrder(string gzhAppid, string gzhMchid, string gzhPaykey,string transactionId,string outTradeNo)
        {
            string nonceStr = TenPayV3Util.GetNoncestr();
            RequestHandler packageReqHandler = new RequestHandler(null);

            //设置package订单参数
            packageReqHandler.SetParameter("appid", gzhAppid);		  //公众账号ID
            packageReqHandler.SetParameter("mch_id", gzhMchid);		  //商户号
            if(!string.IsNullOrEmpty(transactionId))
                packageReqHandler.SetParameter("transaction_id", transactionId);       //填入微信订单号 
            if(!string.IsNullOrEmpty(outTradeNo))
                packageReqHandler.SetParameter("out_trade_no", outTradeNo);         //填入商家订单号
            packageReqHandler.SetParameter("nonce_str", nonceStr);             //随机字符串
            string sign = packageReqHandler.CreateMd5Sign("key", gzhPaykey);
            packageReqHandler.SetParameter("sign", sign);	                    //签名

            string data = packageReqHandler.ParseXML();

            var result = TenPayV3.OrderQuery(data);
            var res = XDocument.Parse(result);
            return res;
        }

        public static async Task<XDocument> GZHPay_QueryOrderAsync(string gzhAppid, string gzhMchid, string gzhPaykey, string transactionId, string outTradeNo)
        {
            var result = await AsyncHelper.RunAsync(GZHPay_QueryOrder, gzhAppid, gzhMchid, gzhPaykey, transactionId, outTradeNo);
            return result;
        }


        public static void GZHPay_QueryOrder(string gzhAppid,string gzhMchid,string gzhPaykey, string transactionId, string outTradeNo,Action<XDocument> processResultAction )
        {
            var res = GZHPay_QueryOrder(gzhAppid, gzhMchid, gzhPaykey, transactionId,outTradeNo);
            processResultAction(res);
        }

        public static async void GZHPay_QueryOrderAsync(string gzhAppid, string gzhMchid, string gzhPaykey, string transactionId, string outTradeNo,Action<XDocument> processResultAction)
        {
            var res = await GZHPay_QueryOrderAsync(gzhAppid, gzhMchid, gzhPaykey, transactionId, outTradeNo);
            processResultAction(res);
        }

        //TODO 有了ef对象后写
        //public static async void GZHPay_QueryOrderAsync(string gzhAppid, Func<string,object> getMerchantObjectByAppid,string transactionId, string outTradeNo, Action<XDocument> processResultAction)
        //{
        //    object o = await AsyncHelper.RunAsync(getMerchantObjectByAppid, gzhAppid);
        //    var res = await GZHPay_QueryOrderAsync(gzhAppid, o.ToString(), o.ToString(), transactionId, outTradeNo);
        //    processResultAction(res);
        //}

        #endregion

        #region refund
            /// <summary>
            /// 退款的api
            /// </summary>
            /// <param name="gzhAppid"></param>
            /// <param name="mId">商铺号</param>
            /// <param name="payKey"></param>
            /// <param name="transactionid"></param>
            /// <param name="outTradeNo"></param>
            /// <param name="outRefundNo">32位代码</param>
            /// <param name="cert_dir"></param>
            /// <param name="total_fee"></param>
            /// <param name="refund_fee"></param>
            /// <param name="processReturnXml"></param>
            /// <returns></returns>
        public static XDocument Refund(string gzhAppid,string mId,string payKey,string transactionid,string outTradeNo,string outRefundNo,string cert_dir,int total_fee,int refund_fee,Action<XDocument> processReturnXml)
        {
            string nonceStr = GetNonce();
            RequestHandler packageReqHandler = new RequestHandler(null);

            //设置package订单参数
            packageReqHandler.SetParameter("appid", gzhAppid);		  //公众账号ID
            packageReqHandler.SetParameter("mch_id", mId);        //商户号
            if (!string.IsNullOrEmpty(transactionid))
                packageReqHandler.SetParameter("transaction_id", transactionid);       //填入微信订单号 
            if(!string.IsNullOrEmpty(outTradeNo))
                packageReqHandler.SetParameter("out_trade_no", outTradeNo);                 //填入商家订单号
            packageReqHandler.SetParameter("out_refund_no", outRefundNo);                //填入退款订单号
            packageReqHandler.SetParameter("total_fee", total_fee.ToString());               //填入总金额
            packageReqHandler.SetParameter("refund_fee", refund_fee.ToString());               //填入退款金额
            packageReqHandler.SetParameter("op_user_id", mId);   //操作员Id，默认就是商户号
            packageReqHandler.SetParameter("nonce_str", nonceStr);              //随机字符串
            string sign = packageReqHandler.CreateMd5Sign("key", payKey);
            packageReqHandler.SetParameter("sign", sign);	                    //签名
            //退款需要post的数据
            string data = packageReqHandler.ParseXML();

            //退款接口地址
            string url = "https://api.mch.weixin.qq.com/secapi/pay/refund";
            //本地或者服务器的证书位置（证书在微信支付申请成功发来的通知邮件中）
            string cert = cert_dir;//@"D:\cert\apiclient_cert.p12";
            //私钥（在安装证书时设置）
            string password = mId;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            //调用证书
            X509Certificate2 cer = new X509Certificate2(cert, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);

            #region 发起post请求
            HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
            webrequest.ClientCertificates.Add(cer);
            webrequest.Method = "post";

            byte[] postdatabyte = Encoding.UTF8.GetBytes(data);
            webrequest.ContentLength = postdatabyte.Length;
            Stream stream;
            stream = webrequest.GetRequestStream();
            stream.Write(postdatabyte, 0, postdatabyte.Length);
            stream.Close();

            HttpWebResponse httpWebResponse = (HttpWebResponse)webrequest.GetResponse();
            StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
            string responseContent = streamReader.ReadToEnd();
            #endregion

            var res = XDocument.Parse(responseContent);
            //string openid = res.Element("xml").Element("out_refund_no").Value;
            processReturnXml?.Invoke(res);
            return res;
        }
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;
            return false;
        }

        public static XDocument QueryRefund(string gzhAppid,string mid,string transactionid,string payKey,Action<XDocument>processXml)
        {
            string nonce = GetNonce();
            RequestHandler request = new RequestHandler(null);
            request.SetParameter("appid",gzhAppid);
            request.SetParameter("mch_id", mid);
            request.SetParameter("nonce_str", nonce);
            request.SetParameter("transaction_id", transactionid);
            string sign = request.CreateMd5Sign("key", payKey);
            request.SetParameter("sign",sign);
            string data = request.ParseXML();
            string result = TenPayV3.RefundQuery(data);
            var res = XDocument.Parse(result);
            processXml?.Invoke(res);
            return res;
        }
        #endregion

        public static string GetCetDir(string appid)
        {
            WxPayConfig config = MdConfigurationManager.GetConfig<WxPayConfig>();
            if (config != null)
            {
                var dir = config.CertDir + @"\" + appid + @"\";
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
            throw new MDException(typeof(WXPayHelper),"pay config get error!");
        }

        public static string GetCetFilePath(string appid)
        {
            var dir = GetCetDir(appid);
            var certFilePath = dir + "apiclient_cert.p12";
            return certFilePath;
        }

        public static bool IsCertExists(string appid)
        {
            var file = GetCetFilePath(appid);
            return File.Exists(file);
        }

        public static string GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }

            return null;
        }
    }
}
