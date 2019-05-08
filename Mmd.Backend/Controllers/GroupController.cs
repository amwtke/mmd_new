using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using MD.Configuration;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.Configuration.Aliyun;
using MD.Model.Configuration.UI;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using System.Text;
using MD.Lib.Log;
using MD.Lib.Weixin.Component;
using MD.Lib.MQ.MD;
using MD.Model.MQ;
using MD.Lib.ElasticSearch;
using Senparc.Weixin.MP.AdvancedAPIs;
using MD.Model.MQ.MD;
using MD.Lib.Weixin.Message;
using Senparc.Weixin.Entities;
using MD.Model.DB.Professional;
using System.Drawing;
using System.Net;
using MD.Lib.Util.Files;

namespace Mmd.Backend.Controllers
{
    public class GroupController : Controller
    {
        // GET: Group
        public ActionResult Index()
        {
            return View();
        }

        public async Task<bool> CanAddGroup()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return false;
            using (var repo = new BizRepository())
            {
                var mbiz = await repo.GetMbizAsync(mer.mid, "DD");
                if (mbiz != null && mbiz.quota_remain > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<ActionResult> AddGroup(Guid pid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            bool canadd = await CanAddGroup();
            if (!canadd)
                return Content("您好，贵公司剩余的拼团订单额不足，无法创建新的拼团活动，请联系工作人员充值，联系电话：18108611928！");
            Product p = null;
            List<WriteOffPoint> WriteOffPoints = new List<WriteOffPoint>();
            StringBuilder woidList = new StringBuilder();
            using (var repo = new BizRepository())
            {
                p = await repo.GetProductByPidAsync(pid);
                if (p == null || p.pid == Guid.Empty)
                {
                    MDLogger.LogError(typeof(GroupController),new Exception($"pid:{pid.ToString()} is null!"));
                    return Content($"pid:{ pid.ToString()} is null!");
                }
                    //throw new MDException(typeof(GroupController), $"pid:{pid.ToString()} is null!");
                WriteOffPoints = await repo.GetWOPsByMidAsync(mer.mid);
                foreach (var wopt in WriteOffPoints)
                {
                    woidList.Append(wopt.woid);
                    woidList.Append(",");
                }
            }
            Group g = new Group
            {
                gid = Guid.NewGuid(),
                pid = p.pid,
                mid = p.mid,
                time_limit = TimeSpan.FromDays(1).TotalSeconds,
                userobot = 0,//默认不使用
                order_limit = 0,//默认无限制
                isshowpting = 1,//默认显示
                group_type = 0,//默认普通团
                WriteOffPoints = WriteOffPoints,
                activity_point = woidList.ToString().Trim(','),//默认全部选中   
                Commission = 1
            };
            ViewBag.p_no = p.p_no.ToString();
            ViewBag.userobot = g.userobot;
            ViewBag.status = (int)EGroupStatus.待发布;
            //ViewBag.merName = mer.name;
            var listTemplate = new List<SelectListItem>();
            using (var repo = new AddressRepository())
            {
                List<Logistics_Template> list = await repo.GetTemplateByMidAsync(mer.mid);
                foreach (var template in list)
                {
                    listTemplate.Add(new SelectListItem() { Value = template.ltid.ToString(), Text = template.name });
                }
            }
            ViewBag.listTemplate = listTemplate;
            return View(g);
        }

        public async Task<ActionResult> EditGroup(Guid gid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");

            var group = await EsGroupManager.GetByGidAsync(gid);
            if (group == null || group.Id.Equals(Guid.Empty))
                await EsGroupManager.AddOrUpdateAsync(await EsGroupManager.GenObject(gid));

            ViewBag.p_no = group.p_no;
            var listTemplate = new List<SelectListItem>();
            using (var repo = new AddressRepository())
            {
                List<Logistics_Template> list = await repo.GetTemplateByMidAsync(mer.mid);
                foreach (var template in list)
                {
                    listTemplate.Add(new SelectListItem() { Value = template.ltid.ToString(), Text = template.name });
                }
            }
            ViewBag.listTemplate = listTemplate;
            using (var repo = new BizRepository())
            {
                var g = await repo.GroupGetGroupById(Guid.Parse(group.Id));
                ViewBag.userobot = g.userobot;
                ViewBag.status = g.status;
                g.WriteOffPoints = await repo.GetWOPsByMidAsync(g.mid);
                return View("AddGroup", g);
            }
        }

        public async Task<ActionResult> GroupSave(Group group, HttpPostedFileBase picg, string[] activity_point)
        {
            try
            {
                Merchant mer = await SessionHelper.GetMerchant(this);
                if (mer == null)
                    return RedirectToAction("SessionTimeOut", "Session");

                if (group == null || group.gid.Equals(Guid.Empty) || group.pid.Equals(Guid.Empty) || group.mid.Equals(Guid.Empty))
                {
                    throw new MDException(typeof(GroupController), $"开团失败，参数错误。gid:{group.gid},pid:{group.pid},mid:{group.mid}");
                }
                if (!mer.mid.Equals(group.mid))
                {
                    throw new MDException(typeof(GroupController), $"添加团失败！mid不一致！商品的mid为：{group.mid},但是session的mid:{mer.mid}");
                }
                if (group.waytoget != (int)EGroupWaytoget.自提)
                {
                    if (group.ltid == Guid.Empty)
                    {
                        return Content("请设置运费模板");
                    }
                }
                else
                {
                    group.ltid = Guid.Empty;
                }
                if (picg != null && picg.ContentLength > 0)
                {
                    var timestamp = CommonHelper.GetUnixTimeNow();
                    var path = OssPicPathManager<OssPicBucketConfig>.UploadGroupAdvertisPic(group.gid, timestamp.ToString(), picg.InputStream);
                    if (!string.IsNullOrEmpty(path))
                        group.advertise_pic_url = path;
                    else
                        throw new MDException(typeof(GroupController), "上传文件失败！");
                }

                //存储
                using (var repo = new BizRepository())
                {
                    var product = await repo.GetProductByPidAsync(group.pid);
                    if (product == null)
                    {
                        throw new MDException(typeof(GroupController), $"添加Group失败！pid:{group.pid}");
                    }
                    group.biz_type = EBizType.DD.ToString();
                    group.time_limit = TimeSpan.FromDays(1).TotalSeconds;
                    group.origin_price = product.price;
                    group.last_update_time = CommonHelper.GetUnixTimeNow();

                    //如果没有设置过product_seting_count则设置。
                    if (group.product_setting_count == null || group.product_setting_count == 0 || group.status == (int)EGroupStatus.待发布)
                        group.product_setting_count = group.product_quota;
                    //解析选中的活动门店woid
                    StringBuilder sbpoint = new StringBuilder();
                    if (activity_point != null && activity_point.Count() > 0)
                    {
                        foreach (var str in activity_point)
                        {
                            sbpoint.Append(str + ",");
                        }
                    }
                    //如果是抽奖团，则不开启自动成团
                    if (group.group_type == (int)EGroupTypes.抽奖团)
                    {
                        group.userobot = 0;
                    }
                    group.activity_point = sbpoint.ToString().Trim(',');
                    group = await repo.GroupAddOrUpdateAsync(group);
                }

                var index = await EsGroupManager.GenObject(group.gid);
                if (index != null)
                {
                    if (!await EsGroupManager.AddOrUpdateAsync(index))
                        throw new MDException(typeof(GroupController), "EsGroupManager error!");
                }
                //return Content($"gid:{group.gid},状态:{group.GroupStatus.description},title:{group.title},groupprice:{group.group_price},productprice:{group.origin_price},人数:{group.person_quota},库存：{group.product_quota},时长:{group.time_limit},宣传图：{group.advertise_pic_url},biztype:{group.BizType.description},取货方式:{group._WayToGet.description},商品：{group.pid},商家:{group.mid}");
                if (group.group_type == (int)EGroupTypes.抽奖团)
                {
                    if (!string.IsNullOrEmpty(group.lucky_endTime) && group.lucky_endTime != "0")
                        group.lucky_endTime = CommonHelper.FromUnixTime(Convert.ToDouble(group.lucky_endTime)).ToString("yyyy-MM-dd HH:mm");
                    return View("AddGroup_lucky", group);
                }
                return RedirectToAction("GroupManage", new { status = group.status });
            }
            catch (Exception ex)
            {
                // throw new MDException(typeof(GroupController), new Exception($"后台GroupSave报错," + ex.Message));
                MDLogger.LogErrorAsync(typeof(GroupController), new Exception($"后台GroupSave报错,gid:{group.gid}" + ex.Message));
                return Content($"后台GroupSave报错,请联系工作人员。");
            }
        }

        public async Task<ActionResult> GroupSave_lucky(Group group)
        {
            try
            {
                if (group == null || group.gid.Equals(Guid.Empty) || group.pid.Equals(Guid.Empty) || group.mid.Equals(Guid.Empty))
                {
                    throw new MDException(typeof(GroupController), "抽奖团开团失败，参数错误。");
                }
                Merchant mer = await SessionHelper.GetMerchant(this);
                if (mer == null)
                    return RedirectToAction("SessionTimeOut", "Session");
                if (!mer.mid.Equals(group.mid))
                {
                    throw new MDException(typeof(GroupController), $"添加团失败！mid不一致！商品的mid为：{group.mid},但是session的mid:{mer.mid}");
                }
                using (var repo = new BizRepository())
                {
                    Group g = await repo.GroupGetGroupById(group.gid);
                    g.lucky_count = group.lucky_count;
                    g.lucky_endTime = CommonHelper.ToUnixTime(Convert.ToDateTime(group.lucky_endTime)).ToString();
                    await repo.Group_luckyUpdateAsync(g);
                }
                return RedirectToAction("GroupManage", new { status = group.status });
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(GroupController), new Exception($"后台GroupSave_lucky报错lucky_endTime:{group.lucky_endTime},lucky_count:{group.lucky_count}" + ex.Message));
            }
        }

        public async Task<ActionResult> GroupManage(int status)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");

            ViewBag.status = status;
            return View();
        }

