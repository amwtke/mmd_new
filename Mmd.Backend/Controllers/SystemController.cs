using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Configuration;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Aliyun;
using MD.Model.Configuration.UI;
using MD.Model.DB;

namespace Mmd.Backend.Controllers
{
    public enum EsysTagName
    {
        M=0,
        W=1,
        AddWoer=2,
        AddWop=3,
        AllWopers = 4
    }
    public class SystemController : Controller
    {
        // GET: System
        public async Task<ActionResult> Index(Guid? woid,int tag= 0)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.tag = tag;
            ViewBag.woid = woid;
            return View();
        }

        /// <summary>
        /// ajax 调用
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="q"></param>
        /// <param name="indexPage"></param>
        /// <param name="wop"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public async Task<PartialViewResult> SwichTag(int? tag,string q,int? indexPage,Guid? woid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");


            if (tag == (int)EsysTagName.M)
            {
                return PartialView("Sys/_SysMerchantPartial", mer);
            }

            if (tag == (int) EsysTagName.W)
            {
                q = string.IsNullOrEmpty(q) ? "" : q;
                if(indexPage==null)
                    return await GetWopsPartial(q, 1);
                return await GetWopsPartial(q,indexPage.Value);
            }

            if (tag == (int) EsysTagName.AddWoer)
            {
                if (woid != null)
                    return await GetWoerPartial(woid.Value);
                return await GetWoerPartial(Guid.Empty);
            }

            if (tag == (int) EsysTagName.AddWop)
            {
                if (woid != null)
                    return await GetWopPartial(woid.Value);
                return await GetWopPartial(Guid.Empty);
            }
            if (tag == (int) EsysTagName.AllWopers)
            {
                return await GetAllWoper("");
            }

