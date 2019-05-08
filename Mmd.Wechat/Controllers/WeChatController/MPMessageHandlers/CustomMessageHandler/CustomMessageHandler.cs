/*----------------------------------------------------------------
    Copyright (C) 2015 Senparc
    
    文件名：CustomMessageHandler.cs
    文件功能描述：自定义MessageHandler
    
    
    创建标识：Senparc - 20150312
----------------------------------------------------------------*/

using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using Senparc.Weixin.MP.Agent;
using Senparc.Weixin.Context;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Weixin.MP.Helpers;
using MD.CommonService.CustomMessageHandler;
using MD.Model.Configuration;
using MD.Configuration;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch;
using MD.Lib.Weixin.User;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;

namespace MD.Wechat.CommonService.CustomMessageHandler
{
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

        string agentUrl = "http://localhost:12222/App/Weixin/4";
        string agentToken = "27C455F496044A87";
        string wiweihiKey = "CNadjJuWzyX5bz5Gn+/XoyqiqMa5DjXQ";
        static WeixinConfig wexinConfig = MD.Configuration.MdConfigurationManager.GetConfig<WeixinConfig>();

        private string appId = wexinConfig.WeixinAppId;//WebConfigurationManager.AppSettings["WeixinAppId"];
        private string appSecret = wexinConfig.WeixinAppSecret;

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
        public override IResponseMessageBase OnTextRequest(RequestMessageText requestMessage)
        {
            //这里的逻辑可以交给Service处理具体信息，参考OnLocationRequest方法或/Service/LocationSercice.cs

            //方法一（v0.1），此方法调用太过繁琐，已过时（但仍是所有方法的核心基础），建议使用方法二到四
            //var responseMessage =
            //    ResponseMessageBase.CreateFromRequestMessage(RequestMessage, ResponseMsgType.Text) as
            //    ResponseMessageText;

            //方法二（v0.4）
            //var responseMessage = ResponseMessageBase.CreateFromRequestMessage<ResponseMessageText>(RequestMessage);

            //方法三（v0.4），扩展方法，需要using Senparc.Weixin.MP.Helpers;
            //var responseMessage = RequestMessage.CreateResponseMessage<ResponseMessageText>();

            //方法四（v0.6+），仅适合在HandlerMessage内部使用，本质上是对方法三的封装
            //注意：下面泛型ResponseMessageText即返回给客户端的类型，可以根据自己的需要填写ResponseMessageNews等不同类型。

            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();

            if (requestMessage.Content != null)
            {
                string q = requestMessage.Content;
                using (var repo = new BizRepository())
                {
                    var tuple = repo.MerchantSearchByName(q, 1, 5);
                    if (tuple.Item1 > 0)
                    {
                        var openResponseMessage = requestMessage.CreateResponseMessage<ResponseMessageNews>();
                        foreach (var v in tuple.Item2)
                        {
                            openResponseMessage.Articles.Add(new Article()
                            {
                                Title = v.name,
                                Description = "点击打开",
                                Url = $"http://mmpintuan.com/Mid/Index?mid={v.mid}"
                            });
                        }
                        return openResponseMessage;
                    }
                    else
                    {
                        responseMessage.Content = "没有找到！";
                    }
                }
            }
//            else if (requestMessage.Content == "约束")
//            {
//                responseMessage.Content =
//                    @"您正在进行微信内置浏览器约束判断测试。您可以：
//<a href=""http://weixin.senparc.com/FilterTest/"">点击这里</a>进行客户端约束测试（地址：http://weixin.senparc.com/FilterTest/），如果在微信外打开将直接返回文字。
//或：
//<a href=""http://weixin.senparc.com/FilterTest/Redirect"">点击这里</a>进行客户端约束测试（地址：http://weixin.senparc.com/FilterTest/Redirect），如果在微信外打开将重定向一次URL。";
//            }
//            else if (requestMessage.Content == "1")
//            {
//                var openResponseMessage = requestMessage.CreateResponseMessage<ResponseMessageNews>();
//                openResponseMessage.Articles.Add(new Article()
//                {
//                    Title = "mm拼团欢迎你",
//                    Description = @"欢迎",
//                    //Url = WXComponentUserHelper.GenKTUrl("wx3de24bcac3bb95cb", "1","state")
//                    //Url = WXComponentUserHelper.GenComponentCallBackUrl("wx3de24bcac3bb95cb", "1-xj","state","group", "entrance"),
//                    Url = MdWxSettingUpHelper.GenEntranceUrl("wx3de24bcac3bb95cb")
//                });
//                MDLogger.LogInfoAsync(typeof(CustomMessageHandler), $"url:{openResponseMessage.Articles[0].Url}");
//                //MDLogger.LogInfoAsync(typeof(CustomMessageHandler),"open!");
//                return openResponseMessage;
//            }
//            else if (requestMessage.Content == "2")
//            {
//                var openResponseMessage = requestMessage.CreateResponseMessage<ResponseMessageNews>();
//                openResponseMessage.Articles.Add(new Article()
//                {
//                    Title = "测试核销",
//                    Description = @"测试核销",
//                    //Url = WXComponentUserHelper.GenKTUrl("wx3de24bcac3bb95cb", "1","state")
//                    //Url = WXComponentUserHelper.GenComponentCallBackUrl("wx3de24bcac3bb95cb", "1-xj","state","group", "entrance"),
//                    Url = MdWxSettingUpHelper.GenWriteOfferUrl("wx3de24bcac3bb95cb", Guid.Parse("2ee0ce2f-7be2-438b-bc48-029a3d730650"))
//                });
//                MDLogger.LogInfoAsync(typeof(CustomMessageHandler), $"核销url:{openResponseMessage.Articles[0].Url}");
//                //MDLogger.LogInfoAsync(typeof(CustomMessageHandler),"open!");
//                return openResponseMessage;
//            }
//            else if (requestMessage.Content == "open")
//            {
//                var openResponseMessage = requestMessage.CreateResponseMessage<ResponseMessageNews>();
//                openResponseMessage.Articles.Add(new Article()
//                {
//                    Title = "开放平台微信授权测试",
//                    Description = @"测试测试测试",
//                    //Url = WXComponentUserHelper.GenKTUrl("wx3de24bcac3bb95cb", "1","state")
//                    Url=WXComponentUserHelper.GenComponentCallBackUrl("wx3de24bcac3bb95cb","1-xj")
//                });
//                MDLogger.LogInfoAsync(typeof(CustomMessageHandler),$"url:{WXComponentUserHelper.GenComponentCallBackUrl("wx3de24bcac3bb95cb", "1")}");
//                //MDLogger.LogInfoAsync(typeof(CustomMessageHandler),"open!");
//                return openResponseMessage;
//            }
//            else if (requestMessage.Content == "login")
//            {
//                var openResponseMessage = requestMessage.CreateResponseMessage<ResponseMessageNews>();
//                openResponseMessage.Articles.Add(new Article()
//                {
//                    Title = "开放平台微信授权测试",
//                    Description = @"测试授权",
//                    Url = WXAuthHelper.GetAuthUrl("http://w.worldline.me/wxcallback")
//                });
//                return openResponseMessage;
//            }
//            else if (requestMessage.Content == "pay")
//            {
//                var openResponseMessage = requestMessage.CreateResponseMessage<ResponseMessageNews>();
//                string url = WXPayHelper.GenPayUrl("wx3de24bcac3bb95cb", "", "State");
//                openResponseMessage.Articles.Add(new Article()
//                {
//                    Title = "微信支付测试",
//                    Description = @"测试支付",
//                    Url = url
//                });
//                MDLogger.LogInfoAsync(typeof(CustomMessageHandler),$"正式的payurl:{url}");
//                return openResponseMessage;
//            }
//            else if (requestMessage.Content == "testpay")
//            {
//                var openResponseMessage = requestMessage.CreateResponseMessage<ResponseMessageNews>();
//                var url = WXPayHelper.GenPayTestUrl("wx3de24bcac3bb95cb", "", "State");
//                openResponseMessage.Articles.Add(new Article()
//                {
//                    Title = "微信支付测试",
//                    Description = @"测试支付",
//                    Url = url
//                });
//                MDLogger.LogInfoAsync(typeof(CustomMessageHandler), $"测试的payurl:{url}");
//                return openResponseMessage;
//            }
//            else
//            {
//                MDLogger.LogInfoAsync(typeof(CustomMessageHandler), EmojiFilter.FilterEmoji(requestMessage.Content));
//            }
            return responseMessage;
        }

