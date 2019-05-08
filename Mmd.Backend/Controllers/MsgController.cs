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
using MD.Lib.Log;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration;
using Senparc.Weixin;
using Senparc.Weixin.Exceptions;

using Senparc.Weixin.Open.MessageHandlers;
using Tencent;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Weixin.Context;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.Entities;
using MD.Lib.DB.Repositorys;
using Senparc.Weixin.MP.Helpers;
using Senparc.Weixin.MessageHandlers;
using MD.Model.DB;
using MD.Lib.DB.Redis.MD;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.MQ.MD;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using MD.Lib.ElasticSearch;
using Senparc.Weixin.Open.ComponentAPIs;

namespace Mmd.Backend.Controllers
{
    public class MsgController : Controller
    {
        [HttpPost]
        // GET: Msg
        public ActionResult callback(PostModel postModel, string appid)
        {
            //此处的appid参数为第三方用户的appid用于业务逻辑，不用于解密消息。

            try
            {
                //var openConfig = MD.Configuration.MdConfigurationManager.GetConfig<WXOpenConfig>();
                //if (openConfig == null)
                //{
                //    MDLogger.LogError(typeof(MsgController), new Exception("没有取到配置信息！"));
                //    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                //}

                if (string.IsNullOrEmpty(postModel?.Msg_Signature) || string.IsNullOrEmpty(postModel.Nonce) || string.IsNullOrEmpty(postModel.Timestamp))
                {
                    //ActionResult result = new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    MDLogger.LogError(typeof(MsgController), new Exception($"Paramerters Error,appid:{appid},Msg_Signature:{postModel?.Msg_Signature},Nonce:{postModel.Nonce},Timestamp:{postModel.Timestamp}"));
                    //return result;
                    return Content("");
                }
                else
                {
                    postModel.AppId = "wx323abc83f8c7e444"; //openConfig.AppId;
                    postModel.Token = "open_mmpintuan";//openConfig.Token;
                    postModel.EncodingAESKey = "VHINhw6MAt3fIZhoVYRx5ESqwhRuNGuJC2ixnSAW9Wz";//openConfig.EncodingAESKey;
                    //MDLogger.LogInfo(typeof(HomeController), "消息：" + $"appid:{postModel.appid}");
                }
                int maxRecordCount = 10;
                //var myhandler = new MDOpenCustomMessageHandler2(HttpContext.Request.InputStream, postModel);
                var myhandler = new CustomMessageHandler(Request.InputStream, postModel, maxRecordCount);
                myhandler.Execute();
                //return new ContentResult()
                //{
                //    Content = myhandler.TextResponseMessage,
                //    ContentEncoding = Encoding.UTF8
                //};
                return new WeixinResult(myhandler);//v0.8+
            }
            catch (Exception ex)
            {
                MDLogger.LogError(typeof(MsgController), ex);
                //throw new MDException(typeof(HomeController), ex);
                return Content("");
            }
        }
    }


    /// <summary>
    /// 自定义MessageHandler
    /// 把MessageHandler作为基类，重写对应请求的处理方法
    /// </summary>
    public partial class CustomMessageHandler : MessageHandler<CustomMessageContext>
    {
        /*
         * 重要提示：v1.5起，MessageHandler提供了一个DefaultResponseMessage的抽象方法，
         * DefaultResponseMessage必须在子类中重写，用于返回没有处理过的消息类型（也可以用于默认消息，如帮助信息等）；
         * 其中所有原OnXX的抽象方法已经都改为虚方法，可以不必每个都重写。若不重写，默认返回DefaultResponseMessage方法中的结果。
         */

        static WXOpenConfig wexinConfig = MD.Configuration.MdConfigurationManager.GetConfig<WXOpenConfig>();

        private string appId = wexinConfig.AppId;//WebConfigurationManager.AppSettings["WeixinAppId"];
        private string appSecret = wexinConfig.AppSecret;

