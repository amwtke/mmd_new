using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.Entities;
using MD.Lib.Weixin.Component;

namespace Senparc.Weixin.MP.AdvancedAPIs.MyExtension
{
    /// <summary>
    /// 模板消息接口
    /// </summary>
    public static class MyTemplateApi
    {
        /// <summary>
        /// 模板消息接口
        /// </summary>
        /// <param name="accessTokenOrAppId"></param>
        /// <param name="openId"></param>
        /// <param name="templateId"></param>
        /// <param name="topcolor"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="timeOut">代理请求超时时间（毫秒）</param>
        /// <returns></returns>
        public static async System.Threading.Tasks.Task<SendTemplateMessageResult> SendTemplateMessageAsync(string accessToken, string openId, string templateId, string topcolor, string url, object data, int timeOut = Config.TIME_OUT)
        {

            const string urlFormat = "https://api.weixin.qq.com/cgi-bin/message/template/send?access_token={0}";
            var msgData = new TempleteModel()
            {
                touser = openId,
                template_id = templateId,
                topcolor = topcolor,
                url = url,
                data = data
            };
            SendTemplateMessageResult result = await Senparc.Weixin.CommonAPIs.CommonJsonSend.SendAsync<SendTemplateMessageResult>(accessToken, urlFormat, msgData, timeOut: timeOut);
            return result;

        }

        public static async Task<AddTemplateMessageResult> AddemplateMessageAsync(string accessToken, string tempInShort, int timeOut = Config.TIME_OUT)
        {

            const string urlFormat = "https://api.weixin.qq.com/cgi-bin/template/api_add_template?access_token={0}";
            var msgData = new 
            {
                template_id_short = tempInShort
            };
            AddTemplateMessageResult result = await Senparc.Weixin.CommonAPIs.CommonJsonSend.SendAsync<AddTemplateMessageResult>(accessToken, urlFormat, msgData, timeOut: timeOut);
            return result;
        }

        public static async Task<WxJsonResult> SendNewsAsync(string accessToken, string openId,object data)
        {
            //string accessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
            string URL_FORMAT = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}";
            List<Article> list = data as List<Article>;
            var sendData = new
            {
                touser = openId,
                msgtype = "news",
                news = new
                {
                    articles = list.Select(
                        a => new {
                            title = a.Title,
                            description = a.Description,
                            picurl = a.PicUrl,
                            url = a.Url }).ToList()
                }
            };
            WxJsonResult result = await Senparc.Weixin.CommonAPIs.CommonJsonSend.SendAsync<WxJsonResult>(accessToken, URL_FORMAT, sendData);
            return result;
        }

        public static  WxJsonResult SendNews(string accessToken, string openId, object data)
        {
            string URL_FORMAT = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}";
            List<Article> list = data as List<Article>;
            var sendData = new
            {
                touser = openId,
                msgtype = "news",
                news = new
                {
                    articles = list.Select(
                        a => new {
                            title = a.Title,
                            description = a.Description,
                            picurl = a.PicUrl,
                            url = a.Url
                        }).ToList()
                }
            };
            WxJsonResult result = Senparc.Weixin.CommonAPIs.CommonJsonSend.Send<WxJsonResult>(accessToken, URL_FORMAT, sendData);
            return result;
        }
    }
}