        public async Task<JsonResult> SendGroupNews(Guid gid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            DateTime today = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 00:00:00"));
            double timeStart = CommonHelper.ToUnixTime(today);
            double timeEnd = CommonHelper.ToUnixTime(today.AddDays(1));
            var tuple = EsBizLogStatistics.SearchBizView(ELogBizModuleType.MerSendMsg, Guid.Empty, mer.mid, timeStart, timeEnd);
            if (tuple.Item1 > 0)
            {
                return Json(new { status = "LimitTimesOut" });
            }
            string appid = mer.wx_appid;
            string token = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
            var group = await EsGroupManager.GetByGidAsync(gid);
            var listUser = await EsUserManager.GetByMidAsync(mer.mid,1000000);
            if (group != null && listUser != null && listUser.Count > 0)
            {
                string title = group.title;
                string description = group.description;
                string picurl = group.advertise_pic_url;
                string url = "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + appid + "&redirect_uri=http%3A%2F%2F" + appid + ".wx.mmpintuan.com%2FGroup%2Fgroupdetail%3Fbizid%3D" + gid.ToString() + "&response_type=code&scope=snsapi_userinfo&state=state&component_appid=wx323abc83f8c7e444#wechat_redirect";
                //string url0 = "http://" + appid + ".wx.mmpintuan.com/mmpt/app/#/app/gokt/one/" + gid + "&" + listUser[0].openid;
                var obj0 = MqWxTempMsgManager.GenGroupNewsObject(appid, token, listUser[0].openid, title, url, description, picurl);
                await MqWxTempMsgManager.SendMessageAsync(obj0);
                AsyncHelper.RunAsync(async delegate ()
                {
                    for (int i = 1; i < listUser.Count ; i++)
                    {
                        string openid = listUser[i].openid;
                        //string url = "http://" + appid + ".wx.mmpintuan.com/mmpt/app/#/app/gokt/one/" + gid + "&" + openid;
                        var obj = MqWxTempMsgManager.GenGroupNewsObject(appid, token, openid, title, url, description, picurl);
                        await MqWxTempMsgManager.SendMessageAsync(obj);
                    }
                }, null);
            }
            MDLogger.LogBiz(typeof(GroupController),
                    new BizMQ(ELogBizModuleType.MerSendMsg.ToString(), "", mer.mid, gid.ToString(), null, null, null));
            return Json(new { status = "Success"});
        }