        public CustomMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0)
            : base(inputStream, postModel, maxRecordCount)
        {
            //这里设置仅用于测试，实际开发可以在外部更全局的地方设置，
            //比如MessageHandler<MessageContext>.GlobalWeixinContext.ExpireMinutes = 3。
            WeixinContext.ExpireMinutes = 3;

            if (!string.IsNullOrEmpty(postModel.AppId))
            {
                appId = postModel.AppId;//通过第三方开放平台发送过来的请求
            }
        }



        public override void OnExecuting()
        {
            //测试MessageContext.StorageData
            if (CurrentMessageContext.StorageData == null)
            {
                CurrentMessageContext.StorageData = 0;
            }
            base.OnExecuting();
        }

        public override void OnExecuted()
        {
            base.OnExecuted();
            CurrentMessageContext.StorageData = ((int)CurrentMessageContext.StorageData) + 1;
        }

        /// <summary>
        /// 处理文字请求
        /// </summary>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnTextRequest(RequestMessageText requestMessage)
        {
            return DefaultResponseMessage(requestMessage);
            //if (requestMessage.ToUserName != "gh_3c884a361561")
            //{
            //    return DefaultResponseMessage(requestMessage);
            //}
            //else
            //{
            //    var responseMessage = CreateResponseMessage<ResponseMessageText>();
            //    requestMessage.Content = "";
            //    if (requestMessage.Content == "TESTCOMPONENT_MSG_TYPE_TEXT")
            //    {
            //        responseMessage.Content = "TESTCOMPONENT_MSG_TYPE_TEXT_callback";
            //        return responseMessage;
            //    }
            //    if (requestMessage.Content.IndexOf("QUERY_AUTH_CODE") > -1)
            //    {
            //        string code = requestMessage.Content.Replace("QUERY_AUTH_CODE:", "");
            //        code = HttpUtility.UrlDecode(code);
            //        string openid = requestMessage.FromUserName;
            //        string component_accessToken = WXComponentHelper.GetComponentAccessToken();
            //        string component_appid = "wx323abc83f8c7e444";
            //        var queryAuthResult = ComponentApi.QueryAuth(component_accessToken, component_appid, code);
            //        if (queryAuthResult != null)
            //        {
            //            string at = queryAuthResult.authorization_info.authorizer_access_token;
            //            string URL_FORMAT = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}";
            //            var sendData = new
            //            {
            //                touser = openid,
            //                msgtype = "text",
            //                text = new
            //                {
            //                    content = code + "_from_api"
            //                }
            //            };
            //            Senparc.Weixin.CommonAPIs.CommonJsonSend.Send<WxJsonResult>(at, URL_FORMAT, sendData);
            //        }
            //        return null;
            //    }
            //    return responseMessage;
            //}
        }
        
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnTextOrEventRequest(RequestMessageText requestMessage)
        {
            // 预处理文字或事件类型请求。
            // 这个请求是一个比较特殊的请求，通常用于统一处理来自文字或菜单按钮的同一个执行逻辑，
            // 会在执行OnTextRequest或OnEventRequest之前触发，具有以下一些特征：
            // 1、如果返回null，则继续执行OnTextRequest或OnEventRequest
            // 2、如果返回不为null，则终止执行OnTextRequest或OnEventRequest，返回最终ResponseMessage
            // 3、如果是事件，则会将RequestMessageEvent自动转为RequestMessageText类型，其中RequestMessageText.Content就是RequestMessageEvent.EventKey

            //if (requestMessage.Content == "OneClick")
            //{
            //    var strongResponseMessage = CreateResponseMessage<ResponseMessageText>();
            //    strongResponseMessage.Content = "您点击了底部按钮。\r\n为了测试微信软件换行bug的应对措施，这里做了一个——\r\n换行";
            //    return strongResponseMessage;
            //}
            return null;//返回null，则继续执行OnTextRequest或OnEventRequest
        }

