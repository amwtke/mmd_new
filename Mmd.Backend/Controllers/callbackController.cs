using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using MD.Lib.Aliyun.OSS;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.Util.HttpUtil;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration.Aliyun;
using MD.Model.DB;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using System.Reflection;
using MD.Lib.DB.Redis;
using MD.Lib.MMBizRule.MerchantRule;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;
using MD.Model.Configuration.Redis;
using MD.Model.DB.Code;
using MD.Model.MQ;
using MD.Lib.DB.Redis.MD.ForTest;

namespace Mmd.Backend.Controllers
{
    public class callbackController : Controller
    {
        /// <summary>
        /// 公众号授权回调url:wx.mmpintuan.com/callback带上code.
        /// 这是登录与授权的入口。
        /// </summary>
        /// <param name="auth_code"></param>
        /// <param name="expires_in"></param>
        /// <returns></returns>
        //[HttpGet]
        public async Task<ActionResult> Index(string auth_code, string expires_in)
        {
            //WatchStopper ws = new WatchStopper(typeof(callbackController), "simpleObject");
            //ws.Start();
            try
            {
                var simpleObject = await WXComponentHelper.WxCallbackAsync(auth_code);
                if (simpleObject == null)
                    return new ContentResult()
                    {
                        Content = "没有取到simpleobject！"
                    };
                //如果不包含服务号或者不在白名单中，则不让通过
                if (simpleObject.ServiceType != null && !simpleObject.ServiceType.Contains("服务号") && !(await RedisForTestOp.ContainAppidAsync(simpleObject.Appid)))
                {
                    return Content($"您当前登录的账号类型不是服务号！服务号类型：{simpleObject.ServiceType}");
                }
                MDLogger.LogInfoAsync(typeof(callbackController), $"!callback的字符串为：{HttpUtility.UrlDecode(Request.QueryString.ToString())}.登录商家：{simpleObject?.NiceName},登录时间：{DateTime.Now}");
                //ws.Stop();
                //MDLogger.LogInfoAsync(typeof(callbackController),$"！sign:{sign}，simpleObject耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                //ws.Restart("其余部分");

                Merchant mer = null;
                using (BizRepository repo = new BizRepository())
                {
                    mer = await repo.GetMerchantByAppidAsync(simpleObject.Appid) ?? new Merchant();
                }

                // 0或者未注册的情况
                if (mer.mid.Equals(Guid.Empty) || mer.status == (int)ECodeMerchantStatus.待审核)
                {
                    try
                    {
                        mer = await DealStatus_0(mer, simpleObject);
                        if (mer == null)
                            return Content("状态0错误！");
                        //Session[mer.wx_appid] = mer;
                        //Session[mer.mid.ToString()] = mer;
                        SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                        SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                        SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                        SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                        //ws.Stop();
                        //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                        return RedirectToAction("register", "callback");
                    }
                    catch (Exception)
                    {
                        return Content(@"请检查公众号是否设置了头像，如果没有,请先:<a href=http://mp.weixin.qq.com>登录微信设置.</a>");
                    }

                }
                if (mer.status == (int)ECodeMerchantStatus.已开通未配置)//已通过审核，未完成配置
                {
                    //Session[mer.wx_appid] = mer;
                    //Session[mer.mid.ToString()] = mer;
                    SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                    SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                    SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                    SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                    //ws.Stop();
                    //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                    return RedirectToAction("Register_payconfig", "callback");
                }
                if (mer.status == (int)ECodeMerchantStatus.已配置)//已完成配置，跳转到mer home
                {
                    //Session[mer.wx_appid] = mer;
                    //Session[mer.mid.ToString()] = mer;
                    SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                    SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                    SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                    SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                    //ws.Stop();
                    //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                    return RedirectToAction("Merchant_Home", "Home");
                }
                if (mer.status == (int)ECodeMerchantStatus.未通过)//未通过审核，跳转到s2
                {
                    //Session[mer.wx_appid] = mer;
                    //Session[mer.mid.ToString().ToLower()] = mer;
                    SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                    SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                    SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                    SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                    //ws.Stop();
                    //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                    return RedirectToAction("register_s2");
                }

                if (mer.status == (int)ECodeMerchantStatus.审核中)//未通过审核，跳转到s2
                {
                    //Session[mer.wx_appid] = mer;
                    //Session[mer.mid.ToString().ToLower()] = mer;
                    SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                    SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                    SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                    SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                    //ws.Stop();
                    //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                    return RedirectToAction("register_s2");
                }

                if (mer.status == (int)ECodeMerchantStatus.已删除)//已删除
                {
                    return Content("您的账号已经被禁用!");
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(callbackController), ex);
                return Content("Callback Error");
            }

            //ws.Stop();
            //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

            return Content("未定义状态！");
        }

