using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using mmd.wechat.Controllers;
using MD.Lib.Log;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration;
using Senparc.Weixin;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.Open;
using Senparc.Weixin.Open.Entities.Request;
using Senparc.Weixin.Open.MessageHandlers;
using Tencent;

namespace MD.Wechat.Controllers
{

    //http://localhost:2895/
    //POST /wxab51f281f3b2764d/callback?signature=77350eb2441860f18d29f3f5ea51bdb2d5286c72&timestamp=1467341370&nonce=770471808&openid=o_pkOuKLT6YnuHOpypyiJByGbQX0&encrypt_type=aes&msg_signature=ae941653bc8624d2239c2a0bc1d90de84f8f4f83 HTTP/1.0
        //User-Agent: Mozilla/4.0
        //Accept: */*
        //Host: www.msg.mmpintuan.com
        //Pragma: no-cache
        //Content-Length: 618
        //Content-Type: text/xml
    //<xml>
    //<ToUserName><![CDATA[gh_3ef18cef2b2e]]></ToUserName>
    //<Encrypt><![CDATA[nNhj8wZQkA8l3lcf3fwlx9i7E5CDcILz1KZl9nLnCBPW3fOM8vWYgPpTw6/wPJ2//E7Oxk8Zv7OR6qls7O9sc4q6BXE7XenMV8cAwGDtX4vZmslGDMhetEgSq+ufyBCqWjKQ+R9P9aZgEznHizzxyRXDh4mEzRgXIxTw0dHXnQawuWw4WPxmvqXq17KzdY5k85abRUQq35FecEBerfXLuNN7KxXUi1ab5BRGIFGUBaAQIx5qEqrrYN33k/Qz1UEUx12SZTU81kSvgZAl/vJfZ7Rpi9JYglkqGExjZyiebOpuphjqBWbi+SpcFuFShbdnN9GUlGu33fYWMONa7coW9QGknKqLC2RIXAqRsOjJ6EqGhtIAiDQRViddEmLqWLD1IFtg33y8zfxTId+4u7mY28G5bSPCihu/sErolR/7s3AD3XX7ziGYPjFxWyBxS4UwkjgWPtUU4QjH050l1WgwB/7CZKwCjb4pIYc/wjF1Ifo6IhXx3OPA7la/uUFOZMXgH]]></Encrypt>
    //</xml>
    public class MsgController : Controller
    {
        [HttpPost]
        // GET: Msg
        public ActionResult callback(MsgPostModel postModel,string appid)
        {
            //此处的appid参数为第三方用户的appid用于业务逻辑，不用于解密消息。

            try
            {
                var openConfig = MD.Configuration.MdConfigurationManager.GetConfig<WXOpenConfig>();
                if (openConfig == null)
                {
                    MDLogger.LogError(typeof(MsgController), new Exception("没有取到配置信息！"));
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                }
                if (String.IsNullOrEmpty(postModel?.Msg_Signature) || string.IsNullOrEmpty(postModel.Nonce) || string.IsNullOrEmpty(postModel.Timestamp))
                {
                    ActionResult result = new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    MDLogger.LogError(typeof(HomeController), new Exception("postModel没取到"));
                    return result;
                }
                else
                {
                    postModel.appid = openConfig.AppId;
                    postModel.Token = openConfig.Token;
                    postModel.EncodingAESKey = openConfig.EncodingAESKey;

                    //MDLogger.LogInfo(typeof(HomeController), "消息：" + $"appid:{postModel.appid}");
                }
                var myhandler = new MDOpenCustomMessageHandler(HttpContext.Request.InputStream, postModel);
                myhandler.Execute();
                return new ContentResult()
                {
                    Content = myhandler.ResponseMessageText,
                    ContentEncoding = Encoding.UTF8
                };
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(HomeController), ex);
            }
        }
    }



    public class MDOpenCustomMessageHandler : MessageHandler
    {
        public MDOpenCustomMessageHandler(Stream inputStream, MsgPostModel postModel = null)
            : base(inputStream, postModel)
        {

        }

        public override string OnComponentVerifyTicketRequest(RequestMessageComponentVerifyTicket requestMessage)
        {
            bool flag = WXComponentHelper.SaveVerifyTicket(requestMessage);
            return base.OnComponentVerifyTicketRequest(requestMessage);//返回success给微信。
        }

        public override string OnUnauthorizedRequest(RequestMessageUnauthorized requestMessage)
        {
            // 取消授权的流程
            return base.OnUnauthorizedRequest(requestMessage);
        }
    }

    public abstract class MessageHandler
    {
        private MsgPostModel _postModel;
        /// <summary>
        /// 加密（原始）的XML
        /// </summary>
        public XDocument EcryptRequestDocument { get; set; }
        /// <summary>
        /// 解密之后的XML
        /// </summary>
        public XDocument RequestDocument { get; set; }
        /// <summary>
        /// 请求消息，对应解密之之后的XML数据
        /// </summary>
        //public IRequestMessageBase RequestMessage { get; set; }

        public string ResponseMessageText { get; set; }

        public bool CancelExcute { get; set; }

        public MessageHandler(Stream inputStream, MsgPostModel postModel = null)
        {
            _postModel = postModel;
            EcryptRequestDocument = Senparc.Weixin.XmlUtility.XmlUtility.Convert(inputStream);//原始加密XML转成XDocument

            Init();
        }

        public XDocument Init()
        {
            //解密XML信息
            var postDataStr = EcryptRequestDocument.ToString();

            WXBizMsgCrypt msgCrype = new WXBizMsgCrypt(_postModel.Token, _postModel.EncodingAESKey, _postModel.appid);
            string msgXml = null;
            var result = msgCrype.DecryptMsg(_postModel.Msg_Signature, _postModel.Timestamp, _postModel.Nonce, postDataStr, ref msgXml);

            //判断result类型
            if (result != 0)
            {
                //验证没有通过，取消执行
                CancelExcute = true;
                return null;
            }

            RequestDocument = XDocument.Parse(msgXml);//完成解密

            //((RequestMessageBase)RequestMessage).FillEntityWithXml(RequestDocument);

            return RequestDocument;
        }

        public void Execute()
        {
            if (CancelExcute)
            {
                return;
            }

            OnExecuting();

            if (CancelExcute)
            {
                return;
            }

            try
            {
                if (RequestDocument == null)
                {
                    return;
                }

                ResponseMessageText = OnRequestMsg(RequestDocument);
            }
            catch (Exception ex)
            {
                throw new MessageHandlerException("ThirdPartyMessageHandler中Execute()过程发生错误：" + ex.Message, ex);
            }
            finally
            {
                OnExecuted();
            }
        }

        public virtual void OnExecuting()
        {
        }

        public virtual void OnExecuted()
        {
        }

        public virtual string OnRequestMsg(XDocument RequestDocument )
        {
            return "success";
        }

        /// <summary>
        /// 推送component_verify_ticket协议
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public virtual string OnComponentVerifyTicketRequest(RequestMessageComponentVerifyTicket requestMessage)
        {
            return "success";
        }

        /// <summary>
        /// 推送取消授权通知
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public virtual string OnUnauthorizedRequest(RequestMessageUnauthorized requestMessage)
        {
            return "success";
        }
    }

    public class MsgPostModel : EncryptPostModel
    {
        public string ToUserName { get; set; }

        public string appid { get; set; }
        /// <summary>
        /// 加密类型，通常为"aes"
        /// </summary>
        public string Encrypt_Type { get; set; }
    }
}