        /// <summary>
        /// 处理位置请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnLocationRequest(RequestMessageLocation requestMessage)
        {
            //bool updateOK = LocationManager.AddOrUpdateLocation(requestMessage.FromUserName, requestMessage.Location_X, requestMessage.Location_Y);

            //StringBuilder sb = new StringBuilder();
            //sb.AppendLine("OnLocationRequest->您的位置是：");
            //sb.AppendLine(string.Format("经度{0},维度{1}", requestMessage.Location_Y.ToString(), requestMessage.Location_X.ToString()));
            //sb.AppendLine("OpenId:" + requestMessage.FromUserName);
            //sb.AppendLine("Update:" + updateOK.ToString());
            //这里是微信客户端（通过微信服务器）自动发送过来的位置信息
            return DefaultResponseMessage(requestMessage);
        }

        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnShortVideoRequest(RequestMessageShortVideo requestMessage)
        {
            return DefaultResponseMessage(requestMessage);
        }

        /// <summary>
        /// 处理图片请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnImageRequest(RequestMessageImage requestMessage)
        {
            //var responseMessage = CreateResponseMessage<ResponseMessageNews>();
            //responseMessage.Articles.Add(new Article()
            //{
            //    Title = "您刚才发送了图片信息",
            //    Description = "您发送的图片将会显示在边上",
            //    PicUrl = requestMessage.PicUrl,
            //    Url = "http://weixin.senparc.com"
            //});
            //responseMessage.Articles.Add(new Article()
            //{
            //    Title = "第二条",
            //    Description = "第二条带连接的内容",
            //    PicUrl = requestMessage.PicUrl,
            //    Url = "http://weixin.senparc.com"
            //});
            return DefaultResponseMessage(requestMessage);
            //var responseMessage = CreateResponseMessage<ResponseMessageText>();
            //responseMessage.Content = "";
            //return responseMessage;
        }

        /// <summary>
        /// 处理语音请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnVoiceRequest(RequestMessageVoice requestMessage)
        {
            return DefaultResponseMessage(requestMessage);
            //StringBuilder sb = new StringBuilder();
            //sb.AppendLine(string.Format("您说的是不是：{0}", requestMessage.Recognition));
            //sb.AppendLine("OPENID:" + requestMessage.FromUserName);
            //var responseMessage = CreateResponseMessage<ResponseMessageMusic>();
            ////上传缩略图
            //var accessToken = CommonAPIs.AccessTokenContainer.TryGetAccessToken(appId, appSecret);
            //var uploadResult = AdvancedAPIs.MediaApi.UploadTemporaryMedia(accessToken, UploadMediaFileType.image,
            //                                             Server.GetMapPath("~/Images/Logo.jpg"));

            ////设置音乐信息
            //responseMessage.Music.Title = "天籁之音";
            //responseMessage.Music.Description = "播放您上传的语音";
            //responseMessage.Music.MusicUrl = "http://weixin.senparc.com/Media/GetVoice?mediaId=" + requestMessage.MediaId;
            //responseMessage.Music.HQMusicUrl = "http://weixin.senparc.com/Media/GetVoice?mediaId=" + requestMessage.MediaId;
            //responseMessage.Music.ThumbMediaId = uploadResult.media_id;
            //return responseMessage;
            //var responseMessage = CreateResponseMessage<ResponseMessageText>();
            //responseMessage.Content = sb.ToString();
            //return responseMessage;
        }
        /// <summary>
        /// 处理视频请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnVideoRequest(RequestMessageVideo requestMessage)
        {
            //var responseMessage = CreateResponseMessage<ResponseMessageText>();
            //responseMessage.Content = "您发送了一条视频信息，ID：" + requestMessage.MediaId;
            //return responseMessage;
            return DefaultResponseMessage(requestMessage);
        }