        public async Task<JsonResult> SendGroupImg(Guid gid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            string at = "";
            string media_id = "";
            List<WriteOffer> list = null;
            using (var repo = new BizRepository())
            {
                var media = await repo.GroupMediaGetByGid(gid);
                if (media != null)
                {
                    string appid = mer.wx_appid;
                    media_id = media.media_id;
                    at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
                    list = await repo.GetWOerByMid2Async(mer.mid,true);
                }
            }
            AsyncHelper.RunAsync(delegate () {
                if (list != null && list.Count > 0 && at != "")
                {
                    string URL_FORMAT = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}";
                    foreach (var item in list)
                    {
                        string openid = item.openid;
                        var sendData = new
                        {
                            touser = openid,
                            msgtype = "image",
                            image = new { media_id = media_id }
                        };
                        try
                        {
                            Senparc.Weixin.CommonAPIs.CommonJsonSend.Send<WxJsonResult>(at, URL_FORMAT, sendData);
                        }
                        catch (Exception ex)
                        {
                            continue;
                        }
                    }
                }
            }, null);
            return Json(new { status = "Success" });
        }

        public string GenImage(Group group,Merchant mer)
        {
            Image img = Image.FromFile(Server.MapPath("/Content/MediaFiles/ad002.jpg"));
            //生成随机的背景图
            Bitmap backgroundimg = new Bitmap(img);
            if (group == null || mer == null)
                return "";
            Graphics g = Graphics.FromImage(backgroundimg);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            //加Logo
            Uri uriLogo = new Uri(mer.logo_url);
            WebRequest webRequest = WebRequest.Create(uriLogo);
            WebResponse webResponse = webRequest.GetResponse();
            Bitmap bitLogo = new Bitmap(webResponse.GetResponseStream());
            g.DrawImage(bitLogo, 25, 25, 110, 110);
            //加商家名称
            g.DrawString(mer.name, getFont(36), new SolidBrush(Color.Black), new PointF(145, 60));
            //加活动标题
            g.DrawString(group.title, getFont(36), new SolidBrush(Color.White), new RectangleF(new PointF(25, 170), new SizeF(700, 95)));
            //加活动主图片
            Uri uripic1 = new Uri(group.advertise_pic_url);
            WebRequest webRequest2 = WebRequest.Create(uripic1);
            WebResponse webResponse2 = webRequest2.GetResponse();
            Bitmap bitpic1 = new Bitmap(webResponse2.GetResponseStream());
            g.DrawImage(bitpic1, 35, 300, 680, 450);
            //加价格和介绍
            g.DrawString("原价：" + (group.origin_price.Value / 100.00).ToString("f2") + "元    拼团价：", getFont(32), new SolidBrush(Color.Black), new PointF(35, 755));
            g.DrawString("" + (group.group_price.Value / 100.00).ToString("f2") + "元", getFont(40), new SolidBrush(Color.Red), new PointF(435, 745));
            g.DrawString(group.description.Replace("\n\r","  "), getFont(24), new SolidBrush(Color.Gray), new RectangleF(new PointF(35, 800), new SizeF(700, 125)));
            g.DrawLine(new Pen(Color.Black), new PointF(35, 780), new PointF(270, 780));
            //活动二维码
            string groupCode = GetGroupDetail(mer.wx_appid, group.gid.ToString());
            var bitcode = ImageHelper.GenQr_Code(groupCode, 280, 280);
            g.DrawImage(bitcode, 235, 970, bitcode.Width, bitcode.Height);
            g.Dispose();
            //MemoryStream ms = new MemoryStream();
            //backgroundimg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            string filePath = "/Content/MediaFiles/" + group.gid.ToString() + ".jpg";
            backgroundimg.Save(Server.MapPath(filePath));
            return filePath;
        }