        public async Task<ActionResult> Login(string appid, string code)
        {
            if (string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(code) || !code.Equals("mmpintuan.com"))
                return Content("error!");

            Merchant mer = null;
            var simpleObject = await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<AuthorizerInfoRedis>(appid);
            if (string.IsNullOrEmpty(simpleObject.NiceName))
                return Content($"没有这个appid:{appid}");

            using (BizRepository repo = new BizRepository())
            {
                mer = await repo.GetMerchantByAppidAsync(appid) ?? new Merchant();
            }

            // 0或者未注册的情况
            if (mer.mid.Equals(Guid.Empty) || mer.status == (int)ECodeMerchantStatus.待审核)
            {
                try
                {
                    mer = await DealStatus_0(mer, simpleObject);
                    if (mer == null)
                        return Content("状态0错误！");
                    //Session[mer.wx_appid] = mer;
                    //Session[mer.mid.ToString()] = mer;
                    SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                    SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                    SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                    SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                    //ws.Stop();
                    //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                    return RedirectToAction("register", "callback");
                }
                catch (Exception ex)
                {
                    return Content(@"请检查公众号是否设置了头像，如果没有,请先:<a href=http://mp.weixin.qq.com>登录微信设置.</a>");
                }

            }
            if (mer.status == (int)ECodeMerchantStatus.已开通未配置)//已通过审核，未完成配置
            {
                //Session[mer.wx_appid] = mer;
                //Session[mer.mid.ToString()] = mer;
                SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                //ws.Stop();
                //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                return RedirectToAction("Register_payconfig", "callback");
            }
            if (mer.status == (int)ECodeMerchantStatus.已配置)//已完成配置，跳转到mer home
            {
                //Session[mer.wx_appid] = mer;
                //Session[mer.mid.ToString()] = mer;
                SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                //ws.Stop();
                //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                return RedirectToAction("Merchant_Home", "Home");
            }
            if (mer.status == (int)ECodeMerchantStatus.未通过)//未通过审核，跳转到s2
            {
                //Session[mer.wx_appid] = mer;
                //Session[mer.mid.ToString().ToLower()] = mer;
                SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                //ws.Stop();
                //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                return RedirectToAction("register_s2");
            }

            if (mer.status == (int)ECodeMerchantStatus.审核中)//未通过审核，跳转到s2
            {
                //Session[mer.wx_appid] = mer;
                //Session[mer.mid.ToString().ToLower()] = mer;
                SessionHelper.Set(this, ESessionStateKeys.AppId, mer.wx_appid);
                SessionHelper.Set(this, ESessionStateKeys.Mid, mer.mid);
                SessionHelper.Set(this, ESessionStateKeys.MerName, mer.name);
                SessionHelper.Set(this, ESessionStateKeys.MerStatus, mer.status);

                //ws.Stop();
                //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

                return RedirectToAction("register_s2");
            }

            if (mer.status == (int)ECodeMerchantStatus.已删除)//已删除
            {
                return Content("您的账号已经被禁用!");
            }
            //ws.Stop();
            //MDLogger.LogInfoAsync(typeof(callbackController), $"！sign:{sign}，其余耗时：{Watch.Elapsed.TotalSeconds} 秒！");

            return Content("未定义状态！");
        }

        #region 状态处理部分

