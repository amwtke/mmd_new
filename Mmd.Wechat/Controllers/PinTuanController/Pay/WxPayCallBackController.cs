using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Lib.Log;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Pay;
using MD.Model.MQ;
using MD.Wechat.Controllers.PinTuanController.Pay;
using Senparc.Weixin.MP.TenPayLibV3;
namespace MD.Wechat.Controllers.PinTuanController.Pay
{
    public class WxPayCallBackController : Controller
    {
        // 微信支付的支付结果回调,POST.
        // pay.mmpintuan.com
        public async Task<ActionResult> Index()
        {
            //MDLogger.LogBizAsync(typeof(WxPayCallBackController),new BizMQ("微信支付回调", "", Guid.NewGuid(), "获取了一个回调"));

            ResponseHandler resHandler = new ResponseHandler(null);

            //设置支付key
            //resHandler.SetKey(TestPayConfig.Key);
            if (!await MdWxPayUtil.SetMidKeyAsync(resHandler))
            {
                throw new MDException(typeof(WxPayCallBackController), $"获取商家的支付key失败！appid:{resHandler.GetParameter("appid")}");
            }

            //验证请求是否从微信发过来（安全）
            if (resHandler.IsTenpaySign())
            {
                //正确的订单处理

                //记录es日志
                MdWxPayUtil.Log(resHandler);

                //回调处理

                try
                {
                    //MDLogger.LogInfoAsync(typeof(WxPayCallBackController),$"我来了！时间：{DateTime.Now}");
                    await MdWxPayUtil.PayCallbackProcess(resHandler);
                }
                catch (Exception ex)
                {
                    MDLogger.LogInfoAsync(typeof(WxPayCallBackController), $"支付处理有异常！时间：{DateTime.Now}");
                    //退款流程。
                    MdWxPayUtil.RefundAsync(resHandler, null);
                    MDLogger.LogErrorAsync(typeof(WxPayCallBackController),ex);
                }


                //回复
                string xml = MdWxPayUtil.StandardResponse("SUCCESS", "OK");
                //MDLogger.LogInfoAsync(typeof(WxPayCallBackController), $"回复给微信服务器！{xml}.时间：{DateTime.Now}");
                return Content(xml, "text/xml");
            }

            string error = MdWxPayUtil.StandardResponse("FAILED", "WRONG");
            MDLogger.LogInfoAsync(typeof(WxPayCallBackController), $"回复Error给微信服务器！{error}.时间：{DateTime.Now}");
            return Content(error, "text/xml");
        }
    }
}