        private string GetGroupDetail(string appid,string gid)
        {
            return "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + appid + "&redirect_uri=http%3A%2F%2F" + appid + ".wx.mmpintuan.com%2FGroup%2Fgroupdetail%3Fbizid%3D" + gid + "&response_type=code&scope=snsapi_userinfo&state=state&component_appid=wx323abc83f8c7e444#wechat_redirect";
        }

        private string GetGroupFxDetail(string appid, string gid,string shareopenid)
        {
            return "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + appid + "&redirect_uri=http%3A%2F%2F" + appid + ".wx.mmpintuan.com%2FGroup%2Fgroupdetail%3Fbizid%3D" + gid + "&shareopenid=" + shareopenid + "&response_type=code&scope=snsapi_userinfo&state=state&component_appid=wx323abc83f8c7e444#wechat_redirect";
        }

        private Font getFont(int size)
        {
            FontFamily fm = new FontFamily("微软雅黑");
            Font font = new Font(fm, size, FontStyle.Regular, GraphicsUnit.Pixel);
            return font;
        }
        public async Task<ActionResult> UploadImg(Guid mid,Guid gid,string filePath)
        {
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId("wxa2f78f4dfc3b8ab6");
            var res = MediaApi.UploadForeverMedia(at, filePath);
            if (!string.IsNullOrEmpty(res.media_id))
            {
                Group_Media media = new Group_Media();
                media.gid = gid;
                media.mid = mid;
                string mediaId = res.media_id;
                media.media_id = mediaId;
                media.createtime = CommonHelper.GetUnixTimeNow();
                media.pic = filePath;
                using (var repo = new BizRepository())
                {
                    await repo.GroupMediaAddAsync(media);
                }
                return Json(new { res = mediaId });
            }
            return Json(new { res = "error" });
        }

