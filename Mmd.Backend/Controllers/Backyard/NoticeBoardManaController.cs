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
using MD.Model.Configuration.Aliyun;
using MD.Model.Configuration.UI;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using MD.Lib.MQ.MD;
using System.Text.RegularExpressions;
using MD.Lib.Weixin.Component;
using MD.Lib.Util.Files;

namespace Mmd.Backend.Controllers.Backyard
{
    public class NoticeBoardManaController : Controller
    {
        Guid ManagerMid = Guid.Parse("8334f757-6ace-43c0-9afe-8d3d3c228a44");
        Guid DefaultMid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        // GET: NoticeBoardMana
        public async Task<ActionResult> Index(int status = 1)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.status = status;
            if (Guid.Parse(mid.ToString()) != ManagerMid)
                return View("MerIndex");
            return View();
        }
        public async Task<PartialViewResult> GetList(string q, int pageIndex,int status)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(NoticeBoardManaController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);
            var listCate = new List<CodeNoticeCategory>();
            using (var codeRepo = new CodeRepository())
            {
                listCate = await codeRepo.GetNoticeCateListAsync();
            }
            Guid merId = Guid.Parse(mid.ToString());
            string viewName = "Backyard/NoticeMana/MerNoticeManaPartial";
            var listMer = new List<Merchant>();
            if (merId == ManagerMid)
            {
                merId = Guid.Empty;
                viewName = "Backyard/NoticeMana/NoticeManaPartial";
            }   
            using (var repo = new BizRepository())
            {
                var tuple = await EsNoticeBoardManager.SearchByMidAsnyc(merId, q, pageIndex, pageSize, status);
                ViewBag.listCate = listCate;
                if (mid.ToString() == ManagerMid.ToString())
                {
                    var tupleMer = await repo.MerchantSearchByNameAsync("", 1, 1000, (int)ECodeMerchantStatus.已配置, "");
                    listMer = tupleMer.Item2;
                    ViewBag.listMerchant = listMer;
                }
                var list = new List<NoticeBoard>();
                if (tuple != null && tuple.Item1 > 0 && tuple.Item2 != null && tuple.Item2.Count > 0)
                {
                    list = await repo.GetNoticeBoardAsync(tuple.Item2);
                }
                return PartialView(viewName, new NoticeManaPartialObject()
                {
                    PageSize = pageSize,
                    PageIndex = pageIndex,
                    TotalCount = tuple.Item1,
                    List = list,
                    Q = q
                });
            }
        }
        public async Task<ActionResult> EditNoticeBoard(Guid nid)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");

            //判断ES中是否存在
            var indexnotice = await EsNoticeBoardManager.GetNoticeBoardAsync(nid);
            if (indexnotice == null || indexnotice.Id.Equals(Guid.Empty))
                return Content("erro IndexNoticeBoard==null! ");
            var listTemplate = new List<SelectListItem>();
            using (var codeRepo = new CodeRepository())
            {
                var list = await codeRepo.GetNoticeCateListAsync();
                foreach (var item in list)
                {
                    listTemplate.Add(new SelectListItem() { Value = item.code.ToString(), Text = item.name });
                }
            }
            using (var repo = new BizRepository())
            {
                var g = await repo.GetNoticeBoardAsync(nid);
                ViewBag.Categroy = listTemplate;
                return View("AddNoticeBoard", g);
            }
        }

        public async Task<ActionResult> AddNoticeBoard()
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            NoticeBoard nb = new NoticeBoard { nid=Guid.NewGuid()};
            var listTemplate = new List<SelectListItem>();
            using (var codeRepo = new CodeRepository())
            {
                var list = await codeRepo.GetNoticeCateListAsync();
                foreach (var item in list)
                {
                    listTemplate.Add(new SelectListItem() { Value = item.code.ToString(), Text = item.name });
                }
                ViewBag.Categroy = listTemplate;
                if (Guid.Parse(mid.ToString()) != ManagerMid)
                    return View("MerAddNoticeBoard", nb);
                return View(nb);
            }
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="nid">主键</param>
        /// <param name="status">需要修改的状态</param>
        /// <param name="nowStatus">修改后需停留的选项卡值</param>
        /// <returns></returns>
        public async Task<ActionResult> UpdateNoticeBoard(Guid nid, int status,ENoticeBoardStatus nowStatus)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            using (var repo = new BizRepository())
            {
                bool b = await repo.UpdateNoticeBoardStatusAsync(nid, status);
                if (b)
                {
                    //更新es
                    var index = await EsNoticeBoardManager.GenObjectAsync(nid);//从数据库中重新取，然后返回新的IndexNoticeBoard
                    if (index != null)
                    {
                        if (!EsNoticeBoardManager.AddOrUpdate(index))
                            throw new MDException(typeof(NoticeBoardManaController), "NoticeBoardManaController error!");
                    }
                }
                return RedirectToAction("Index", new { status = (int)nowStatus });
            }
        }

        public async Task<bool> DelNoticeBoard(Guid nid,int status)
        {
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return false;
            using (var repo = new BizRepository())
            {
                bool b = await repo.UpdateNoticeBoardStatusAsync(nid, status);
                if (b)
                {
                    //更新es
                    var index = await EsNoticeBoardManager.GenObjectAsync(nid);//从数据库中重新取，然后返回新的IndexNoticeBoard
                    if (index != null)
                    {
                        if (!EsNoticeBoardManager.AddOrUpdate(index))
                            throw new MDException(typeof(NoticeBoardManaController), "NoticeBoardManaController error!");
                    }
                }
                return true;
            }
        }

        public async Task<ActionResult> NoticeBoardSave(NoticeBoard nb, HttpPostedFileBase picg)
        {
            if (nb == null)
            {
                throw new MDException(typeof(NoticeBoardManaController), "添加失败，参数错误。");
            }
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return RedirectToAction("SessionTimeOut", "Session");
            Guid MerId = Guid.Parse(mid.ToString());
            if (MerId == ManagerMid) nb.mid = DefaultMid;
            else nb.mid = MerId;
            if (picg != null && picg.ContentLength > 0)
            {
                var timestamp = CommonHelper.GetUnixTimeNow();
                var path = OssPicPathManager<OssPicBucketConfig>.UploadNoticeBoardthumb_pic(nb.nid, timestamp.ToString(), picg.InputStream);
                if (!string.IsNullOrEmpty(path))
                    nb.thumb_pic = path;
                else
                    throw new MDException(typeof(NoticeBoardManaController), "上传文件失败！");
            }

            //存储
            using (var repo = new BizRepository())
            {
                nb = await repo.SaveOrUpdateNoticeBoardAsync(nb);
            }
            //更新es
            var index = await EsNoticeBoardManager.GenObjectAsync(nb.nid);//从数据库中重新取，然后返回新的IndexNoticeBoard
            if (index != null)
            {
                if (!await EsNoticeBoardManager.AddOrUpdateAsync(index))
                    throw new MDException(typeof(NoticeBoardManaController), "NoticeBoardManaController error!");
            }
            return RedirectToAction("Index", new {status = (int)ENoticeBoardStatus.待发布 });
        }


        /// <summary>
        /// 设置置顶
        /// </summary>
        /// <param name="nid"></param>
        /// <param name="operation">为settop时置顶，否则取消置顶</param>
        /// <returns></returns>
        public async Task<JsonResult> SetTop(Guid nid,string operation)
        {
            string extend = operation == "settop" ? CommonHelper.GetUnixTimeNow().ToString() : "";
            try
            {
                //存储
                using (var repo = new BizRepository())
                {
                    await repo.UpdateNoticeBoardTopAsync(nid, extend);
                }
                var index = await EsNoticeBoardManager.GetNoticeBoardAsync(nid);
                if (index != null)
                {
                    index.extend_1 = extend;
                    if (!await EsNoticeBoardManager.AddOrUpdateAsync(index))
                        throw new MDException(typeof(NoticeBoardManaController), "NoticeBoardManaController error!");
                }
                return Json(new { Status = "ok", Result = "Success" });
            }
            catch (Exception ex)
            {
                return Json(new { Status = "ok", Result = "Fail", Message=ex.Message});
            }
        }

        public async Task<JsonResult> SendArticle(Guid nid)
        {
            var index = await EsNoticeBoardManager.GetNoticeBoardAsync(nid);
            if(index == null) return Json(new { Status = "fail", Result = "Error:NoticeBoardNotExist" });
            string title = index.title;
            string des = CommonHelper.ReplaceHtmlStr(index.description);
            string description = des.Length > 82 ? des.Substring(0, 82) + "..." : des;
            string picurl = index.thumb_pic;
            string at0 = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId("wxa2f78f4dfc3b8ab6");
            var obj0 = MqWxTempMsgManager.GenGroupNewsObject("wxa2f78f4dfc3b8ab6", at0, "otuH9sjtu4yCQZ43oFua3qCVg7l4", title, "http://wxa2f78f4dfc3b8ab6.wx.mmpintuan.com/mmpt/app/#/app/cosmetics/page/3f0c3833-c387-4020-b305-173f3bee3e63", description, picurl);
            await MqWxTempMsgManager.SendMessageAsync(obj0);
            AsyncHelper.RunAsync(async delegate ()
            {
                using (var repo = new BizRepository())
                {
                    var tuple = await repo.MerchantSearchByNameAsync("", 1, 1000, (int)ECodeMerchantStatus.已配置, "");
                    var listMer = tuple.Item2;
                    for (int i = 0; i < listMer.Count; i++)
                    {
                        try
                        {
                            var mer = listMer[i];
                            var listUser = await EsUserManager.GetByMidAsync(mer.mid, 1000000);
                            string appid = mer.wx_appid;
                            string token = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
                            string url = "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + appid + "&redirect_uri=http%3A%2F%2F" + appid + ".wx.mmpintuan.com%2FGroup%2Fnoticedetail%3Fbizid%3D" + index.Id + "&response_type=code&scope=snsapi_userinfo&state=state&component_appid=wx323abc83f8c7e444#wechat_redirect";
                            for (int m = 0; m < listUser.Count; m++)
                            {
                                string openid = listUser[m].openid;
                                var obj = MqWxTempMsgManager.GenGroupNewsObject(appid, token, openid, title, url, description, picurl);
                                await MqWxTempMsgManager.SendMessageAsync(obj);
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }, null);
            return Json(new { Status = "ok", Result = "Success"});
        }

        public async Task<JsonResult> Test()
        {
            int count = 0;
            List<object> list = new List<object>();
            for (int i = 0; i <= 9999; i++)
            {
                int r = 0;
                int m = i;
                int n = i;
                while (m > 0)
                {
                    int t = m % 10;
                    r += t == 7 ? 1 : 0;
                    m = (m - t) / 10;
                }
                count += r;
                list.Add(new { num = n,count = r,sum = count});
            }
            return Json(new { data = list });
        }

        public async Task<JsonResult> TestAsyncRun()
        {
            //AsyncHelper.Run2Async(delegate() {
            //    for (int i = 0; i < 5; i++)
            //    {
            //        System.Threading.Thread.Sleep(1000);
            //        System.IO.File.AppendAllText(@"d:\log.txt", i.ToString());
            //    }
            //},null);
            System.Threading.SpinLock slock = new System.Threading.SpinLock(false);
            Parallel.For(0, 5, item =>
            {
                bool lockTaken = false;
                try
                {
                    slock.Enter(ref lockTaken);
                    System.IO.File.AppendAllText(@"d:\log.txt", item.ToString() + Environment.NewLine);
                }
                finally
                {
                    if (lockTaken)
                        slock.Exit(false);
                    System.Threading.Thread.Sleep(1000);
                }   
            });
            return Json(new { status = "ok"});
        }

        public JsonResult TestZipFile()
        {
            //string filesPath = Server.MapPath("/Content");
            string filesPath = @"E:\project\demo";
            ZipFileHelper.CreateZip(filesPath, "E:\\test.zip");
            return Json(new { status = "success"});
        }

        public class NoticeManaPartialObject
        {
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public List<NoticeBoard> List { get; set; }

            public string Q { get; set; }
        }
    }
}