        /// <summary>
        /// 处理链接消息请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnLinkRequest(RequestMessageLink requestMessage)
        {
            //            var responseMessage = Senparc.Weixin.MP.Entities.ResponseMessageBase.CreateFromRequestMessage<ResponseMessageText>(requestMessage);
            //            responseMessage.Content = string.Format(@"您发送了一条连接信息：
            //Title：{0}
            //Description:{1}
            //Url:{2}", requestMessage.Title, requestMessage.Description, requestMessage.Url);
            //            return responseMessage;
            return DefaultResponseMessage(requestMessage);
        }

        /// <summary>
        /// 处理事件请求（这个方法一般不用重写，这里仅作为示例出现。除非需要在判断具体Event类型以外对Event信息进行统一操作
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnEventRequest(IRequestMessageEventBase requestMessage)
        {
            var eventResponseMessage = base.OnEventRequest(requestMessage);//对于Event下属分类的重写方法，见：CustomerMessageHandler_Events.cs
                                                                           // 对Event信息进行统一操作
            return eventResponseMessage;
            //if (requestMessage.ToUserName != "gh_3c884a361561")
            //{
            //    var eventResponseMessage = base.OnEventRequest(requestMessage);//对于Event下属分类的重写方法，见：CustomerMessageHandler_Events.cs
            //       // 对Event信息进行统一操作
            //    return eventResponseMessage;
            //}
            //else
            //{
            //    var responseMessage = CreateResponseMessage<ResponseMessageText>();
            //    responseMessage.Content = requestMessage.Event.ToString() +"from_callback";
            //    return responseMessage;
            //}
            
        }

        /// <summary>
        /// 订阅（关注）事件
        /// </summary>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnEvent_SubscribeRequest(RequestMessageEvent_Subscribe requestMessage)
        {
            var responseMessage = CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "";
            /*********保存关注用户的openid到redis，后期更新***********/

            string openid = requestMessage.FromUserName;
            string mp_id = requestMessage.ToUserName;
            using (BizRepository rep = new BizRepository())
            {
                Merchant mer = rep.GetMerchantByMpId(mp_id);
                RedisUserOp.SaveOpenid(new UserSubMapRedis() { OpenId = openid, Appid = mer == null ? "" : mer.wx_appid });
                if (mer != null)
                {
                    EsBizLogStatistics.AddSubBizViewLog(ELogBizModuleType.UserSub, mp_id, openid, mer.mid, mer.wx_appid);
                    string id = RedisUserOp.GetTmpId(openid);//从redis读取跳转到关注页之前的gid或goid
                    if (!string.IsNullOrEmpty(id))
                    {
                        var obj = MqWxTempMsgManager.GenNewsObject(mer.wx_appid, openid, id);
                        MqWxTempMsgManager.SendMessage(obj);
                        RedisUserOp.DelTmpId(openid);
                    }
                }
            }
            /******************************************************/
            return responseMessage;
        }

        /// <summary>
        /// 退订
        /// 实际上用户无法收到非订阅账号的消息，所以这里可以随便写。
        /// unsubscribe事件的意义在于及时删除网站应用中已经记录的OpenID绑定，消除冗余数据。并且关注用户流失的情况。
        /// </summary>
        /// <returns></returns>
        public override Senparc.Weixin.MP.Entities.IResponseMessageBase OnEvent_UnsubscribeRequest(RequestMessageEvent_Unsubscribe requestMessage)
        {
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "";
            /********删除redis中关注用户的openid，后期更新******/
            string openid = requestMessage.FromUserName;
            string mp_id = requestMessage.ToUserName;
            RedisUserOp.DeleteOpenid(openid);
            using (BizRepository rep = new BizRepository())
            {
                Merchant mer = rep.GetMerchantByMpId(mp_id);
                if (mer != null)
                {
                    EsBizLogStatistics.AddSubBizViewLog(ELogBizModuleType.UserUnsub, mp_id, openid, mer.mid, mer.wx_appid);
                }
            }
            /************************************************/
            return responseMessage;
        }

