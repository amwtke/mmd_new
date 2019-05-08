using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aliyun.OSS;
using Aliyun.OSS.Common;
using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Aliyun;

namespace MD.Lib.Aliyun.OSS
{
    public class AliyunOssHelper<BucketConfig> where BucketConfig:OssClientConfig, new()
    {
        private readonly OssClientConfig _config;
        private readonly OssClient client;
        private static readonly ConcurrentDictionary<Type, OssClient> _dic = new ConcurrentDictionary<Type, OssClient>();
        static object lockObject = new object(); 

        public AliyunOssHelper()
        {
            _config = MdConfigurationManager.GetConfig<BucketConfig>();
            if (_config == null)
                throw new MDException(typeof(AliyunOssHelper<BucketConfig>), "OSS配置获取失败！");

            Type t = typeof(BucketConfig);
            if (!_dic.TryGetValue(t, out client))
            {
                lock (lockObject)
                {
                    if (!_dic.TryGetValue(t, out client))
                    {
                        try
                        {
                            //初始化客户端
                            var conf = new ClientConfiguration {IsCname = true};
                            Uri cNameUri = new Uri(_config.CNameUrl);
                            client = new OssClient(cNameUri, _config.AppKey, _config.Secret, conf);
                            _dic[t] = client;
                        }
                        catch (Exception ex)
                        {
                            MDLogger.LogErrorAsync(typeof(AliyunOssHelper<BucketConfig>), ex);
                            throw ex;
                        }
                    }
                }
            }

            init();
        }

        void init()
        {
            //确保bucket存在
            string bucketName = _config.BucketName;
            if (!client.DoesBucketExist(bucketName))
                client.CreateBucket(bucketName);
        }

        #region Operations

        public OssClientConfig GetConfig()
        {
            return _config;
        }

        public string UploadFile(string ossFilePath, Stream stream)
        {

            try
            {
                //var result = client.PutObject(_config.BucketName, ossFilePath, stream);
                ObjectMetadata om = new ObjectMetadata();
                om.CacheControl = "max-age=2592000";
                var result = client.PutBigObject(_config.BucketName, ossFilePath, stream, om);
                return result.ETag;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(AliyunOssHelper<BucketConfig>), ex);
                throw ex;
            }
        }

        public void UploadFileAsync(string ossFilePath, Stream stream, AsyncCallback callBackFunc, object state)
        {

            try
            {
                var result = client.BeginPutObject(_config.BucketName, ossFilePath, stream, callBackFunc, state);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(AliyunOssHelper<BucketConfig>), ex);
                throw ex;
            }
        }

        public void UploadFileAsync(string ossFilePath, Stream stream)
        {

            try
            {
                var result = client.BeginPutObject(_config.BucketName, ossFilePath, stream, PutObjectCallback, null);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(AliyunOssHelper<BucketConfig>), ex);
                throw ex;
            }
        }

        private void PutObjectCallback(IAsyncResult ar)
        {
            try
            {
                var result = client.EndPutObject(ar);
                //Console.WriteLine("ETag:{0}", result.ETag);
                //Console.WriteLine("User Parameter:{0}", ar.AsyncState as string);
                //Console.WriteLine("Put object succeeded");
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(AliyunOssHelper<BucketConfig>), ex);
            }
        }

        public OssClient GetClient()
        {
            return client;
        }

        public Stream DownloadFile(string ossFilePath)
        {
            try
            {
                var obj = client.GetObject(_config.BucketName, ossFilePath);
                return obj.Content;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(AliyunOssHelper<BucketConfig>), ex);
            }
            return null;
        }

        public byte[] DownloadFileBytes(string ossFilePath)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var obj = client.GetObject(_config.BucketName, ossFilePath);
                    using (var requestStream = obj.Content)
                    {
                        byte[] buf = new byte[1024];
                        int len = 0;
                        while ((len = requestStream.Read(buf, 0, 1024)) != 0)
                        {
                            ms.Write(buf, 0, len);
                        }
                    }
                    return ms.GetBuffer();
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(AliyunOssHelper<BucketConfig>), ex);
            }
            return null;
        }

        /// <summary>
        /// 删除测试数据时清除图片
        /// </summary>
        /// <param name="ossFilePath"></param>
        /// <returns></returns>
        public bool DeleteObject(string ossFilePath)
        {
            try
            {
                client.DeleteObject(_config.BucketName, ossFilePath);
                return true;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(AliyunOssHelper<BucketConfig>), ex);
            }
            return false;
        }
        #endregion
    }
}
