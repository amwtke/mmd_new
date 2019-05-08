using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using MD.Lib.DB.Redis.MD.ForTest;
using MD.Lib.DB.Redis.MD.WxStatistics;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using Senparc.Weixin.Exceptions;

namespace MD.Wechat.Controllers.PinTuanController
{
    public class GroupController : Controller
    {
        // GET: PinTuan
        public ActionResult Index(string appid)
        {
            return Content($"欢迎进入拼团！您的appid:{appid}.");
        }

        ////http://appid.wx.mmpintuan.com/group/test/2222?key1=%E6%83%A0%E6%99%AEHP-ENVY-13-D104TU-%E7%AC%94%E8%AE%B0%E6%9C%AC%E7%94%B5%E8%84%91-skylake-i7-8G-256GSSD-QHD-B-O-%E9%9F%B3%E6%95%88-%E9%87%91%E5%B1%9E%E6%9C%BA%E8%BA%AB-%E5%85%A8%E8%A6%86%E7%9B%96%E6%98%BE%E7%A4%BA%E5%B1%8F&key2=key2&key3=key3
        ////out:appid:appid,id:2222,key1:惠普HP-ENVY-13-D104TU-笔记本电脑-skylake-i7-8G-256GSSD-QHD-B-O-音效-金属机身-全覆盖显示屏,key2:key2,key3:key3
        //public ActionResult Test(string appid, int id, string key1, string key2, string key3)
        //{
        //    return new ContentResult()
        //    {
        //        Content = $"appid:{appid},id:{id},key1:{key1},key2:{key2},key3:{key3}"
        //    };
        //}


        public async Task<ActionResult> entrance(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }
                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);