        /// <summary>
        /// 处理位置请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnLocationRequest(RequestMessageLocation requestMessage)
        {
            bool updateOK = LocationManager.AddOrUpdateLocation(requestMessage.FromUserName, requestMessage.Location_X, requestMessage.Location_Y);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("OnLocationRequest->您的位置是：");
            sb.AppendLine(string.Format("经度{0},维度{1}", requestMessage.Location_Y.ToString(), requestMessage.Location_X.ToString()));
            sb.AppendLine("OpenId:" + requestMessage.FromUserName);
            sb.AppendLine("Update:" + updateOK.ToString());
            //这里是微信客户端（通过微信服务器）自动发送过来的位置信息
            var responseMessage = CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = sb.ToString();
            return responseMessage;
        }

        public override IResponseMessageBase OnShortVideoRequest(RequestMessageShortVideo requestMessage)
        {
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "您刚才发送的是小视频";
            return responseMessage;
        }

        /// <summary>
        /// 处理图片请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnImageRequest(RequestMessageImage requestMessage)
        {
            var responseMessage = CreateResponseMessage<ResponseMessageNews>();
            responseMessage.Articles.Add(new Article()
            {
                Title = "您刚才发送了图片信息",
                Description = "您发送的图片将会显示在边上",
                PicUrl = requestMessage.PicUrl,
                Url = "http://weixin.senparc.com"
            });
            responseMessage.Articles.Add(new Article()
            {
                Title = "第二条",
                Description = "第二条带连接的内容",
                PicUrl = requestMessage.PicUrl,
                Url = "http://weixin.senparc.com"
            });

            return responseMessage;
        }

