using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Redis.MD;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.Message;
using MD.Lib.Weixin.Message.TemplateMessageObjects;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.Configuration.Redis;
using MD.Model.DB;
using MD.Model.MQ.MD;
using Senparc.Weixin.MP.Entities;
using MD.Model.DB.Activity;

namespace MD.Lib.MQ.MD
{
    public static class MqWxTempMsgManager
    {
        static MqWxTempMsgManager()
        {
            try
            {
                MQManager.Prepare_P_MQ<MqWxTempMsgConfig>();
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MqEmailManager), ex);
            }
        }

        private static string GetOrderUrl(string appid, string oid)
        {
            return MdWxSettingUpHelper.GenOrderDetailUrl(appid, Guid.Parse(oid));
        }

        private static string GetGroupUrl(string appid, Guid goid)
        {
            return MdWxSettingUpHelper.GenGoDetailUrl(appid, goid);
        }

        private static string GetGroupDetailUrl(string appid,Guid gid)
        {
            return MdWxSettingUpHelper.GenGroupDetailUrl(appid, gid);
        }
        private static string GenLadderGoDetailUrl(string appid, Guid goid)
        {
            return MdWxSettingUpHelper.GenLadderGoDetailUrl(appid, goid);
        }

        public static async Task<MqWxTempMsgObject> GenFromPtSucess(Order order)
        {
            if (order == null)
                return null;

            var indexGo = await EsGroupOrderManager.GetByIdAsync(order.goid);
            if (indexGo == null)
                return null;

            var indexGroup = await EsGroupManager.GetByGidAsync(Guid.Parse(indexGo.gid));
            if (indexGroup == null)
                return null;
            var mer = await RedisMerchantOp.GetByMidAsync(Guid.Parse(indexGroup.mid));
            if (string.IsNullOrEmpty(mer?.wx_appid))
                return null;
            var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(mer.wx_appid);

            var indexUserLeader = await EsUserManager.GetByIdAsync(Guid.Parse(indexGo.leader));
            if (indexUserLeader == null)
                return null;

            var indexUser = await EsUserManager.GetByIdAsync(order.buyer);
            if (indexUser == null)
                return null;

            var indexProduct = await EsProductManager.GetByPidAsync(Guid.Parse(indexGo.pid));
            if (indexProduct == null)
                return null;

            string shortId = TemplateMsgHelper.GetShortId(TemplateType.PTSuccess);
            MessageBase first = new MessageBase("您好，您有新的拼团成功订单", "#173177");
            MessageBase remark = new MessageBase("恭喜您拼团成功，点击查看订单详情！");
            MessageBase ky1 = new MessageBase(indexProduct.name, "#173177");
            MessageBase ky2 = new MessageBase(indexUserLeader.name, "#173177");
            MessageBase ky3 = new MessageBase(indexGroup.person_quota.ToString(), "#173177");

            PtSuccessObject obj = new PtSuccessObject(first,remark,ky1,ky2,ky3);

            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = mer.wx_appid,
                At = authorizerAccessToken,
                Data = obj,
                ShortId = shortId,
                ToOpenId = indexUser.openid,
                TopColor= TemplateMsgHelper.DefaultColor,
                Url= GetOrderUrl(mer.wx_appid,order.oid.ToString())
            };
            return retObject;
        }

        /// <summary>
        /// 阶梯团拼团成功后的模板消息
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public static async Task<MqWxTempMsgObject> GenFromLadderGroupPtSucess(LadderOrder order)
        {
            if (order == null)
                return null;

            var indexGo = await EsLadderGroupOrderManager.GetByIdAsync(order.goid);
            if (indexGo == null)
                return null;

            var indexGroup = await EsLadderGroupManager.GetByGidAsync(order.gid);
            if (indexGroup == null)
                return null;
            var mer = await RedisMerchantOp.GetByMidAsync(Guid.Parse(indexGroup.mid));
            if (string.IsNullOrEmpty(mer?.wx_appid))
                return null;
            var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(mer.wx_appid);

            var indexUserLeader = await EsUserManager.GetByIdAsync(Guid.Parse(indexGo.leader));
            if (indexUserLeader == null)
                return null;

            var indexUser = await EsUserManager.GetByIdAsync(order.buyer);
            if (indexUser == null)
                return null;

            var indexProduct = await EsProductManager.GetByPidAsync(Guid.Parse(indexGo.pid));
            if (indexProduct == null)
                return null;

            int orderCount = await EsLadderOrderManager.GetOrderCountByGoidAsync(order.goid);
            string shortId = TemplateMsgHelper.GetShortId(TemplateType.PTSuccess);
            MessageBase first = new MessageBase("您好，您有新的拼团成功订单", "#173177");
            MessageBase remark = new MessageBase("恭喜您拼团成功，点击查看订单详情！");
            MessageBase ky1 = new MessageBase(indexProduct.name, "#173177");
            MessageBase ky2 = new MessageBase(indexUserLeader.name, "#173177");
            MessageBase ky3 = new MessageBase(orderCount.ToString(), "#173177");

            PtSuccessObject obj = new PtSuccessObject(first, remark, ky1, ky2, ky3);

            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = mer.wx_appid,
                At = authorizerAccessToken,
                Data = obj,
                ShortId = shortId,
                ToOpenId = indexUser.openid,
                TopColor = TemplateMsgHelper.DefaultColor,
                Url = GenLadderGoDetailUrl(mer.wx_appid, order.goid)
            };
            return retObject;
        }

        public static async Task<MqWxTempMsgObject> GenFromPtFail(Order order)
        {
            if (order == null)
                return null;
            var indexGo = await EsGroupOrderManager.GetByIdAsync(order.goid);
            if (indexGo == null)
                return null;

            var indexGroup = await EsGroupManager.GetByGidAsync(Guid.Parse(indexGo.gid));
            if (indexGroup == null)
                return null;
            var mer = await RedisMerchantOp.GetByMidAsync(Guid.Parse(indexGroup.mid));
            if (string.IsNullOrEmpty(mer?.wx_appid))
                return null;
            var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(mer.wx_appid);

            var indexUser = await EsUserManager.GetByIdAsync(order.buyer);
            if (indexUser == null)
                return null;
            var indexProduct = await EsProductManager.GetByPidAsync(Guid.Parse(indexGo.pid));
            if (indexProduct == null)
                return null;

            string shortId = TemplateMsgHelper.GetShortId(TemplateType.PTFail);
            MessageBase first = new MessageBase("您好，您参加的拼团未在规定时间内达到成团人数，拼团失败。", "#173177");
            MessageBase remark = new MessageBase("您的退款已经提交微信审核，系统将自动原路退款，感谢您的参与！");
            MessageBase ky1 = new MessageBase(indexProduct.name, "#173177");
            MessageBase ky2 = new MessageBase("￥"+(float)order.order_price/100, "#173177");
            MessageBase ky3 = new MessageBase("￥"+(float)order.actual_pay/100, "#173177");

            PtFailObject obj = new PtFailObject(first, remark, ky1, ky2, ky3);

            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = mer.wx_appid,
                At = authorizerAccessToken,
                Data = obj,
                ShortId = shortId,
                ToOpenId = indexUser.openid,
                TopColor = TemplateMsgHelper.DefaultColor,
                Url = GetOrderUrl(mer.wx_appid, order.oid.ToString())
            };
            return retObject;
        }

        public static async Task<MqWxTempMsgObject> GenFromPaySuccessAsync(Order order,string wx_appid,string openid)
        {
            if (order == null)
                return null;
            var indexGroup = await EsGroupManager.GetByGidAsync(order.gid);
            if (indexGroup == null)
                return null;
            var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(wx_appid);
            string shortId = TemplateMsgHelper.GetShortId(TemplateType.PaySuccess);
            MessageBase first = new MessageBase("恭喜您支付成功！", "#173177");
            MessageBase orderMoneySum = new MessageBase(((float)order.order_price / 100).ToString() + "元", "#173177");
            MessageBase orderProductName = new MessageBase(indexGroup.title, "#173177");
            MessageBase Remark = new MessageBase("24小时内参团人数不足将自动退款，点击邀请闺蜜来参团！");
            PaySuccessObject obj = new PaySuccessObject(first,Remark,orderMoneySum,orderProductName);
            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = wx_appid,
                At = authorizerAccessToken,
                Data = obj,
                ShortId = shortId,
                ToOpenId = openid,
                TopColor = TemplateMsgHelper.DefaultColor,
                Url = GetGroupUrl(wx_appid, order.goid)
            };
            return retObject;
        }
        public static MqWxTempMsgObject GenFromPaySuccess(Order order, string wx_appid, string openid)
        {
            if (order == null)
                return null;
            var indexGroup = EsGroupManager.GetByGid(order.gid);
            if (indexGroup == null)
                return null;
            var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(wx_appid);
            string shortId = TemplateMsgHelper.GetShortId(TemplateType.PaySuccess);
            MessageBase first = new MessageBase("恭喜您支付成功！", "#173177");
            MessageBase orderMoneySum = new MessageBase(((float)order.order_price / 100).ToString() + "元", "#173177");
            MessageBase orderProductName = new MessageBase(indexGroup.title, "#173177");
            MessageBase Remark = new MessageBase("24小时内参团人数不足将自动退款，点击邀请闺蜜来参团！");
            PaySuccessObject obj = new PaySuccessObject(first, Remark, orderMoneySum, orderProductName);
            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = wx_appid,
                At = authorizerAccessToken,
                Data = obj,
                ShortId = shortId,
                ToOpenId = openid,
                TopColor = TemplateMsgHelper.DefaultColor,
                Url = GetGroupUrl(wx_appid, order.goid)
            };
            return retObject;
        }

        public static MqWxTempMsgObject GenGroupRemind(string wx_appid, string openid,Guid goid,string productName,int timeLeft, int userLeft )
        {
            if (string.IsNullOrEmpty(wx_appid)) return null;
            var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(wx_appid);
            string shortId = TemplateMsgHelper.GetShortId(TemplateType.PTRemind);
            MessageBase first = new MessageBase("您的拼团还有" + timeLeft + "小时就要到期了，还差" + userLeft + "个人哦，快去叫上闺蜜一起拼吧！", "#173177");
            MessageBase keyword1 = new MessageBase(productName, "#173177");
            MessageBase keyword2 = new MessageBase(timeLeft + "小时", "#173177");
            MessageBase keyword3 = new MessageBase(userLeft + "人", "#173177");
            MessageBase Remark = new MessageBase("点击此处邀请闺蜜来参团！");
            PtFailObject obj = new PtFailObject(first, Remark, keyword1, keyword2,keyword3);
            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = wx_appid,
                At = authorizerAccessToken,
                Data = obj,
                ShortId = shortId,
                ToOpenId = openid,
                TopColor = TemplateMsgHelper.DefaultColor,
                Url = GetGroupUrl(wx_appid, goid)
            };
            return retObject;
        }

        public static MqWxTempMsgObject GenLotteryResult(string wx_appid, string openid,Guid oid,string groupName,string productName,bool isLucky)
        {
            if (string.IsNullOrEmpty(wx_appid)) return null;
            var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(wx_appid);
            string shortId = TemplateMsgHelper.GetShortId(TemplateType.LotteryResult);
            string message = isLucky ? "恭喜您参与的活动中奖了！" : "很遗憾您没有中奖";
            MessageBase first = new MessageBase(message);
            MessageBase keyword1 = new MessageBase(groupName, "#173177");
            MessageBase keyword2 = new MessageBase(productName, "#173177");
            MessageBase remark = new MessageBase("感谢您的参与", "#173177");
            PtSuccessObject obj = new PtSuccessObject(first, remark, keyword1, keyword2, null);
            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = wx_appid,
                At = authorizerAccessToken,
                Data = obj,
                ShortId = shortId,
                ToOpenId = openid,
                TopColor = TemplateMsgHelper.DefaultColor,
                Url = GetOrderUrl(wx_appid, oid.ToString())
            };
            return retObject;
        }

        /// <summary>
        /// 活动模板消息
        /// </summary>
        /// <param name="wx_appid"></param>
        /// <param name="openid"></param>
        /// <param name="title">模板消息标题</param>
        /// <param name="actName">活动名称</param>
        /// <param name="actTreasureName">奖品名称</param>
        /// <param name="url">模板消息的链接</param>
        /// <returns></returns>
        public static MqWxTempMsgObject GenActivityMessage(string wx_appid, string openid,string title,string actName,string actTreasureName,string url)
        {
            if (string.IsNullOrEmpty(wx_appid)) return null;
            var authorizerAccessToken = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(wx_appid);
            string shortId = TemplateMsgHelper.GetShortId(TemplateType.LotteryResult);
            MessageBase first = new MessageBase(title);
            MessageBase keyword1 = new MessageBase(actName, "#173177");
            MessageBase keyword2 = new MessageBase(actTreasureName, "#173177");
            MessageBase remark = new MessageBase("宝贝数量有限，先到先得，领完为止！", "#173177");
            PtSuccessObject obj = new PtSuccessObject(first, remark, keyword1, keyword2, null);
            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = wx_appid,
                At = authorizerAccessToken,
                Data = obj,
                ShortId = shortId,
                ToOpenId = openid,
                TopColor = TemplateMsgHelper.DefaultColor,
                Url = url
            };
            return retObject;
        }

        public static MqWxTempMsgObject GenNewsObject(string wx_appid,string openid,string id)
        {
            string title = "欢迎进入美美社区，点击继续购买";
            string url = "", description = "", picurl = "";
            string[] str = id.Split(',');
            if (str.Length == 2)
            {
                switch (str[0])
                {
                    case "kt"://开团的链接
                        Guid gid = Guid.Parse(str[1]);
                        #region 此处也会报异常:现在无法开始异步操作。url改成下面的拼接方法；
                        //url = GetGroupDetailUrl(wx_appid, gid);
                        //现在无法开始异步操作。异步操作只能在异步处理程序或模块中开始，或在页生存期中的特定事件过程中开始。如果此异常在执行 Page 时发生，请确保 Page 标记为 <%@ Page Async=\"true\" %>。此异常也可能表明试图调用“异步无效”方法，在 ASP.NET 请求处理内一般不支持这种方法。相反，该异步方法应该返回一个任务，而调用方应该等待该任务。
                        #endregion
                        url = "http://" + wx_appid + ".wx.mmpintuan.com/mmpt/app/#/app/gokt/one/" + gid + "&" + openid;
                        var indexGroup = EsGroupManager.GetByGid(gid);
                        if (indexGroup != null)
                        {
                            description = indexGroup.title;
                            picurl = indexGroup.advertise_pic_url;
                        }
                        break;
                    case "ct"://参团的链接
                        Guid goid = Guid.Parse(str[1]);
                        //url = GetGroupUrl(wx_appid, goid);
                        url = "http://" + wx_appid + ".wx.mmpintuan.com/mmpt/app/#/app/gokt/successOpengroup/" + goid + "&" + openid;
                        var groupOrder = EsGroupOrderManager.GetById(goid);
                        if (groupOrder != null)
                        {
                            var group = EsGroupManager.GetByGid(Guid.Parse(groupOrder.gid));
                            if (group != null)
                            {
                                description = group.title;
                                picurl = group.advertise_pic_url;
                            }
                        }
                        break;
                    case "ladderkt":
                        Guid ladderGid = Guid.Parse(str[1]);
                        url = "http://" + wx_appid + ".wx.mmpintuan.com/mmpt/app/#/app/toolbox/grdetails/" + ladderGid + "&" + openid;
                        var ladderGroup = EsLadderGroupManager.GetByGid(ladderGid);
                        if (ladderGroup != null)
                        {
                            description = ladderGroup.title;
                            picurl = ladderGroup.pic;
                        }
                        break;
                    case "ladderct":
                        Guid ladderGoid = Guid.Parse(str[1]);
                        url = "http://" + wx_appid + ".wx.mmpintuan.com/mmpt/app/#/app/toolbox/success/" + ladderGoid + "&" + openid;
                        var ladderGroupOrder = EsLadderGroupOrderManager.GetById(ladderGoid);
                        if (ladderGroupOrder != null)
                        {
                            var ladderGroup2 = EsLadderGroupManager.GetByGid(Guid.Parse(ladderGroupOrder.gid));
                            if (ladderGroup2 != null)
                            {
                                description = ladderGroup2.title;
                                picurl = ladderGroup2.pic;
                            }
                        }
                        break;
                    default:
                        return null;
                }
            }
            List<Article> list = new List<Article>();
            list.Add(new Article() { Title = title, Description = description, Url = url, PicUrl = picurl });
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(wx_appid);
            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = wx_appid,
                At = at,
                Data = list,
                ShortId = TemplateType.CustomerNews.ToString(),
                ToOpenId = openid
            };
            return retObject;
        }

        public static MqWxTempMsgObject GenGroupNewsObject(string wx_appid,string at,string openid,string title,string url,string description,string picurl)
        {
            //string title = "",url = "", description = "", picurl = "";
            #region 此处也会报异常:现在无法开始异步操作。url改成下面的拼接方法；
            ////url = GetGroupDetailUrl(wx_appid, gid);
            ////现在无法开始异步操作。异步操作只能在异步处理程序或模块中开始，或在页生存期中的特定事件过程中开始。如果此异常在执行 Page 时发生，请确保 Page 标记为 <%@ Page Async=\"true\" %>。此异常也可能表明试图调用“异步无效”方法，在 ASP.NET 请求处理内一般不支持这种方法。相反，该异步方法应该返回一个任务，而调用方应该等待该任务。
            #endregion
            //url = "http://" + wx_appid + ".wx.mmpintuan.com/mmpt/app/#/app/gokt/one/" + gid + "&" + openid;
            //var indexGroup = EsGroupManager.GetByGid(gid);
            //if (indexGroup != null)
            //{
            //    title = indexGroup.title;
            //    description = indexGroup.description;
            //    picurl = indexGroup.advertise_pic_url;
            //}
            List<Article> list = new List<Article>();
            list.Add(new Article() { Title = title, Description = description, Url = url, PicUrl = picurl });
            MqWxTempMsgObject retObject = new MqWxTempMsgObject()
            {
                Appid = wx_appid,
                At = at,
                Data = list,
                ShortId = TemplateType.CustomerNews.ToString(),
                ToOpenId = openid
            };
            return retObject;
        }

        public static bool SendMessage(MqWxTempMsgObject obj)
        {
            if (obj != null)
                return MQManager.SendMQ_TB<MqWxTempMsgConfig>(obj);
            return false;
        }

        public static async Task<bool> SendMessageAsync(MqWxTempMsgObject obj)
        {
            if (obj != null)
                return await MQManager.SendMQ<MqWxTempMsgConfig>(obj);
            return false;
        }

        public static async Task<bool> SendMessageAsync(Order order,TemplateType type)
        {
            Object obj = null;
            if (type.Equals(TemplateType.PTFail))
                obj = await GenFromPtFail(order);

            if (type.Equals(TemplateType.PTSuccess))
                obj = await GenFromPtSucess(order);

            if (obj != null)
                return await MQManager.SendMQ<MqWxTempMsgConfig>(obj);
            return false;
        }
    }
}