        public async Task<ActionResult> SendImg()
        {
            string appid = "wxa2f78f4dfc3b8ab6";
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
            string openid = "otuH9sjtu4yCQZ43oFua3qCVg7l4";
            string URL_FORMAT = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}";
            var sendData = new
            {
                touser = openid,
                msgtype = "image",
                image = new
                {
                    media_id = "Sf6nI8WwPQl6dDMvclJVWgmy4Q3aCWv9epJTWes3L1-q4a4rcaUl8P3W7fn1niy8"
                }
            };
            WxJsonResult result = Senparc.Weixin.CommonAPIs.CommonJsonSend.Send<WxJsonResult>(at, URL_FORMAT, sendData);
            return Json(new { res = result });
        }

        public async Task<string> GetImgUrl(Guid gid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return "";
            using (var repo = new BizRepository())
            {
                var groupMedia = await repo.GroupMediaGetByGid(gid);
                if (groupMedia != null)
                {
                    return groupMedia.pic;
                }
                else
                {
                    var group = await repo.GroupGetGroupById(gid);
                    string filePath = GenImage(group,mer);
                    if (filePath != "")
                    {
                        AsyncHelper.RunAsync(delegate () {
                            try
                            {
                                string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(mer.wx_appid);
                                var res = MediaApi.UploadForeverMedia(at, Server.MapPath(filePath));
                                if (!string.IsNullOrEmpty(res.media_id))
                                {
                                    using (var repoBiz = new BizRepository())
                                    {
                                        Group_Media media = new Group_Media();
                                        media.gid = gid;
                                        media.mid = mer.mid;
                                        string mediaId = res.media_id;
                                        media.media_id = mediaId;
                                        media.createtime = CommonHelper.GetUnixTimeNow();
                                        media.pic = filePath;
                                        bool result = repoBiz.GroupMediaAdd(media);
                                    }
                                }
                            }
                            catch (Exception){}
                        }, null);
                    }
                    return filePath;                 
                }
            }
        }
        #region 用于分页展示
        public async Task<PartialViewResult> GroupGetPartial(int status, int pageIndex, string q)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");

            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);
            ViewBag.mid = Guid.Parse(mid.ToString());
            //es get data
            if (string.IsNullOrEmpty(q)) //无搜索
            {
                using (var repo = new BizRepository())
                {
                    var tuple = await repo.GroupGetListByStatus(Guid.Parse(mid.ToString()), status, pageIndex, pageSize);
                    if (tuple.Item1 != 0 || tuple.Item2.Count != 0)
                    {
                        return PartialView("Group/_GroupListPatrial",
                            GenParameters(pageIndex, tuple.Item1, pageSize, status, tuple.Item2));
                    }
                    return PartialView("Group/_GroupListPatrial",
                        GenParameters(pageIndex, tuple.Item1, pageSize, status, new List<Group>()));
                }
            }

            using (var repo = new BizRepository())
            {
                var tuple =
                    await EsGroupManager.SearchAsnyc(q, status, Guid.Parse(mid.ToString()), pageIndex, pageSize);
                if (tuple.Item2 != null && tuple.Item2.Count > 0)
                {
                    var list = await repo.GroupGetByList(tuple.Item2);
                    return PartialView("Group/_GroupListPatrial",
                        GenParameters(pageIndex, tuple.Item1, pageSize, status, list));
                }
                return PartialView("Group/_GroupListPatrial",
                    GenParameters(pageIndex, tuple.Item1, pageSize, status, new List<Group>()));
            }

            //var tuple = await EsGroupManager.SearchAsnyc2(q, status, Guid.Parse(mid.ToString()), pageIndex, pageSize);
            //if (tuple.Item2.Count > 0)
            //{
            //    return PartialView("Group/_GroupListPatrial",
            //        GenParameters(pageIndex, tuple.Item1, pageSize, status, tuple.Item2));
            //}
            //return PartialView("Group/_GroupListPatrial",
            //    GenParameters(pageIndex, tuple.Item1, pageSize, status, new List<Group>()));

        }

        private PartialParameter GenParameters(int pageIndex, int totalCount, int pageSize, int status, List<Group> list)
        {
            return new PartialParameter(pageIndex, totalCount, pageSize, status, list);
        }

        public class PartialParameter
        {
            public PartialParameter(int pageIndex, int totalCount, int pageSize, int status,
                List<Group> list)
            {
                PageIndex = pageIndex;
                TotalCount = totalCount;
                PageSize = pageSize;
                Status = status;
                List = list;
            }
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int Status { get; set; }
            public List<Group> List { get; set; }
        }

        public async Task<JsonResult> DelGroup(Guid gid, string q)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return Json(new { status = "SessionTimeOut" });

            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);

            using (var repo = new BizRepository())
            {
                if (await repo.GroupDel(gid))
                {
                    if (await EsGroupManager.AddOrUpdateAsync(await EsGroupManager.GenObject(gid)))
                    {
                        return Json(new { status = "Success" });
                    }
                }
                return Json(new { status = "Fail" });
            }
        }

        /// <summary>
        /// 上线，变成已发布
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="pageIndex"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public async Task<PartialViewResult> Online(Guid gid, string q)
        {
            //session
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");

            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);
            
            using (var repo = new BizRepository())
            {
                //var groupMedia = await repo.GroupMediaGetByGid(gid);
                //if (await repo.GroupOnline(gid) && groupMedia == null)
                //{
                //    var group = await repo.GroupGetGroupById(gid);
                //    AsyncHelper.RunAsync(delegate () {
                //        string filePath = GenImage(group,mer);
                //        if (filePath != "")
                //        {
                //            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(mer.wx_appid);
                //            var res = MediaApi.UploadForeverMedia(at, Server.MapPath(filePath));
                //            if (!string.IsNullOrEmpty(res.media_id))
                //            {
                //                using (var repoBiz = new BizRepository())
                //                {
                //                    Group_Media media = new Group_Media();
                //                    media.gid = gid;
                //                    media.mid = mer.mid;
                //                    string mediaId = res.media_id;
                //                    media.media_id = mediaId;
                //                    media.createtime = CommonHelper.GetUnixTimeNow();
                //                    media.pic = filePath;
                //                    bool result = repoBiz.GroupMediaAdd(media);
                //                }
                //            }
                //        } 
                //    }, null);
                //    var indexGroup = await EsGroupManager.GenObject(gid);
                //    if (await EsGroupManager.AddOrUpdateAsync(indexGroup))
                //    {
                //        return await GroupGetPartial((int)EGroupStatus.待发布, 1, q);
                //    }
                //}
                if (await repo.GroupOnline(gid))
                {
                    var indexGroup = await EsGroupManager.GenObject(gid);
                    if (await EsGroupManager.AddOrUpdateAsync(indexGroup))
                    {
                        return await GroupGetPartial((int)EGroupStatus.待发布, 1, q);
                    }
                }
                return PartialView("ProductPartial/ProductListErrorPartial", $"要上线的团不存在！gid:{gid.ToString()}");
            }
        }

        /// <summary>
        /// 清空库存，导致团结束
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public async Task<PartialViewResult> InventoryClear(Guid gid, string q)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");

            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);

            using (var repo = new BizRepository())
            {
                if (await repo.GroupInventoryClear(gid))
                {
                    if (await EsGroupManager.AddOrUpdateAsync(await EsGroupManager.GenObject(gid)))
                    {
                        return await GroupGetPartial((int)EGroupStatus.已发布, 1, q);
                    }
                }
            }
            return await GroupGetPartial((int)EGroupStatus.已发布, 1, q);
        }

        public async Task<PartialViewResult> Addproduct_quota(Guid gid, int product_quota, string q)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            using (var repo = new BizRepository())
            {
                if (await repo.GroupInventoryAdd(gid, product_quota))
                {
                    if (await EsGroupManager.AddOrUpdateAsync(await EsGroupManager.GenObject(gid)))
                    {
                        return await GroupGetPartial((int)EGroupStatus.已发布, 1, q);
                    }
                }
            }
            return await GroupGetPartial((int)EGroupStatus.已发布, 1, q);
        }

        /// <summary>
        /// 已删除到待发布
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="pageIndex"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public async Task<PartialViewResult> Resume(Guid gid, string q)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");

            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);

            using (var repo = new BizRepository())
            {
                if (await repo.GroupResume(gid))
                {
                    if (await EsGroupManager.AddOrUpdateAsync(await EsGroupManager.GenObject(gid)))
                    {
                        return await GroupGetPartial((int)EGroupStatus.已删除, 1, q);
                    }
                }
                return PartialView("ProductPartial/ProductListErrorPartial", $"要还原的团不存在！gid:{gid.ToString()}");
            }
        }

        #endregion

        #region Group detail and  partial

        public async Task<ActionResult> GroupDetail(Guid gid)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");

            return View(gid);
        }

        /// <summary>
        /// 团详情页
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public async Task<PartialViewResult> GroupGetDetailPartial(Guid gid, int pageIndex)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");

            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);

            //db get data
            if (gid.Equals(Guid.Empty))
                return PartialView("ProductPartial/ProductListErrorPartial", "gid is null!");

            //indexGroup
            var indexGroup = await EsGroupManager.GetByGidAsync(gid);
            if (indexGroup == null)
                return PartialView("ProductPartial/ProductListErrorPartial", $"gid:{gid}的团找不到!");

            using (var repo = new BizRepository())
            {
                var indexs = await EsGroupOrderManager.GetByGidAsync(gid, pageIndex, pageSize);
                if (indexs != null && indexs.Item2.Count > 0)
                {
                    List<Guid> _goGuids = new List<Guid>();
                    foreach (var i in indexs.Item2)
                    {
                        _goGuids.Add(Guid.Parse(i.Id));
                    }
                    var goList = await repo.GroupOrderGetByListAsnyc(_goGuids);
                    return PartialView("Group/_GroupDetailPartial",
                        new GroupDetailPartialObject()
                        {
                            List = goList,
                            PageSize = pageSize,
                            PageIndex = pageIndex,
                            TotalCount = indexs.Item1,
                            EsObject = indexGroup
                        });
                }
            }
            //没有数据时返回
            return PartialView("Group/_GroupDetailPartial",
                new GroupDetailPartialObject()
                {
                    List = new List<GroupOrder>(),
                    PageSize = pageSize,
                    PageIndex = pageIndex,
                    TotalCount = 0,
                    EsObject = indexGroup
                });
        }

        public class GroupDetailPartialObject
        {
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int PageIndex { get; set; }
            public List<GroupOrder> List { get; set; }
            public IndexGroup EsObject { get; set; }
        }

        public string GetPreviewUrl(string appid, Guid gid)
        {
            return MdWxSettingUpHelper.GenGroupPreviewUrl(appid, gid);
        }

        #endregion
    }
}