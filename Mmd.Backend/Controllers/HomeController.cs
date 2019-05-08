using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Lib.Log;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using Senparc.Weixin.Open;
using Senparc.Weixin.Open.Entities.Request;
using Senparc.Weixin.Open.MessageHandlers;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using MD.Model.DB.Code;
using MD.Lib.ElasticSearch;

namespace Mmd.Backend.Controllers
{
    /// <summary>
    /// wx.mmpintuan.com 用于接收微信开放平台。
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// 授权的测试页
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> Index()
        {
            ViewBag.url = await WXComponentHelper.GetAuthUrlAsync();
            return View();
        }

        /// <summary>
        /// 事件接收，微信post，wx.mmpintuan.com。包括接收ticket的通知消息。
        /// 还有关注，取消关注等通知消息。
        /// </summary>
        /// <param name="postModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("Index")]
        public ActionResult Post(PostModel postModel)
        {
            try
            {
                //var openConfig = MD.Configuration.MdConfigurationManager.GetConfig<WXOpenConfig>();
                //if (openConfig == null)
                //{
                //    MDLogger.LogError(typeof(HomeController), new Exception("没有取到配置信息！"));
                //    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                //}
                if (String.IsNullOrEmpty(postModel?.Msg_Signature) || string.IsNullOrEmpty(postModel.Nonce) || string.IsNullOrEmpty(postModel.Timestamp))
                {
                    ActionResult result = new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    MDLogger.LogError(typeof(HomeController), new Exception("postModel没取到"));
                    return result;
                }
                else
                {
                    postModel.AppId = "wx323abc83f8c7e444";// openConfig.AppId;
                    postModel.Token = "open_mmpintuan"; //openConfig.Token;
                    postModel.EncodingAESKey = "VHINhw6MAt3fIZhoVYRx5ESqwhRuNGuJC2ixnSAW9Wz"; //openConfig.EncodingAESKey;

                    MDLogger.LogInfo(typeof(HomeController), "取到一个Tiket：" + $"appid:{postModel.AppId}");
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
                MDLogger.LogError(typeof(HomeController), ex);
                return new ContentResult()
                {
                    Content = "success",
                    ContentEncoding = Encoding.UTF8
                };
                //throw new MDException(typeof(HomeController), ex);
            }
        }

        public async Task<ActionResult> Merchant_Home(string strtime)
        {
            var mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Redirect("http://www.mmpintuan.com");
            double f = 0, t = 0;
            DateTime from, to;
            if (string.IsNullOrEmpty(strtime))
            {
                //默认今天
                from = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));
                to = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd")).AddDays(1);
                f = CommonHelper.ToUnixTime(from);
                t = CommonHelper.ToUnixTime(to);
            }
            else
            {
                from = Convert.ToDateTime(strtime.Substring(0, 10));
                to = Convert.ToDateTime(strtime.Substring(strtime.Length - 10, 10)).AddDays(1);
                f = CommonHelper.ToUnixTime(from);
                t = CommonHelper.ToUnixTime(to);
            }
            ViewBag.seachDate = strtime;
            await getTotal(mer.mid, f, t);
            return View(mer);
        }
        public async Task<int> getTotal(Guid mid, double f, double t)
        {
            using (var reop = new BizRepository())
            {
                int kaituanTotal = await reop.GetCountByMidAsync(mid, new List<int>() { 0, 1, 2 }, f, t);
                ViewBag.kaituanTotal = kaituanTotal;//开团总数
                int chengtuanTotal = await reop.GetCountByMidAsync(mid, new List<int>() { (int)EGroupOrderStatus.拼团成功 }, f, t);
                ViewBag.chengtuanTotal = chengtuanTotal;//成团数
                int orderOkTotal = await EsOrderManager.GetOrderCountAsync(mid, new List<int>() { (int)EOrderStatus.已成团未提货, (int)EOrderStatus.拼团成功 }, f, t);
                ViewBag.orderOkTotal = orderOkTotal;//成交订单总数
                var tuple = await EsOrderManager.GetOrderCountAndAmountAsync(mid, new List<int>() { (int)EOrderStatus.已成团未提货, (int)EOrderStatus.拼团成功 }, f, t);
                ViewBag.chegnjiaoTotal = tuple.Item2;//成交总金额
                int hexiaoTotal= await EsOrderManager.GetOrderCountAsync(mid, new List<int>() {(int)EOrderStatus.拼团成功 }, f, t);
                ViewBag.hexiaoTotal = hexiaoTotal;//核销总数
                var tuplelll = EsBizLogStatistics.SearchBizView(ELogBizModuleType.MidView,mid,Guid.Empty,f,t);
                ViewBag.lll = tuplelll.Item1;//浏览量
            }
            return 1;
        }
    }

    public class MDOpenCustomMessageHandler : ThirdPartyMessageHandler
    {
        public MDOpenCustomMessageHandler(Stream inputStream, PostModel postModel = null)
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
}