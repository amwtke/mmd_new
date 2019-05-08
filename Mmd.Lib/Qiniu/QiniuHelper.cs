using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using Qiniu.Conf;
using Qiniu.IO;
using Qiniu.IO.Resumable;
using Qiniu.RPC;
using Qiniu.RS;

namespace MD.Lib.Qiniu
{
    public class QiniuHelper
    {
        protected string Bucket = "";
        protected string DOMAIN = "";

        private static bool init = false;
        private static Model.Configuration.PaaS.QiniuConfig config = Configuration.MdConfigurationManager.GetConfig<Model.Configuration.PaaS.QiniuConfig>();
        private void Init()
        {
            if(!init)
            {
                if(config == null)
                    Util.JsonResponseHelper.HttpRMtoJson(null, System.Net.HttpStatusCode.OK, Util.ECustomStatus.Fail);
                else
                {
                    Config.ACCESS_KEY = config.ACCESS_KEY;
                    Config.SECRET_KEY = config.SECRET_KEY;
                    init = true;
                }
            }

        }

        public QiniuHelper()
        {
            Init();
            Bucket = config.HDPBUCKET;
            DOMAIN = config.HDPDOMAIN;
        }

        public QiniuHelper(QiniuBucket QB)
        {
            Init();
            if(QB == QiniuBucket.Att)
            {
                Bucket = config.AttBUCKET;
                DOMAIN = config.AttDOMAIN;
            }
            else if(QB == QiniuBucket.Img)
            {
                Bucket = config.ImgBUCKET;
                DOMAIN = config.ImgDOMAIN;
            }
            else if(QB == QiniuBucket.Headpic)
            {
                Bucket = config.HDPBUCKET;
                DOMAIN = config.HDPDOMAIN;
            }
            else if(QB == QiniuBucket.EKArticle)
            {
                Bucket = config.EKABUCKET;
                DOMAIN = config.EKADOMAIN;
            }

        }

        public QiniuHelper(string bucket, string domain)
        {
            Init();
            Bucket = bucket;
            DOMAIN = domain;
        }

        /// <summary>
        /// 查看单个文件属性信息
        /// </summary>
        /// <param name="bucket">七牛云存储空间名</param>
        /// <param name="key">文件key</param>
        public static void Stat(string bucket, string key)
        {
            RSClient client = new RSClient();
            Entry entry = client.Stat(new EntryPath(bucket, key));
            if(entry.OK)
            {
                Console.WriteLine("Hash: " + entry.Hash);
                Console.WriteLine("Fsize: " + entry.Fsize);
                Console.WriteLine("PutTime: " + entry.PutTime);
                Console.WriteLine("MimeType: " + entry.MimeType);
                Console.WriteLine("Customer: " + entry.Customer);
            }
            else
            {
                Console.WriteLine("Failed to Stat");
            }
        }


        /// <summary>
        /// 复制单个文件
        /// </summary>
        /// <param name="bucketSrc">需要复制的文件所在的空间名</param>
        /// <param name="keySrc">需要复制的文件key</param>
        /// <param name="bucketDest">目标文件所在的空间名</param>
        /// <param name="keyDest">标文件key</param>
        public static void Copy(string bucketSrc, string keySrc, string bucketDest, string keyDest)
        {
            RSClient client = new RSClient();
            CallRet ret = client.Copy(new EntryPathPair(bucketSrc, keySrc, bucketDest, keyDest));
            if(ret.OK)
            {
                Console.WriteLine("Copy OK");
            }
            else
            {
                Console.WriteLine("Failed to Copy");
            }
        }


        /// <summary>
        /// 移动单个文件
        /// </summary>
        /// <param name="bucketSrc">需要移动的文件所在的空间名</param>
        /// <param name="keySrc">需要移动的文件</param>
        /// <param name="bucketDest">目标文件所在的空间名</param>
        /// <param name="keyDest">目标文件key</param>
        public static void Move(string bucketSrc, string keySrc, string bucketDest, string keyDest)
        {
            Console.WriteLine("\n===> Move {0}:{1} To {2}:{3}",
            bucketSrc, keySrc, bucketDest, keyDest);
            RSClient client = new RSClient();
            new EntryPathPair(bucketSrc, keySrc, bucketDest, keyDest);
            CallRet ret = client.Move(new EntryPathPair(bucketSrc, keySrc, bucketDest, keyDest));
            if(ret.OK)
            {
                Console.WriteLine("Move OK");
            }
            else
            {
                Console.WriteLine("Failed to Move");
            }
        }

