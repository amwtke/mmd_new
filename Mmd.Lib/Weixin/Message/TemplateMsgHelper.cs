using MD.Lib.Weixin.Token;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Redis;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.WeChat;
using Senparc.Weixin.MP.AdvancedAPIs.MyExtension;

namespace MD.Lib.Weixin.Message
{

    public static class TemplateMsgHelper
    {
        static Dictionary<TemplateType, string> _dic = new Dictionary<TemplateType, string>();
        static TemplateMsgHelper()
        {
            _dic.Add(TemplateType.PTFail, "OPENTM401113750");
            _dic.Add(TemplateType.PTSuccess, "OPENTM400932513");
            _dic.Add(TemplateType.PaySuccess, "TM00015");
            _dic.Add(TemplateType.PTRemind, "OPENTM406281958");
            //_dic.Add(TemplateType.LotteryResult, "OPENTM204632492");
            _dic.Add(TemplateType.LotteryResult, "OPENTM206854010");
        }

        public static string DefaultColor => "#E61C64";

        public static string GetShortId(TemplateType type)
        {
            return _dic[type];
        }

        public static async Task<string> GetTempId(string appid, string shortId)
        {
            try
            {
                if (string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(shortId))
                    return null;

                string key = WxTemplateMsgRedis.makeKey(appid, shortId);

                WxTemplateMsgRedis obj =
                    await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<WxTemplateMsgRedis>(key);

                //如果appid没有注册此模板消息
                if (string.IsNullOrEmpty(obj?.tempId))
                {
                    string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);

                    var result = await MyTemplateApi.AddemplateMessageAsync(at, shortId);
                    if (!string.IsNullOrEmpty(result?.template_id))
                    {
                        obj = new WxTemplateMsgRedis
                        {
                            key = key,
                            tempId = result.template_id
                        };
                        await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(obj);
                    }
                }
                return obj.tempId;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(TemplateMsgHelper), ex);
            }
        }
        /// <summary>
        /// 获取模板消息的id
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTemplateId(TemplateType type)
        {
            string id;
            return _dic.TryGetValue(type, out id) ? id : null;
        }

        public static SendTemplateMessageResult Send(string authrizerAt,string toOpenId, string tempId, object data, string url, string topColor= "#E61C64")
        {
            if(string.IsNullOrEmpty(tempId))
                throw new MDException(typeof(TemplateMsgHelper),$"模板消息类型：{tempId},未注册！");
            var result = TemplateApi.SendTemplateMessage(authrizerAt, toOpenId, tempId, topColor, url, data);
            return result;
        }

        public static async Task<SendTemplateMessageResult> SendAsync(string authrizerAt, string toOpenId, string tempId, object data, string url, string topColor = "#E61C64")
        {
            if (string.IsNullOrEmpty(tempId))
                throw new MDException(typeof(TemplateMsgHelper), $"模板消息类型：{tempId},未注册！");

            var result = await MyTemplateApi.SendTemplateMessageAsync(authrizerAt, toOpenId, tempId, topColor, url, data);
            return result;
        }
    }
}