        private async Task<Merchant> DealStatus_0(Merchant mer, AuthorizerInfoRedis simpleObject)
        {

            AuthorizerInfoRedis obj = await WXComponentHelper.GetAuthorizerInfoAsync(simpleObject.Appid);
            mer.wx_appid = obj.Appid;
            mer.name = obj.NiceName;
            mer.qr_url = obj.QrCodeUrl;
            mer.wx_mp_id = obj.UserName;
            mer.logo_url = obj.HeadImgUrl;
            if (string.IsNullOrEmpty(mer.logo_url))
                throw new MDException(typeof(callbackController),
                    new Exception("公众号没有设置头像！请登录http://mp.weixin.qq.com进行设置！"));

            #region 更新logo与qr的图片到OSS中

            try
            {

                AsyncHelper.RunAsync(async delegate ()
                {
                    byte[] logo_array = await Get.DownloadAsync(mer.logo_url);
                    using (Stream logoStream = new MemoryStream(logo_array))
                    {
                        if (logoStream.Length > 0)
                        {
                            string path_logo = OssPicPathManager<OssPicBucketConfig>.UploadMerchantLogoPic(
                                mer.wx_appid, logoStream);
                            if (!string.IsNullOrEmpty(path_logo))
                            {
                                mer.logo_url = path_logo;
                                using (var repo = new BizRepository())
                                {
                                    await repo.SaveOrUpdateRegisterMerchantAsync(mer);
                                    MDLogger.LogInfoAsync(typeof(callbackController), $"appid:{mer.wx_appid},更新了logo图片:{mer.logo_url}");
                                }
                            }
                        }
                    }
                }, null);


                AsyncHelper.RunAsync(async delegate ()
                {
                    byte[] qr_array = await Get.DownloadAsync(mer.qr_url);
                    using (Stream qrStream = new MemoryStream(qr_array))
                    {
                        if (qrStream.Length > 0)
                        {
                            string path_qr = OssPicPathManager<OssPicBucketConfig>.UploadMerchantQrPic(mer.wx_appid,
                                qrStream);
                            if (!string.IsNullOrEmpty(path_qr))
                            {
                                mer.qr_url = path_qr;
                                using (var repo = new BizRepository())
                                {
                                    await repo.SaveOrUpdateRegisterMerchantAsync(mer);
                                    MDLogger.LogInfoAsync(typeof(callbackController), $"appid:{mer.wx_appid},更新了qr图片:{mer.qr_url}");
                                }
                            }
                        }
                    }
                }, null);
                #endregion

                using (var repo = new BizRepository())
                {
                    await repo.SaveOrUpdateRegisterMerchantAsync(mer);
                    return await repo.GetMerchantByAppidAsync(mer.wx_appid);
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(callbackController), ex);
            }
            return null;
        }

        #endregion 
        /// <summary>
        /// 第一步注册的上传程序
        /// </summary>
        /// <param name="postObject"></param>
        /// <param name="uploadedFile"></param>
        /// <returns></returns>
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> Upload(Merchant postObject, HttpPostedFileBase uploadedFile)
        {
            try
            {
                Merchant mer = await SessionHelper.GetMerchant(this);
                if (mer == null)
                    return RedirectToAction("SessionTimeOut", "Session");

                if (string.IsNullOrEmpty(postObject?.wx_appid))
                    return Content("appid is null!");

                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    var path = OssPicPathManager<OssPicBucketConfig>.UploadBizLicencePic(postObject.wx_appid, uploadedFile.FileName, uploadedFile.InputStream);
                    if (!string.IsNullOrEmpty(path))
                    {
                        postObject.biz_licence_url = path;
                    }
                    else
                    {
                        throw new MDException(typeof(callbackController), "上传文件失败！");
                    }
                    postObject.biz_licence_url = path;
                }

                using (BizRepository repo = new BizRepository())
                {
                    await repo.SaveOrUpdateRegisterMerchantAsync(postObject);
                    await repo.ChangeMerchantStatus(SessionHelper.Get(this, ESessionStateKeys.AppId).ToString(), ECodeMerchantStatus.审核中);
                    return RedirectToAction("register_s2");
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(callbackController), ex);
                throw ex;
            }
        }
        /// <summary>
        /// payconfig的上传程序
        /// </summary>
        /// <param name="postObject"></param>
        /// <param name="p12"></param>
        /// <returns></returns>
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> Upload_s2(Merchant postObject, HttpPostedFileBase wx_p12_dir)
        {
            try
            {
                Merchant mer = await SessionHelper.GetMerchant(this);
                if (mer == null)
                    return RedirectToAction("SessionTimeOut", "Session");

                if (string.IsNullOrEmpty(postObject?.wx_appid))
                    return Content("appid is null!");

                if (wx_p12_dir != null && wx_p12_dir.ContentLength > 0)
                {
                    var path = WXPayHelper.GetCetDir(postObject.wx_appid);

                    if (!string.IsNullOrEmpty(path))
                    {
                        wx_p12_dir.SaveAs(path + wx_p12_dir.FileName);
                        postObject.wx_p12_dir = path + wx_p12_dir.FileName;
                    }
                    else
                    {
                        throw new MDException(typeof(callbackController), "上传文件失败！");
                    }
                    postObject.wx_p12_dir = path + wx_p12_dir.FileName;
                }

                using (BizRepository repo = new BizRepository())
                {
                    if (!string.IsNullOrEmpty(postObject.wx_mch_id))
                        postObject.wx_mch_id = postObject.wx_mch_id.Trim();
                    if (!string.IsNullOrEmpty(postObject.wx_apikey))
                        postObject.wx_apikey = postObject.wx_apikey.Trim();
                    await repo.SaveOrUpdateRegisterMerchantAsync(postObject);
                    //var dbMer = await repo.GetMerchantByAppidAsync(postObject.wx_appid);
                    //Session[postObject.wx_appid] = dbMer;
                    return RedirectToAction("Register_ending");
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(callbackController), ex);
                throw ex;
            }
        }
        /// <summary>
        /// 商家中心，展示商家基本信息，商家在此页面修改基本信息
        /// </summary>
        /// <param name="postObject"></param>
        /// <returns></returns>
        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> Merchant_change(Merchant postObject)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");

