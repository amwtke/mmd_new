using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Util;
using MD.Model.Configuration.Aliyun;

namespace MD.Lib.Aliyun.OSS.Biz
{
    public static class OssPicPathManager<BucketConfig> where BucketConfig : OssClientConfig, new()
    {
        private static AliyunOssHelper<BucketConfig> _oss= new AliyunOssHelper<BucketConfig>();
        //顶级的目录名称
        private const string MerchantDir = "m";
        private const string UserDir = "u";
        private const string ProductDir = "p";
        private const string SupplyDir = "s";
        private const string GroupDir = "g";
        private const string NoticeBoardDir = "n";
        private const string ActivityDir = "a";
        //下级的目录名
        private const string LicenceDir = "l";
        private const string AlbumDir = "a";
        private const string LogoDir = "logo";
        private const string HeadDir = "h";
        private const string Community = "sq";//用户社区图片
        private const string Comment = "c";//评论
        private const string Generalize = "g";//推广宣传海报

        //固定文件名称
        public const string MerchantQrFileName = "qr.jpg";
        public const string MerchantLogoFileName = "logo.jpg";

        #region get paths

        public static string GetMerchantBizLicencePath(string appid)
        {
            string dirPath = $"{MerchantDir}/{appid}/{LicenceDir}/";
            return dirPath;
        }

        public static string GetMerchantLogoPath(string appid)
        {
            string dirPath = $"{MerchantDir}/{appid}/{LogoDir}/";
            return dirPath;
        }

        public static string GetMerchantAlbumPath(string appid)
        {
            string dirPath = $"{MerchantDir}/{appid}/{AlbumDir}/";
            return dirPath;
        }

        public static string GetUserHeadPicPath(Guid user)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(user);
            string dirPath = $"{UserDir}/{uuid}/{HeadDir}/";
            return dirPath;
        }

