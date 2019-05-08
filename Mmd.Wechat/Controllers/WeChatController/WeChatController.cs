﻿using System;
using System.Web.Mvc;
using MD.Lib.Log;
using MD.Model.Configuration;
using MD.Wechat.CommonService.CustomMessageHandler;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MvcExtension;

namespace MD.Wechat.Controllers
{
    public class WeChatController : Controller
    {
        static MD.Model.Configuration.WeixinConfig wexinConfig = MD.Configuration.MdConfigurationManager.GetConfig<WeixinConfig>();

        public static readonly string Token = wexinConfig.WeixinToken;
        //与微信公众账号后台的Token设置保持一致，区分大小写。
        public static readonly string EncodingAESKey = wexinConfig.WeixinEncodingAESKey;            
        //与微信公众账号后台的EncodingAESKey设置保持一致，区分大小写。
        public static readonly string AppId = wexinConfig.WeixinAppId;
        ////与微信公众账号后台的AppId设置保持一致，区分大小写。

        readonly Func<string> _getRandomFileName = () => DateTime.Now.Ticks + Guid.NewGuid().ToString("n").Substring(0, 6);
        //public ActionResult Index()
        //{
        //    if (HttpContext.Request.Url != null)
        //    {
        //        var s = HttpContext.Request.Url.Authority;
        //        return Content(s);
        //    }
        //    return Content("null");
        //}

        [HttpGet]
        [ActionName("Index")]
        public ActionResult Get(PostModel postModel, string echostr)
        {
            if (CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, Token))
            {
                MDLogger.LogInfoAsync(typeof(WeChatController), "微信成功对接！SUCCEED!");
                return Content(echostr); //返回随机字符串则表示验证通过
            }
            else
            {
                MDLogger.LogInfoAsync(typeof(WeChatController), "微信验证失败!如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。");
                return Content("failed:" + postModel.Signature + "," + CheckSignature.GetSignature(postModel.Timestamp, postModel.Nonce, Token) + "。" +
                    "如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。");
            }
        }

        [HttpPost]
        [ActionName("Index")]
        public ActionResult Post(PostModel postModel)
        {
            if (!CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, Token))
            {
                MDLogger.LogInfoAsync(typeof(WeChatController), "failed:" + postModel.Signature + "," + CheckSignature.GetSignature(postModel.Timestamp, postModel.Nonce, Token) + "。" +
                "如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。");
                return Content("参数错误！");
            }
            //MDLogger.LogInfoAsync(typeof (WeChatController),
                //$"请求成功，signature:{postModel.Signature},appid:{postModel.AppId},timestamp:{postModel.Timestamp}");


            postModel.Token = Token;
            postModel.EncodingAESKey = EncodingAESKey;//根据自己后台的设置保持一致
            postModel.AppId = AppId;//根据自己后台的设置保持一致

            //v4.2.2之后的版本，可以设置每个人上下文消息储存的最大数量，防止内存占用过多，如果该参数小于等于0，则不限制
            var maxRecordCount = 10;

            //var logPath = Server.MapPath(string.Format("~/App_Data/MP/{0}/", DateTime.Now.ToString("yyyy-MM-dd")));
            //if (!Directory.Exists(logPath))
            //{
            //    Directory.CreateDirectory(logPath);
            //}

            //自定义MessageHandler，对微信请求的详细判断操作都在这里面。
            var messageHandler = new CustomMessageHandler(Request.InputStream, postModel, maxRecordCount);


            try
            {
                //测试时可开启此记录，帮助跟踪数据，使用前请确保App_Data文件夹存在，且有读写权限。
                //messageHandler.RequestDocument.Save(Path.Combine(logPath, string.Format("{0}_Request_{1}.txt", _getRandomFileName(), messageHandler.RequestMessage.FromUserName)));
                if (messageHandler.UsingEcryptMessage)
                {
                    // messageHandler.EcryptRequestDocument.Save(Path.Combine(logPath, string.Format("{0}_Request_Ecrypt_{1}.txt", _getRandomFileName(), messageHandler.RequestMessage.FromUserName)));
                }

                /* 如果需要添加消息去重功能，只需打开OmitRepeatedMessage功能，SDK会自动处理。
                 * 收到重复消息通常是因为微信服务器没有及时收到响应，会持续发送2-5条不等的相同内容的RequestMessage*/
                messageHandler.OmitRepeatedMessage = true;


                //执行微信处理过程
                messageHandler.Execute();

                //测试时可开启，帮助跟踪数据

                //if (messageHandler.ResponseDocument == null)
                //{
                //    throw new Exception(messageHandler.RequestDocument.ToString());
                //}

                if (messageHandler.ResponseDocument != null)
                {
                    // messageHandler.ResponseDocument.Save(Path.Combine(logPath, string.Format("{0}_Response_{1}.txt", _getRandomFileName(), messageHandler.RequestMessage.FromUserName)));
                }

                if (messageHandler.UsingEcryptMessage)
                {
                    //记录加密后的响应信息
                    // messageHandler.FinalResponseDocument.Save(Path.Combine(logPath, string.Format("{0}_Response_Final_{1}.txt", _getRandomFileName(), messageHandler.RequestMessage.FromUserName)));
                }

                //return Content(messageHandler.ResponseDocument.ToString());//v0.7-
                return new FixWeixinBugWeixinResult(messageHandler);//为了解决官方微信5.0软件换行bug暂时添加的方法，平时用下面一个方法即可
                //return new WeixinResult(messageHandler);//v0.8+
            }
            catch (Exception ex)
            {
                // 
                //using (TextWriter tw = new StreamWriter(Server.MapPath("~/App_Data/Error_" + _getRandomFileName() + ".txt")))
                //{
                //    tw.WriteLine("ExecptionMessage:" + ex.Message);
                //    tw.WriteLine(ex.Source);
                //    tw.WriteLine(ex.StackTrace);
                //    //tw.WriteLine("InnerExecptionMessage:" + ex.InnerException.Message);

                //    if (messageHandler.ResponseDocument != null)
                //    {
                //        tw.WriteLine(messageHandler.ResponseDocument.ToString());
                //    }

                //    if (ex.InnerException != null)
                //    {
                //        tw.WriteLine("========= InnerException =========");
                //        tw.WriteLine(ex.InnerException.Message);
                //        tw.WriteLine(ex.InnerException.Source);
                //        tw.WriteLine(ex.InnerException.StackTrace);
                //    }

                //    tw.Flush();
                //    tw.Close();
            }
            return Content("");
        }
    }
}