                Response.Headers.Add("openid", tuple.Item1.Openid);
                Response.Headers.Add("appid", appid);
                Response.Headers.Add("mid", tuple.Item2.mid.ToString());
                Response.Headers.Add("uid", tuple.Item2.uid.ToString());

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController), $"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/index/" + tuple.Item1.Openid);
                }

                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(@"~/mmpt/app/#/app/index/" + tuple.Item1.Openid);
            }
            catch (Exception ex)
            {
                //MDLogger.LogErrorAsync(typeof(GroupController),ex);
                await WxStatisticsOp.AddExceptionCount();
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenEntranceUrl(appid));
            }
        }
        public async Task<ActionResult> communitryentrance(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }
                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"communitryentrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);


                Response.Headers.Add("openid", tuple.Item1.Openid);
                Response.Headers.Add("appid", appid);
                Response.Headers.Add("mid", tuple.Item2.mid.ToString());
                Response.Headers.Add("uid", tuple.Item2.uid.ToString());

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController), $"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/cosmetics/dynamic/" + tuple.Item1.Openid);
                }

                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(@"~/mmpt/app/#/app/cosmetics/dynamic/" + tuple.Item1.Openid);
            }
            catch (Exception ex)
            {
                //MDLogger.LogErrorAsync(typeof(GroupController),ex);
                await WxStatisticsOp.AddExceptionCount();
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenCommuintryEntranceUrl(appid));
            }
        }

        public async Task<ActionResult> godetail(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["goid"].Value = bizid;
                Response.Cookies["goid"].Expires = DateTime.Now.AddSeconds(1200);

                //Response.Headers.Add("openid", tuple.Item1.Openid);
                //Response.Headers.Add("appid", appid);
                //Response.Headers.Add("mid", tuple.Item2.mid.ToString());
                //Response.Headers.Add("uid", tuple.Item2.uid.ToString());
                //Response.Headers.Add("goid", bizid);
                //MDLogger.LogInfoAsync(typeof(GroupController),$"goid:{bizid}");

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/gokt/successOpengroup/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/gokt/successOpengroup/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController),$"getgodetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(url);
            }

            catch (Exception ex)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenGoDetailUrl(appid, Guid.Parse(bizid)));
            }
        }
        public async Task<ActionResult> godetail_fx(string appid, string bizid, string shareopenid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["shareopenid"].Value = shareopenid;
                Response.Cookies["shareopenid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["goid"].Value = bizid;
                Response.Cookies["goid"].Expires = DateTime.Now.AddSeconds(1200);

                //Response.Headers.Add("openid", tuple.Item1.Openid);
                //Response.Headers.Add("appid", appid);
                //Response.Headers.Add("mid", tuple.Item2.mid.ToString());
                //Response.Headers.Add("uid", tuple.Item2.uid.ToString());
                //Response.Headers.Add("goid", bizid);
                //MDLogger.LogInfoAsync(typeof(GroupController),$"goid:{bizid}");

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/gokt/successOpengroup/" + bizid + "&" + tuple.Item1.Openid + "&" + shareopenid);
                }

                string url = @"~/mmpt/app/#/app/gokt/successOpengroup/" + bizid + "&" + tuple.Item1.Openid + "&" + shareopenid;
                //MDLogger.LogInfoAsync(typeof(GroupController),$"getgodetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(url);
            }

            catch (Exception ex)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenGoDetailUrl_fx(appid, Guid.Parse(bizid), shareopenid));
            }
        }

        #region 商品分享
        public async Task<ActionResult> productlist(string appid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController), $"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/product/list/" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/product/list/" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"orderdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(url);
            }
            catch (Exception ex)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenProductListUrl(appid));
            }
        }

        public async Task<ActionResult> productdetail(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["pid"].Value = bizid;
                Response.Cookies["pid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController), $"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/product/details/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/product/details/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"orderdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(url);
            }

            catch (Exception ex)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenProductDetailUrl(appid, Guid.Parse(bizid)));
            }
        }
        #endregion

        public async Task<ActionResult> orderdetail(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["oid"].Value = bizid;
                Response.Cookies["oid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController), $"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/myOrder/orderStatus/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/myOrder/orderStatus/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"orderdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(url);
            }

            catch (Exception ex)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenOrderDetailUrl(appid, Guid.Parse(bizid)));
            }
        }
        #region 团详情转发链接
        public async Task<ActionResult> groupdetail_fx(string appid, string bizid, string shareopenid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["shareopenid"].Value = shareopenid;
                Response.Cookies["shareopenid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["gid"].Value = bizid;
                Response.Cookies["gid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    return Redirect(@"~/mmpt_test/app/#/app/gokt/one/" + bizid + "&" + tuple.Item1.Openid + "&" + shareopenid);
                }

                string url = @"~/mmpt/app/#/app/gokt/one/" + bizid + "&" + tuple.Item1.Openid + "&" + shareopenid;
                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(url);
            }

            catch (Exception ex)
            {
                await WxStatisticsOp.AddExceptionCount();
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenGroupDetailUrl_fx(appid, Guid.Parse(bizid), shareopenid));
            }
        }
        public async Task<ActionResult> groupdetail(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["gid"].Value = bizid;
                Response.Cookies["gid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/gokt/one/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/gokt/one/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                //EsBizLogStatistics.AddGidBizViewLog(bizid, tuple.Item1.Openid, tuple.Item2.uid);
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(url);
            }

            catch (Exception ex)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenGroupDetailUrl(appid, Guid.Parse(bizid)));
            }
        }
        #endregion

        /// <summary>
        /// 开团的微信回调页面。从用户授权回调过来。
        ///匹配{appid}.wx.mmpintuan.com/{controller}/{action}/{id}《===》 http://{appid}.wx.mmpintuan.com/group/kt/id
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="bizid"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> kt(string appid, string bizid, string code, string state)
        {
            try
            {
                var u = await MdUserHelper.SaveUserFromCallback(appid, code);
                if (u == null)
                {
                    return Content($"kt的用户授权失败！-----code:{code},appid:{appid}");
                }
                //放入cookie
                Response.Cookies["openid"].Value = u.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["ktid"].Value = bizid;
                Response.Cookies["ktid"].Expires = DateTime.Now.AddYears(1);

                return Redirect(@"~/f2e/welcom.html");

            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(GroupController), ex);
                return Content($"Error!-----{ex.ToString()}");
            }
        }

        public async Task<ActionResult> noticedetail(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["nid"].Value = bizid;
                Response.Cookies["nid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/cosmetics/page/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/cosmetics/page/" + bizid + "&" + tuple.Item1.Openid;
                await WxStatisticsOp.AddTotalLoginCount();
                EsBizLogStatistics.AddMidBizViewLog(tuple.Item2.mid.ToString(), tuple.Item1.Openid, tuple.Item2.uid);
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenNoticeDetailUrl(appid, Guid.Parse(bizid)));
            }
        }

        public async Task<ActionResult> grouppreview(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["gid"].Value = bizid;
                Response.Cookies["gid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/gokt/preview/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/gokt/preview/" + bizid + "&" + tuple.Item1.Openid;
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenGroupPreviewUrl(appid, Guid.Parse(bizid)));
            }
        }

        public async Task<ActionResult> gotoolbox(string appid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/toolbox/xb/" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/toolbox/xb/" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenFindBoxUrl(appid));
            }
        }

        public async Task<ActionResult> gotoolboxsign(string appid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/toolbox/sign/" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/toolbox/sign/" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenSignUrl(appid));
            }
        }

        public async Task<ActionResult> communityMyCenter(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["to_uid"].Value = bizid;
                Response.Cookies["to_uid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/cosmetics/zone/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/cosmetics/zone/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenCommunityMyCenterUrl(appid, Guid.Parse(bizid)));
            }
        }

        public async Task<ActionResult> communitydetail(string appid,string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["cid"].Value = bizid;
                Response.Cookies["cid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/cosmetics/pldetai/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/cosmetics/pldetai/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenCommunityDetailUrl(appid, Guid.Parse(bizid)));
            }
        }

        public async Task<ActionResult> laddergrouplist(string appid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/toolbox/group/" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/toolbox/group/" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenLadderGroupListUrl(appid));
            }
        }

        public async Task<ActionResult> laddergroupdetail(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["gid"].Value = bizid;
                Response.Cookies["gid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/toolbox/grdetails/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/toolbox/grdetails/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenLadderGroupDetailUrl(appid, Guid.Parse(bizid)));
            }
        }

        public async Task<ActionResult> laddergrouporderdetail(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["goid"].Value = bizid;
                Response.Cookies["goid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/toolbox/success/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/toolbox/success/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception)
            {
                await WxStatisticsOp.AddExceptionCount();
                //MDLogger.LogErrorAsync(typeof(GroupController), ex);
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenLadderGoDetailUrl(appid, Guid.Parse(bizid)));
            }
        }

        public async Task<ActionResult> laddergrouporderwriteoff(string appid, string bizid, string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"entrance的用户授权失败！-----code:{code},appid:{appid}");

                //放入cookie
                Response.Cookies.Clear();
                Response.Cookies["openid"].Value = tuple.Item1.Openid;
                Response.Cookies["openid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["appid"].Value = appid;
                Response.Cookies["appid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["mid"].Value = tuple.Item2.mid.ToString();
                Response.Cookies["mid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["uid"].Value = tuple.Item2.uid.ToString();
                Response.Cookies["uid"].Expires = DateTime.Now.AddYears(1);

                Response.Cookies["oid"].Value = bizid;
                Response.Cookies["oid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    return Redirect(@"~/mmpt_test/app/#/app/toolbox/cancell/" + bizid + "&" + tuple.Item1.Openid);
                }
                string url = @"~/mmpt/app/#/app/toolbox/cancell/" + bizid + "&" + tuple.Item1.Openid;
                return Redirect(url);
            }
            catch (Exception)
            {
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenLadderWriteOffUrl(appid, Guid.Parse(bizid)));
            }
        }
        public ActionResult supplydetail(string bizid)
        {
            try
            {
                string url = @"~/cg/app/#/app/details/" + bizid;
                return Redirect(url);
            }
            catch (Exception)
            {
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenSupplyDetailUrl(Guid.Parse(bizid)));
            }
        }


        /// <summary>
        /// 跳转到阶梯团详细页
        /// </summary>
        /// <param name="_a">appid</param>
        /// <param name="_i">gid</param>
        /// <returns></returns>
        public ActionResult GoLadderDetail(string _a, Guid _i)
        {
            var url = MdWxSettingUpHelper.GenLadderGroupDetailUrl(_a, _i);
            return Redirect(url);
        }
    }
}