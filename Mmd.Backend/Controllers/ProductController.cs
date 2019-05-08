using MD.Lib.DB.Repositorys;
using MD.Model.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.ElasticSearch.MD;
using MD.Model.Configuration.Aliyun;
using MD.Model.Configuration.UI;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using MD.Model.Index.MD;

namespace Mmd.Backend.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        [HttpGet]
        public async Task<ActionResult> Add_Product(Guid pid)
        {
            try
            {
                //获取商家用户名
                Merchant mer = await SessionHelper.GetMerchant(this);
                if (mer == null)
                {
                    return RedirectToAction("SessionTimeOut", "Session");
                }
                ViewBag.merName = mer.name;
                ViewBag.mid = mer.mid.ToString();
                Product prod;

                if (pid.Equals(Guid.Empty))//新增
                {
                    prod = new Product();
                    prod.pid = Guid.NewGuid();
                    return View(prod);
                }
                //编辑
                using (BizRepository repo = new BizRepository())
                {
                    prod = await repo.GetProductByPidAsync(pid);
                    if (prod != null && !prod.pid.Equals(Guid.Empty))
                    {
                        return View(prod);
                    }
                    return Content($"pid is not avalid!");
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(ProductController), ex);
                throw ex;
            }
        }
        /// <summary>
        /// 将商家产品宣传图片上传至阿里云，将其他数据保存至数据库
        /// </summary>
        /// <param name="product"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ProductSave(Product product, HttpPostedFileBase pic1, HttpPostedFileBase pic2, HttpPostedFileBase pic3)
        {
            try
            {
                if (product.pid.Equals(Guid.Empty))
                    return Content("pid is null!");

                string fileName = CommonHelper.GetUnixTimeNow().ToString();
                //上传第一张图片，并获取路径
                if (pic1 != null && pic1.ContentLength > 0)
                {
                    var path1 = OssPicPathManager<OssPicBucketConfig>.UploadProductAdvertisPic(product.pid, fileName.ToString() + "_1", pic1.InputStream);
                    if (!string.IsNullOrEmpty(path1))
                        product.advertise_pic_1 = path1;
                    else
                        throw new MDException(typeof(callbackController), "上传文件失败！");
                    product.advertise_pic_1 = path1;
                }

                //上传第二张图片，并获取路径
                if (pic2 != null && pic2.ContentLength > 0)
                {
                    var path2 = OssPicPathManager<OssPicBucketConfig>.UploadProductAdvertisPic(product.pid, fileName.ToString() + "_2", pic2.InputStream);
                    if (!string.IsNullOrEmpty(path2))
                        product.advertise_pic_2 = path2;
                    else
                        throw new MDException(typeof(callbackController), "上传文件失败！");
                    product.advertise_pic_2 = path2;
                }
                //上传第三张图片，并获取路径
                if (pic3 != null && pic3.ContentLength > 0)
                {
                    var path3 = OssPicPathManager<OssPicBucketConfig>.UploadProductAdvertisPic(product.pid, fileName.ToString() + "_3", pic3.InputStream);
                    if (!string.IsNullOrEmpty(path3))
                        product.advertise_pic_3 = path3;
                    else
                        throw new MDException(typeof(callbackController), "上传文件失败！");
                    product.advertise_pic_3 = path3;
                }
                //图片上传成功，获得三张图片的路径，将由表单得到的数据存入BizRepository中
                using (BizRepository repo = new BizRepository())
                {
                    //将数据存入DB
                    await repo.SaveOrUpdateProductAsync(product);
                    //数据存入之后，将存完数据后的对象取出来
                    var prod = await repo.GetProductByPidAsync(product.pid);

                    //存入ES
                    var pEs = await EsProductManager.GenObject(prod.pid);
                    if (!await EsProductManager.AddOrUpdateAsync(pEs))
                    {
                        return Content("Es failed!");
                    }

                    //重定向到另一个页面
                    return RedirectToAction("ProductList");
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(ProductController), new Exception($"fun:ProductSave,pid:{product.pid},ex:{ex.Message}"));
                throw ex;
            }
        }

        public async Task<ActionResult> ProductList()
        {
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(ProductController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);

            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            using (BizRepository repo = new BizRepository())
            {
                int count = await repo.GetProductCountAsync(mer.wx_appid);
                ViewBag.totalCount = count;
                ViewBag.pageSize = pageSize;
                ViewBag.merName = mer.name;

                if (count % pageSize == 0)
                    ViewBag.totalPages = count / pageSize;
                else
                    ViewBag.totalPages = count / pageSize + 1;

                return View(mer);
            }
        }

        public async Task<ActionResult> CommentList(Guid id)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            }
            ViewBag.pid = id;
            return View();
        }

        public async Task<PartialViewResult> CommentListPartial(string q, Guid pid, int pageNo)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            }
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(ProductController), "UiBackEndConfig 没取到！");
            int size = int.Parse(uiConfig.PageSize);
            var tuple = await EsProductCommentManager.SearchAsync(q, pid, pageNo, size);
            var listComments = tuple.Item2;
            List<CommentView> listView = new List<CommentView>();
            ViewBag.pageSize = size;
            ViewBag.pageNo = pageNo;
            ViewBag.totalCount = tuple.Item1;
            if (tuple != null && listComments.Count > 0)
            {
                using (var codebiz = new CodeRepository())
                {
                    foreach (var item in listComments)
                    {
                        var commentView = CommonHelper.GenFromParent<CommentView>(item);
                        commentView.u_skinStr = await codebiz.GetCodeSkin(item.u_skin);
                        var user = await EsUserManager.GetByIdAsync(Guid.Parse(item.uid));
                        commentView.u_name = user == null ? "" : user.name;
                        var order = await EsOrderManager.GetOrderByUid(item.uid);
                        if (order != null)
                        {
                            commentView.cellphone = order.cellphone;
                            commentView.realname = order.name;
                        }
                        listView.Add(commentView);
                    }
                }
                return PartialView("ProductPartial/ProductCommentsPartial", listView);
            }
            return PartialView("ProductPartial/ProductCommentsPartial", new List<CommentView>());
        }

        public class CommentView : IndexProductComment
        {
            public string u_skinStr { get; set; }
            public string u_name { get; set; }
            public string realname { get; set; }
            public string cellphone { get; set; }
        }

        public async Task<PartialViewResult> ProductListPartial(string q, int pageNo)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                //MDLogger.LogErrorAsync(typeof(ProductController), new Exception("Session null!"));
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            }

            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(ProductController), "UiBackEndConfig 没取到！");
            int size = int.Parse(uiConfig.PageSize);

            //无搜索
            if (string.IsNullOrEmpty(q))
            {
                using (BizRepository repo = new BizRepository())
                {
                    var tuple = await repo.GetProductsByAppidAsync(mer.wx_appid, pageNo, size);
                    if (tuple != null && tuple.Item2.Count > 0)
                    {
                        ViewBag.pageSize = size;
                        ViewBag.pageNo = pageNo;
                        ViewBag.totalCount = tuple.Item1;
                        ViewBag.appid = mer.wx_appid;
                        return PartialView("ProductPartial/ProductListPartial", tuple.Item2);
                    }
                }
                //如果没有数据
                ViewBag.pageSize = size;
                ViewBag.pageNo = 1;
                ViewBag.appid = mer.wx_appid;
                ViewBag.totalCount = 0;
                return PartialView("ProductPartial/ProductListPartial", new List<Product>());
            }
            else
            {
                var tuple = await EsProductManager.Search(q, mer.mid, pageNo, size);
                ViewBag.pageSize = size;
                ViewBag.pageNo = pageNo;
                ViewBag.appid = mer.wx_appid;
                ViewBag.totalCount = tuple.Item1;

                if (tuple.Item2.Count > 0)
                {
                    using (var repo = new BizRepository())
                    {
                        List<Product> ret = await repo.GetProductsFromIndex(tuple.Item2);
                        return PartialView("ProductPartial/ProductListPartial", ret);
                    }
                }
                //如果没有数据
                ViewBag.pageSize = size;
                ViewBag.pageNo = 1;
                ViewBag.appid = mer.wx_appid;
                ViewBag.totalCount = 0;
                return PartialView("ProductPartial/ProductListPartial", new List<Product>());
            }
        }

        public async Task<PartialViewResult> DelProduct(Guid pid, string q)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");
            }

            using (var repo = new BizRepository())
            {
                await repo.DeleteProductAsync(pid);

                //更新es

                if (!await EsProductManager.AddOrUpdateAsync(await EsProductManager.GenObject(pid)))
                {
                    //MDLogger.LogErrorAsync(typeof(ProductController), new Exception("delproduct中的ES更新失败!"));
                    throw new MDException(typeof(ProductController), "delproduct中的ES更新失败!");
                }

                //获取pagesize的配置
                UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
                if (uiConfig == null)
                    throw new MDException(typeof(ProductController), "UiBackEndConfig 没取到！");
                int pageSize = int.Parse(uiConfig.PageSize);

                //刷新第一页
                return await ProductListPartial(q, 1);
            }
        }

        public async Task<JsonResult> DelComment(Guid pcid)
        {
            using (var repo = new BizRepository())
            {
                bool res = await repo.DelCommentAsync(pcid);
                return Json(new { status = res, message = "success" });
            }
        }

        public async Task<JsonResult> SetTop(Guid pcid, int isessence)
        {
            using (var repo = new BizRepository())
            {
                var comment = await repo.GetProductCommentByPCidAsync(pcid);
                if (comment != null)
                {
                    comment.isessence = isessence;
                    await repo.SaveOrUpdateProductCommentAsync(comment);
                    var indexComment = EsProductCommentManager.GenObject(comment);
                    await EsProductCommentManager.AddOrUpdateAsync(indexComment);
                }
                return Json(new { status = "success", message = "success" });
            }
        }

        public async Task<JsonResult> ReplyComment(Guid pcid, string reply)
        {
            using (var repo = new BizRepository())
            {
                var comment = await repo.GetProductCommentByPCidAsync(pcid);
                if (comment != null)
                {
                    comment.comment_reply = reply;
                    comment.timestamp_reply = CommonHelper.GetUnixTimeNow();
                    await repo.SaveOrUpdateProductCommentAsync(comment);
                    var indexComment = EsProductCommentManager.GenObject(comment);
                    await EsProductCommentManager.AddOrUpdateAsync(indexComment);
                }
                return Json(new { status = "success", message = "success" });
            }
        }
    }
}