using LitJson;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.Log;
using MD.Model.Configuration.Aliyun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Backend.Controllers
{
    public class KindEditorController : Controller
    {
        /// <summary>
        /// 上传商家推广图文里面的图片
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="imgFile"></param>
        /// <returns></returns>
        [HttpPost]
        public void UploadImg(Guid pid, HttpPostedFileBase imgFile)
        {
            try
            {
                //定义允许上传的文件扩展名
                Hashtable extTable = new Hashtable();
                extTable.Add("image", "gif,jpg,jpeg,png,bmp");
                extTable.Add("flash", "swf,flv");
                extTable.Add("media", "swf,flv,mp3,wav,wma,wmv,mid,avi,mpg,asf,rm,rmvb");
                extTable.Add("file", "doc,docx,xls,xlsx,ppt,htm,html,txt,zip,rar,gz,bz2");
                //最大文件大小
                int maxSize = 1000000;

                
                //对文件大小和格式进行检查
                if (imgFile != null && imgFile.ContentLength > 0)
                {
                    String fileName = imgFile.FileName;
                    String fileExt = Path.GetExtension(fileName).ToLower();
                    String dirName = Request.QueryString["dir"];
                    if (imgFile.InputStream == null || imgFile.InputStream.Length > maxSize)
                    {
                        showError("上传文件大小超过限制。");
                    }

                    if (String.IsNullOrEmpty(fileExt) || Array.IndexOf(((String)extTable[dirName]).Split(','), fileExt.Substring(1).ToLower()) == -1)
                    {
                        showError("上传文件扩展名是不允许的扩展名。\n只允许" + ((String)extTable[dirName]) + "格式。");
                    }
                }
                //对符合要求的文件上传到阿里云并获取返回的路径
                var path = OssPicPathManager<OssPicBucketConfig>.UploadProductAdvertisPic(pid, imgFile.FileName, imgFile.InputStream);
                //将数据放入JSON，刚KindEditor读取
                Hashtable hash = new Hashtable();
                hash["error"] = 0;
                hash["url"] = path;
                Response.AddHeader("Content-Type", "text/html; charset=UTF-8");
                Response.Write(JsonMapper.ToJson(hash));
                Response.End();
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(callbackController), ex);
                throw ex;
            }
        }
        [HttpPost]
        public void UploadNoticeBoardImg(Guid nid, HttpPostedFileBase imgFile)
        {
            try
            {
                //定义允许上传的文件扩展名
                Hashtable extTable = new Hashtable();
                extTable.Add("image", "gif,jpg,jpeg,png,bmp");
                extTable.Add("flash", "swf,flv");
                extTable.Add("media", "swf,flv,mp3,wav,wma,wmv,mid,avi,mpg,asf,rm,rmvb");
                extTable.Add("file", "doc,docx,xls,xlsx,ppt,htm,html,txt,zip,rar,gz,bz2");
                //最大文件大小
                int maxSize = 1000000;


                //对文件大小和格式进行检查
                if (imgFile != null && imgFile.ContentLength > 0)
                {
                    String fileName = imgFile.FileName;
                    String fileExt = Path.GetExtension(fileName).ToLower();
                    String dirName = Request.QueryString["dir"];
                    if (imgFile.InputStream == null || imgFile.InputStream.Length > maxSize)
                    {
                        showError("上传文件大小超过限制。");
                        return;
                    }

                    if (String.IsNullOrEmpty(fileExt) || Array.IndexOf(((String)extTable[dirName]).Split(','), fileExt.Substring(1).ToLower()) == -1)
                    {
                        showError("上传文件扩展名是不允许的扩展名。\n只允许" + ((String)extTable[dirName]) + "格式。");
                        return;
                    }
                }
                //对符合要求的文件上传到阿里云并获取返回的路径
                var path = OssPicPathManager<OssPicBucketConfig>.UploadNoticeDescriptionPic(nid, imgFile.FileName, imgFile.InputStream);
                //将数据放入JSON，刚KindEditor读取
                Hashtable hash = new Hashtable();
                hash["error"] = 0;
                hash["url"] = path;
                Response.AddHeader("Content-Type", "text/html; charset=UTF-8");
                Response.Write(JsonMapper.ToJson(hash));
                Response.End();
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(callbackController), ex);
                throw ex;
            }
        }

        /// <summary>
        /// 供货管理详情图片
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="imgFile"></param>
        [HttpPost]
        public void UploadSupplyImg(Guid sid, HttpPostedFileBase imgFile)
        {
            try
            {
                //定义允许上传的文件扩展名
                Hashtable extTable = new Hashtable();
                extTable.Add("image", "gif,jpg,jpeg,png,bmp");
                extTable.Add("flash", "swf,flv");
                extTable.Add("media", "swf,flv,mp3,wav,wma,wmv,mid,avi,mpg,asf,rm,rmvb");
                extTable.Add("file", "doc,docx,xls,xlsx,ppt,htm,html,txt,zip,rar,gz,bz2");
                //最大文件大小
                int maxSize = 1000000;



                //对文件大小和格式进行检查
                if (imgFile != null && imgFile.ContentLength > 0)
                {
                    String fileName = imgFile.FileName;
                    String fileExt = Path.GetExtension(fileName).ToLower();
                    String dirName = Request.QueryString["dir"];
                    if (imgFile.InputStream == null || imgFile.InputStream.Length > maxSize)
                    {
                        showError("上传文件大小超过限制。");
                    }

                    if (String.IsNullOrEmpty(fileExt) || Array.IndexOf(((String)extTable[dirName]).Split(','), fileExt.Substring(1).ToLower()) == -1)
                    {
                        showError("上传文件扩展名是不允许的扩展名。\n只允许" + ((String)extTable[dirName]) + "格式。");
                    }
                }
                //对符合要求的文件上传到阿里云并获取返回的路径
                var path = OssPicPathManager<OssPicBucketConfig>.UploadSupplyPic(sid, imgFile.FileName, imgFile.InputStream);
                //将数据放入JSON，刚KindEditor读取
                Hashtable hash = new Hashtable();
                hash["error"] = 0;
                hash["url"] = path;
                Response.AddHeader("Content-Type", "text/html; charset=UTF-8");
                Response.Write(JsonMapper.ToJson(hash));
                Response.End();
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(callbackController), ex);
                throw ex;
            }
        }
        //输出错误信息
        private void showError(string message)
        {
            Hashtable hash = new Hashtable();
            hash["error"] = 1;
            hash["message"] = message;
            Response.AddHeader("Content-Type", "text/html; charset=UTF-8");
            Response.Write(JsonMapper.ToJson(hash));
            Response.End();
        }

        // GET: KindEditor/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }
    }
}
