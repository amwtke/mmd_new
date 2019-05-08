using Qiniu.Conf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qiniu
{
    class QiniuHelper
    {
        protected static string Bucket = "";
        protected static string DOMAIN = "qiniuphotos.qiniudn.com";
        protected static string NewKey
        {
            get { return Guid.NewGuid().ToString(); }
        }
        private static bool init = false;

        public QiniuHelper()
        {
            Init();
        }

        //test
        private void Init()
        {
            //zhuzhenhua 2016-07-18 15:25
            //zhuzhenhua 2016-07-18 15:55
            if (init)
                return;

            //for make test

            Config.ACCESS_KEY = "r3_0PzYByEpjj1nFIFkmo1wIqHGuRzdgckeBplji";
            Config.SECRET_KEY = "YKWYVUm-DqE4xFWfgLbWWke5hvsbEe5G-iw-QJfL";
            Bucket = "eksns-img";
            DOMAIN = "7xme7z.com2.z0.glb.qiniucdn.com";

            init = true;
        }

    }
}
