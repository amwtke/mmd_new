using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Lib.DB.Redis.MD.ForTest;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Wechat.Controllers.PinTuanController;
using Microsoft.Ajax.Utilities;
using MD.Lib.DB.Redis.MD.WxStatistics;
using System.Configuration;

namespace MD.Wechat.Controllers.WriteOffController
{
    public class WriteOffController : Controller
    {
        // GET: WriteOff
        public async Task<ActionResult> Index(string code, string state, string appid, string bizid)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"<h1>writeoff——用户授权失败！-----code:{code},appid:{appid}</h1>");

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

                Response.Cookies["woid"].Value = bizid;
                Response.Cookies["woid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/write/bind/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/write/bind/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception ex)
            {
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenWriteOfferUrl(appid, Guid.Parse(bizid)));
                //throw new MDException(typeof(WriteOffController), ex);
            }
        }

        public async Task<ActionResult> WriteOffOrder(string code, string state, string appid, string bizid)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"<h1>writeoff——用户授权失败！-----code:{code},appid:{appid}</h1>");

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
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/write/no/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/write/no/" + bizid + "&" + tuple.Item1.Openid;
                //MDLogger.LogInfoAsync(typeof(GroupController), $"groupdetail:{url}");
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception ex)
            {
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenWriteOffOrderUrl(appid, Guid.Parse(bizid)));
                //throw new MDException(typeof(WriteOffController),ex);
            }
        }

        /// <summary>
        /// 宝箱核销
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <param name="appid"></param>
        /// <param name="bizid"></param>
        /// <returns></returns>
        public async Task<ActionResult> WriteOffFindBox(string code, string state, string appid, string bizid)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"<h1>writeoff——用户授权失败！-----code:{code},appid:{appid}</h1>");

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

                Response.Cookies["utid"].Value = bizid;
                Response.Cookies["utid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/toolbox/xbhx/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/toolbox/xbhx/" + bizid + "&" + tuple.Item1.Openid;
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception ex)
            {
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenWriteOffFindBoxUrl(appid, bizid));
                //throw new MDException(typeof(WriteOffController),ex);
            }
        }

        /// <summary>
        /// 签到核销
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <param name="appid"></param>
        /// <param name="bizid"></param>
        /// <returns></returns>
        public async Task<ActionResult> WriteOffSign(string code, string state, string appid, string bizid)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Content($"<h1>微信服务器错误！请刷新或者重试！</h1>");
                }

                var tuple = await MdUserHelper.SaveUserFromCallback2(appid, code);
                if (tuple?.Item1 == null || tuple.Item2 == null || tuple.Item3 == null)
                    return Content($"<h1>writeoff——用户授权失败！-----code:{code},appid:{appid}</h1>");

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

                Response.Cookies["usid"].Value = bizid;
                Response.Cookies["usid"].Expires = DateTime.Now.AddSeconds(1200);

                if (await RedisForTestOp.ContainAppidAsync(appid))
                {
                    //MDLogger.LogInfoAsync(typeof(GroupController),$"测试号:{appid}");
                    return Redirect(@"~/mmpt_test/app/#/app/toolbox/signhx/" + bizid + "&" + tuple.Item1.Openid);
                }

                string url = @"~/mmpt/app/#/app/toolbox/signhx/" + bizid + "&" + tuple.Item1.Openid;
                await WxStatisticsOp.AddTotalLoginCount();
                return Redirect(url);
            }
            catch (Exception ex)
            {
                Thread.Sleep(1000);
                return Redirect(MdWxSettingUpHelper.GenWriteOffSignUrl(appid, bizid));
                //throw new MDException(typeof(WriteOffController),ex);
            }
        }
    }
}