            return PartialView("ProductPartial/ProductListErrorPartial", $"tag wrong tag:{tag}");
        }

        #region update merchant

        public async Task<ActionResult> UpdateMerchant(Merchant postObj, HttpPostedFileBase pic)
        {
            object appid = SessionHelper.Get(this, ESessionStateKeys.AppId);
            if (appid==null)
                return RedirectToAction("SessionTimeOut", "Session");
            if (!string.IsNullOrEmpty(postObj?.wx_appid) && pic != null && pic.ContentLength > 0)
            {
                var path = OssPicPathManager<OssPicBucketConfig>.UploadMerchantAdvertisPic(appid.ToString(), pic.FileName, pic.InputStream);
                if (!string.IsNullOrEmpty(path))
                    postObj.advertise_pic_url = path;
                else
                    throw new MDException(typeof(GroupController), "上传文件失败！");
            }


            using (var repo = new BizRepository())
            {
                await repo.SaveOrUpdateRegisterMerchantAsync(postObj);
            }
            return RedirectToAction("Index");
        }

        #endregion

        #region wop

        public class WOPsPartialObject
        {
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public string q { get; set; }
            public List<WOPPartialObject> List { get; set; }
        }

        public class WOPPartialObject
        {
            public string name { get; set; }
            public string address { get; set; }
            public string tel { get; set; }
            public string woid { get; set; }
            public int count { get; set; }

        }
        /// <summary>
        /// wop的列表也
        /// </summary>
        /// <param name="q"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        private async Task<PartialViewResult> GetWopsPartial(string q, int pageIndex)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            //page size
            //UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            //if (uiConfig == null)
            //    throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            //int pageSize = int.Parse(uiConfig.PageSize);
            int pageSize = 20;
            var list = await EsWriteOffPointManager.SearchAsnyc(q, mer.mid, pageIndex, pageSize);
            if (list.Item1 != 0)
            {
                using (var repo = new BizRepository())
                {
                    List<WOPPartialObject> retList = new List<WOPPartialObject>();
                    Dictionary<Guid, int> dic = await repo.GetWOerByMidAsync(mer.mid);
                    foreach (var i in list.Item2)
                    {
                        Guid id = Guid.Parse(i.Id);
                        WOPPartialObject temp = new WOPPartialObject()
                        {
                            address = i.address,
                            name = i.name,
                            tel = i.tel,
                            woid = i.Id,
                            count = dic.ContainsKey(id) ? dic[id] : 0
                        };
                        retList.Add(temp);
                    }
                    WOPsPartialObject retObject = new WOPsPartialObject()
                    {
                        List = retList,
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        TotalCount = list.Item1,
                        q = q
                    };
                    return PartialView("Sys/_SysWriteOffPointPartial", retObject);
                }  
            }
            return PartialView("Sys/_SysWriteOffPointPartial", new WOPsPartialObject()
            {
                List = new List<WOPPartialObject>(),
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = list.Item1,
                q = q
            });
        }

        /// <summary>
        /// wop的修改与添加也
        /// </summary>
        /// <param name="woid"></param>
        /// <returns></returns>
        private async Task<PartialViewResult> GetWopPartial(Guid woid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return PartialView("ProductPartial/ProductListErrorPartial", $"session null!");

            using (var repo = new BizRepository())
            {
                WriteOffPoint wop = null;
                if (woid.Equals(Guid.Empty))
                {
                    wop = new WriteOffPoint();
                    wop.mid = mer.mid;
                }
                else
                {
                    wop = await repo.GetWOPByWoidAsync(woid);
                }
                
                return PartialView("Sys/_SysAddWOPPartial", wop);
            }
        }

        public async Task<ActionResult> SaveOrUpdateWop(WriteOffPoint postObject)
        {
            try
            {
                string appid = SessionHelper.Get(this, ESessionStateKeys.AppId).ToString();
                if (string.IsNullOrEmpty(appid))
                    return PartialView("ProductPartial/ProductListErrorPartial", $"session null!");

                using (var repo = new BizRepository())
                {
                    if (postObject.woid.Equals(Guid.Empty))
                    {
                        await repo.AddWOPAsync(postObject);
                    }
                    else
                    {
                        await repo.UpdateWOPAsync(postObject);
                    }
                }
                return RedirectToAction("Index", new { tag = (int)EsysTagName.W });
            }
            catch (Exception ex)
            {
                MD.Lib.Log.MDLogger.LogErrorAsync(typeof(SystemController),new Exception($"SaveOrUpdateWop Error:woid:{postObject.woid}", ex));
                return PartialView("ProductPartial/ProductListErrorPartial", $"Save Error!");
            }
        }

        public async Task<PartialViewResult> DelWop(Guid woid,string q)
        {
            string appid = SessionHelper.Get(this, ESessionStateKeys.AppId).ToString();
            if (string.IsNullOrEmpty(appid))
                return PartialView("ProductPartial/ProductListErrorPartial", $"session null!");

            using (var repo = new BizRepository())
            {
                var wop = await repo.GetWOPByWoidAsync(woid);
                if(wop==null)
                    return PartialView("ProductPartial/ProductListErrorPartial", $"没有取到wop，woid:{woid}");
                wop.is_valid = false;
                var list = await repo.GetWOerByWoidAsync(wop.woid);
                if (list != null && list.Count > 0)
                {
                    list.ForEach(async (woer) => {
                        woer.is_valid = false;
                        await repo.UpdateWoerAsync(woer);
                    });
                }
                await repo.UpdateWOPAsync(wop);
                return await GetWopsPartial(q,1);
            }
        }

        #endregion

        #region 核销员

        public class WoerPartialObject
        {
            public Guid woid { get; set; }
            public string appid { get; set; }
            public string name { get; set; }
            public List<WoerPartialDetailObject> List { get; set; }
        }

        public class WoerPartialDetailObject
        {
            public int? id { get; set; }
            public string name { get; set; }
            public string realname { get; set; }
            public string phone { get; set; }

        }
        private async Task<PartialViewResult> GetWoerPartial(Guid woid)
        {
            string appid = SessionHelper.Get(this, ESessionStateKeys.AppId).ToString();
            if (string.IsNullOrEmpty(appid))
                return PartialView("ProductPartial/ProductListErrorPartial", $"session null!");

            if (woid.Equals(Guid.Empty))
                return PartialView("ProductPartial/ProductListErrorPartial", $"woid is empty:{woid}");

            using (var repo = new BizRepository())
            {
                var list = await repo.GetWOerByWoidAsync(woid);
                var wop = await repo.GetWOPByWoidAsync(woid);
                if (list != null && list.Count > 0)
                {
                    List<WoerPartialDetailObject> retList = new List<WoerPartialDetailObject>();
                    foreach (var wo in list)
                    {
                        var user = await repo.UserGetByOpenIdAsync(wo.openid);
                        if (user != null)
                            retList.Add(new WoerPartialDetailObject() { name = user.name, id = wo.id, realname = wo.realname, phone = wo.phone });
                    }
                    WoerPartialObject retObject = new WoerPartialObject()
                    {
                        List = retList,
                        woid = woid,
                        appid = appid,
                        name = wop == null ? "" :wop.name
                    };
                    return PartialView("Sys/_SysAddWOerPartial", retObject);
                }
                return PartialView("Sys/_SysAddWOerPartial", new WoerPartialObject() {woid = woid,appid = appid,name = wop?.name,List = new List<WoerPartialDetailObject>()});
            }
        }

        public async Task<PartialViewResult> DeleteWoer(int? woerid, Guid woid)
        {
            string appid = SessionHelper.Get(this, ESessionStateKeys.AppId).ToString();
            if (string.IsNullOrEmpty(appid))
                return PartialView("ProductPartial/ProductListErrorPartial", $"session null!");

            using (var repo = new BizRepository())
            {
                await repo.WoerDeleteById(woerid);
                return await GetWoerPartial(woid);
            }
        }


        #endregion

        public async Task<PartialViewResult> GetAllWoper(string q)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return PartialView("ProductPartial/ProductListErrorPartial", $"session null!");
            using (var repo = new BizRepository())
            {
                var listWop = await repo.GetWOPsByMidAsync(mer.mid);
                if (listWop != null && listWop.Count > 0)
                {
                    foreach (var item in listWop)
                    {
                        item.listWriteOffer = await repo.GetWOerByWoid2Async(item.woid);
                    }
                    return PartialView("Sys/_SysWopWoperPartial", listWop);
                }
                else
                {
                    return PartialView("Sys/_SysWopWoperPartial", new List<WriteOffPoint>());
                }
            }
        }
    }
}