            try
            {
                if (string.IsNullOrEmpty(postObject?.wx_appid))
                    return Content("appid is null!");
                using (BizRepository repo = new BizRepository())
                {
                    await repo.SaveOrUpdateRegisterMerchantAsync(postObject);
                    await repo.ChangeMerchantStatus(postObject.wx_appid, ECodeMerchantStatus.已配置);
                    //var dbMer = await repo.GetMerchantByAppidAsync(postObject.wx_appid);
                    //Session[postObject.wx_appid] = dbMer;
                    SessionHelper.Set(this, ESessionStateKeys.MerStatus, (int)ECodeMerchantStatus.已配置);
                    return RedirectToAction("Merchant_Home", "Home");
                }

            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(callbackController), ex);
                throw ex;
            }

        }


        public async Task<ActionResult> register()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");

            if (mer != null)
            {
                mer.address = mer.address == "x" ? "" : mer.address;
                mer.cell_phone = mer.cell_phone == 13800000000 ? null : mer.cell_phone;
                mer.contact_person = mer.contact_person == "x" ? "" : mer.contact_person;
                mer.biz_licence_url = mer.biz_licence_url == "x" ? "" : mer.biz_licence_url;
                mer.service_region = mer.service_region == "x" ? "" : mer.service_region;
                //更新状态到待审核
                using (var repo = new BizRepository())
                {
                    await repo.ChangeMerchantStatus(mer.wx_appid, ECodeMerchantStatus.待审核);
                }
                return View(mer);
            }
            return RedirectToAction("SessionTimeOut", "Session");
        }

        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> register_s2()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");

            return View(mer);
        }
        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> Register_payconfig()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");

            if (mer != null)
            {
                mer.wx_mch_id = mer.wx_mch_id == "x" ? "" : mer.wx_mch_id;
                mer.wx_apikey = mer.wx_apikey == "x" ? "" : mer.wx_apikey;
                mer.wx_p12_dir = mer.wx_p12_dir == "x" ? "" : mer.wx_p12_dir;
                mer.wx_pay_dir = WXComponentHelper.GetPayDir(mer.wx_appid);
                mer.wx_jspay_dir = WXComponentHelper.GetJsDir(mer.wx_appid);
                mer.wx_biz_dir = MdWxSettingUpHelper.GenEntranceUrl(mer.wx_appid);
                //WXComponentHelper.GetBizDir(mer.wx_appid);
            }
            return View(mer);
        }
        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> Register_ending()
        {
            try
            {
                Merchant mer = await SessionHelper.GetMerchant(this);
                if (mer == null)
                    return RedirectToAction("SessionTimeOut", "Session");

                await MBizRule.BuyTaocan(mer, ECodeTaocanType.KTJS10, 1, 0);
                using (var repos = new BizRepository())
                {
                    var quota = await repos.MerchantGetBizQuota(mer.mid, EBizType.DD.ToString());
                    ViewBag.quota = quota;
                    var communityUrl = MdWxSettingUpHelper.GenCommuintryEntranceUrl(mer.wx_appid);
                    ViewBag.communityUrl = communityUrl;
                    return View(mer);
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(callbackController), ex);
            }
        }
        public async Task<ActionResult> Merchant_Config()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");

            return View(mer);
        }

        public ActionResult DownloadGuid()
        {
            Stream st = OssPicPathManager<OssPicBucketConfig>.DownloadGuidPdf();
            return File(st, "application/pdf", "mmpintuan配置指南.pdf");
            //byte[] bs = OssPicPathManager<OssPicBucketConfig>.DownloadGuidPdf2();

            //return File(bs, "application/pdf", "mmpintuan配置指南.pdf");
        }
    }
}