        public static string GetUserCommunityPicPath(Guid user)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(user);
            string dirPath = $"{UserDir}/{uuid}/{Community}/";
            return dirPath;
        }

        public static string GetProductAlbumPath(Guid pUuid)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(pUuid);
            string dirPath = $"{ProductDir}/{uuid}/{AlbumDir}/";
            return dirPath;
        }
        public static string GetProductCommentPath(Guid pUuid)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(pUuid);
            string dirPath = $"{ProductDir}/{uuid}/{Comment}/";
            return dirPath;
        }
        public static string GetSupplyAlbumPath(Guid pUuid)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(pUuid);
            string dirPath = $"{SupplyDir}/{uuid}/{AlbumDir}/";
            return dirPath;
        }

        public static string GetGroupAlbumPath(Guid gUuid)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(gUuid);
            string dirPath = $"{GroupDir}/{uuid}/{AlbumDir}/";
            return dirPath;
        }

        public static string GetNoticeBoardThumb_pic(Guid nid)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(nid);
            string dirPath = $"{NoticeBoardDir}/{uuid}/{AlbumDir}/";
            return dirPath;
        }

        public static string GetActivityPath(Guid gUuid)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(gUuid);
            string dirPath = $"{ActivityDir}/{uuid}/{AlbumDir}/";
            return dirPath;
        }

        public static string GetActivityPath(Guid gUuid,string type)
        {
            string uuid = GuidHelper.GuidNoDashLowercase(gUuid);
            string dirPath = $"{ActivityDir}/{type}/{uuid}/{AlbumDir}/";
            return dirPath;
        }
        #endregion

        #region op
        #region Merchant
        /// <summary>
        /// 商家注册时向aliyun上传营业执照的图片
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string UploadBizLicencePic(string appid,string fileName,Stream stream)
        {
            if (string.IsNullOrEmpty(appid))
                return null;
            var path = GetMerchantBizLicencePath(appid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }

        /// <summary>
        /// 商家注册时向aliyun上传logo
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string UploadMerchantLogoPic(string appid, Stream stream)
        {
            if (string.IsNullOrEmpty(appid))
                return null;
            var path = GetMerchantLogoPath(appid) + MerchantLogoFileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }

        /// <summary>
        /// 商家注册时向aliyun上传logo
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string UploadMerchantQrPic(string appid, Stream stream)
        {
            if (string.IsNullOrEmpty(appid))
                return null;
            var path = GetMerchantLogoPath(appid) + MerchantQrFileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }

        /// <summary>
        /// 商家注册时向aliyun上传宣传图片
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string UploadMerchantAdvertisPic(string appid, string fileName, Stream stream)
        {
            if (string.IsNullOrEmpty(appid))
                return null;
            var path = GetMerchantAlbumPath(appid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }
        #endregion

        #region user
        public static string UploadUserCommunityPic(Guid pUuid, string fileName, Stream stream)
        {
            if (pUuid.Equals(Guid.Empty))
                return null;
            var path = GetUserCommunityPicPath(pUuid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }
        #endregion
        /// <summary>
        /// 添加商品时，商品的宣传画
        /// </summary>
        /// <param name="pUuid"></param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string UploadProductAdvertisPic(Guid pUuid, string fileName, Stream stream)
        {
            if (pUuid.Equals(Guid.Empty))
                return null;
            var path = GetProductAlbumPath(pUuid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }
        /// <summary>
        /// 商品评论的图片
        /// </summary>
        /// <param name="pUuid"></param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string UploadProductCommentPic(Guid pUuid, string fileName, Stream stream)
        {
            if (pUuid.Equals(Guid.Empty))
                return null;
            var path = GetProductCommentPath(pUuid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }
        /// <summary>
        /// 供货商品详情图片上传
        /// </summary>
        /// <param name="pUuid"></param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string UploadSupplyPic(Guid pUuid, string fileName, Stream stream)
        {
            if (pUuid.Equals(Guid.Empty))
                return null;
            var path = GetSupplyAlbumPath(pUuid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;

        }

        public static Stream DownloadGuidPdf()
        {
            string filePath = "sql.pdf";
            return _oss.DownloadFile(filePath);
        }

        public static byte[] DownloadGuidPdf2()
        {
            string filePath = "sql.pdf";
            return _oss.DownloadFileBytes(filePath);
        }
        #region Group
        public static string UploadGroupAdvertisPic(Guid gUuid, string fileName, Stream stream)
        {
            if (gUuid.Equals(Guid.Empty))
                return null;
            var path = GetGroupAlbumPath(gUuid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }

        #endregion

        #region NoticeBoard
        /// <summary>
        /// 上传缩略图
        /// </summary>
        /// <param name="nid">主键</param>
        /// <param name="fileName">文件名</param>
        /// <param name="stream">文件Stream</param>
        /// <returns></returns>
        public static string UploadNoticeBoardthumb_pic(Guid nid, string fileName, Stream stream)
        {
            if (nid.Equals(Guid.Empty))
                return null;
            var path = GetNoticeBoardThumb_pic(nid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;
        }
        /// <summary>
        /// 上传文章详情图片路径
        /// </summary>
        /// <param name="pUuid">nid</param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string UploadNoticeDescriptionPic(Guid pUuid, string fileName, Stream stream)
        {
            if (pUuid.Equals(Guid.Empty))
                return null;
            var path = GetNoticeBoardThumb_pic(pUuid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;

        }
        #endregion

        #region Activity
        public static string UploadActivityPic(Guid gUuid, string fileName, Stream stream)
        {
            if (gUuid.Equals(Guid.Empty))
                return null;
            var path = GetActivityPath(gUuid) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;

        }

        public static string UploadActivityPic(Guid gUuid,string type, string fileName, Stream stream)
        {
            if (gUuid.Equals(Guid.Empty))
                return null;
            var path = GetActivityPath(gUuid,type) + fileName;
            var result = _oss.UploadFile(path, stream);
            if (!string.IsNullOrEmpty(result))
            {
                return _oss.GetConfig().CdnUrl + @"/" + path;
            }
            return null;

        }
        #endregion

        public static bool DeletePic(string ossFilePath)
        {
            return _oss.DeleteObject(ossFilePath);
        }
        public static Stream DownloadPic(string ossFilePath)
        {
            return _oss.DownloadFile(ossFilePath);
        }
        #endregion
    }
}