        /// <summary>
        /// 删除单个文件
        /// </summary>
        /// <param name="bucket">文件所在的空间名</param>
        /// <param name="key">文件key</param>
        public static void Delete(string bucket, string key)
        {
            Console.WriteLine("\n===> Delete {0}:{1}", bucket, key);
            RSClient client = new RSClient();
            CallRet ret = client.Delete(new EntryPath(bucket, key));
            if(ret.OK)
            {
                Console.WriteLine("Delete OK");
            }
            else
            {
                Console.WriteLine("Failed to delete");
            }
        }

        public static void BatchStat(string bucket, string[] keys)
        {
            RSClient client = new RSClient();
            List<EntryPath> EntryPaths = new List<EntryPath>();
            foreach(string key in keys)
            {
                Console.WriteLine("\n===> Stat {0}:{1}", bucket, key);
                EntryPaths.Add(new EntryPath(bucket, key));
            }
            client.BatchStat(EntryPaths.ToArray());
        }


        public static void BatchCopy(string bucket, string[] keys)
        {
            List<EntryPathPair> pairs = new List<EntryPathPair>();
            foreach(string key in keys)
            {
                EntryPathPair entry = new EntryPathPair(bucket, key, Guid.NewGuid().ToString());
                pairs.Add(entry);
            }
            RSClient client = new RSClient();
            client.BatchCopy(pairs.ToArray());
        }


        public static void BatchMove(string bucket, string[] keys)
        {
            List<EntryPathPair> pairs = new List<EntryPathPair>();
            foreach(string key in keys)
            {
                EntryPathPair entry = new EntryPathPair(bucket, key, Guid.NewGuid().ToString());
                pairs.Add(entry);
            }
            RSClient client = new RSClient();
            client.BatchMove(pairs.ToArray());
        }


        public static void BatchDelete(string bucket, string[] keys)
        {
            RSClient client = new RSClient();
            List<EntryPath> EntryPaths = new List<EntryPath>();
            foreach(string key in keys)
            {
                Console.WriteLine("\n===> Stat {0}:{1}", bucket, key);
                EntryPaths.Add(new EntryPath(bucket, key));
            }
            client.BatchDelete(EntryPaths.ToArray());
        }

        /// <summary>
        /// 上传文件测试
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="key"></param>
        /// <param name="fname"></param>
        public static void PutFile(string bucket, string key, string fname)
        {
            PutPolicy policy = new PutPolicy(bucket);
            string upToken = policy.Token();
            PutExtra extra = new PutExtra();
            IOClient client = new IOClient();
            PutRet ret = client.PutFile(upToken, key, fname, extra);
            if(ret.OK)
            {

            }
        }

        public bool PutFile(string key, Stream fs)
        {
            PutPolicy policy = new PutPolicy(Bucket);
            string upToken = policy.Token();
            PutExtra extra = new PutExtra();
            IOClient client = new IOClient();
            PutRet ret = client.Put(upToken, key, fs, extra);
            if(ret.OK)
            {
                return true;
            }
            else
            {
                throw new Exception("Exception: ret.Responce:" + ret.Response + " ret.Exception:" + ret.Exception.ToString().Replace("\r", "").Replace("\n", "") + " ret.StatusCode:" + ret.StatusCode);
            }
        }

        public bool PutFile(string key, string file)
        {
            PutPolicy policy = new PutPolicy(Bucket);
            string upToken = policy.Token();
            PutExtra extra = new PutExtra();
            IOClient client = new IOClient();
            PutRet ret = client.PutFile(upToken, key, file, extra);
            if(ret.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Delete(string key)
        {
            RSClient client = new RSClient();
            CallRet ret = client.Delete(new EntryPath(Bucket, key));
            if(ret.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool FileStat(string key)
        {
            RSClient client = new RSClient();
            Entry entry = client.Stat(new EntryPath(Bucket, key));
            return entry.OK;
        }

        public string GetFileUrl(string key)
        {
            return "HTTP://" + DOMAIN + "/" + key;
        }

        public static void ResumablePutFile(string bucket, string key, string fname)
        {
            Console.WriteLine("\n===> ResumablePutFile {0}:{1} fname:{2}", bucket, key, fname);
            PutPolicy policy = new PutPolicy(bucket, 3600);
            string upToken = policy.Token();
            Settings setting = new Settings();
            ResumablePutExtra extra = new ResumablePutExtra();
            ResumablePut client = new ResumablePut(setting, extra);
            client.PutFile(upToken, fname, Guid.NewGuid().ToString());

        }
    }
    public enum QiniuBucket
    {
        Img,
        Att,
        Headpic,
        EKArticle
    }
}