        /// <summary>
        /// 处理语音请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnVoiceRequest(RequestMessageVoice requestMessage)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("您说的是不是：{0}", requestMessage.Recognition));
            sb.AppendLine("OPENID:" + requestMessage.FromUserName);
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
            var responseMessage = CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content=sb.ToString();
            return responseMessage;
        }
        /// <summary>
        /// 处理视频请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnVideoRequest(RequestMessageVideo requestMessage)
        {
            var responseMessage = CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "您发送了一条视频信息，ID：" + requestMessage.MediaId;
            return responseMessage;
        }

        /// <summary>
        /// 处理链接消息请求
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnLinkRequest(RequestMessageLink requestMessage)
        {
            var responseMessage = ResponseMessageBase.CreateFromRequestMessage<ResponseMessageText>(requestMessage);
            responseMessage.Content = string.Format(@"您发送了一条连接信息：
Title：{0}
Description:{1}
Url:{2}", requestMessage.Title, requestMessage.Description, requestMessage.Url);
            return responseMessage;
        }

        /// <summary>
        /// 处理事件请求（这个方法一般不用重写，这里仅作为示例出现。除非需要在判断具体Event类型以外对Event信息进行统一操作
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnEventRequest(IRequestMessageEventBase requestMessage)
        {
            var eventResponseMessage = base.OnEventRequest(requestMessage);//对于Event下属分类的重写方法，见：CustomerMessageHandler_Events.cs
            // 对Event信息进行统一操作
            return eventResponseMessage;
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            /* 所有没有被处理的消息会默认返回这里的结果，
             * 因此，如果想把整个微信请求委托出去（例如需要使用分布式或从其他服务器获取请求），
             * 只需要在这里统一发出委托请求，如：
             * var responseMessage = MessageAgent.RequestResponseMessage(agentUrl, agentToken, RequestDocument.ToString());
             * return responseMessage;
             */

            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "这条消息来自DefaultResponseMessage。";
            return responseMessage;
        }
    }
}