        public override Senparc.Weixin.MP.Entities.IResponseMessageBase DefaultResponseMessage(Senparc.Weixin.MP.Entities.IRequestMessageBase requestMessage)
        {
            /* 所有没有被处理的消息会默认返回这里的结果，
             * 因此，如果想把整个微信请求委托出去（例如需要使用分布式或从其他服务器获取请求），
             * 只需要在这里统一发出委托请求，如：
             * var responseMessage = MessageAgent.RequestResponseMessage(agentUrl, agentToken, RequestDocument.ToString());
             * return responseMessage;
             */

            //var responseMessage = CreateResponseMessage<ResponseMessageText>();
            //responseMessage.Content = "success";
            //return responseMessage;
            return null;
        }
    }

    public class CustomMessageContext : MessageContext<Senparc.Weixin.MP.Entities.IRequestMessageBase, Senparc.Weixin.MP.Entities.IResponseMessageBase>
    {
        public CustomMessageContext()
        {
            base.MessageContextRemoved += CustomMessageContext_MessageContextRemoved;
        }

        /// <summary>
        /// 当上下文过期，被移除时触发的时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CustomMessageContext_MessageContextRemoved(object sender, Senparc.Weixin.Context.WeixinContextRemovedEventArgs<Senparc.Weixin.MP.Entities.IRequestMessageBase, Senparc.Weixin.MP.Entities.IResponseMessageBase> e)
        {
            /* 注意，这个事件不是实时触发的（当然你也可以专门写一个线程监控）
             * 为了提高效率，根据WeixinContext中的算法，这里的过期消息会在过期后下一条请求执行之前被清除
             */

            var messageContext = e.MessageContext as CustomMessageContext;
            if (messageContext == null)
            {
                return;//如果是正常的调用，messageContext不会为null
            }

            //这里根据需要执行消息过期时候的逻辑，下面的代码仅供参考

            //Log.InfoFormat("{0}的消息上下文已过期",e.OpenId);
            //api.SendMessage(e.OpenId, "由于长时间未搭理客服，您的客服状态已退出！");
        }
    }

    /// <summary>
    /// 返回MessageHandler结果
    /// </summary>
    public class WeixinResult : ContentResult
    {
        //private string _content;
        protected IMessageHandlerDocument _messageHandlerDocument;

        public WeixinResult(string content)
        {
            //_content = content;
            base.Content = content;
        }

        public WeixinResult(IMessageHandlerDocument messageHandlerDocument)
        {
            _messageHandlerDocument = messageHandlerDocument;
        }

        /// <summary>
        /// 获取ContentResult中的Content或IMessageHandler中的ResponseDocument文本结果。
        /// 一般在测试的时候使用。
        /// </summary>
        new public string Content
        {
            get
            {
                if (base.Content != null)
                {
                    return base.Content;
                }
                else if (_messageHandlerDocument != null && _messageHandlerDocument.FinalResponseDocument != null)
                {
                    return _messageHandlerDocument.FinalResponseDocument.ToString();
                }
                else
                {
                    return null;
                }
            }
            set { base.Content = value; }
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (base.Content == null)
            {
                //使用IMessageHandler输出
                if (_messageHandlerDocument == null)
                {
                    throw new Senparc.Weixin.Exceptions.WeixinException("执行WeixinResult时提供的MessageHandler不能为Null！", null);
                }

                if (_messageHandlerDocument.FinalResponseDocument == null)
                {
                    //throw new Senparc.Weixin.MP.WeixinException("ResponseMessage不能为Null！", null);
                }
                else
                {
                    context.HttpContext.Response.ClearContent();
                    context.HttpContext.Response.ContentType = "text/xml";
                    _messageHandlerDocument.FinalResponseDocument.Save(context.HttpContext.Response.OutputStream);
                }
            }

            base.ExecuteResult(context);
        }
    }
}