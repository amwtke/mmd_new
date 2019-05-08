using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;

namespace MD.Model.Configuration.Aliyun
{
    public abstract class OssClientConfig
    {
        [MDKey("AppKey")]
        public string AppKey { get; set; }
        [MDKey("Secret")]
        public string Secret { get; set; }
        [MDKey("BucketName")]
        public string BucketName { get; set; }

        [MDKey("EndPointW")]
        public string EndPointW { get; set; }

        [MDKey("EndPointN")]
        public string EndPointN { get; set; }

        [MDKey("CdnUrl")]
        public string CdnUrl { get; set; }

        [MDKey("CNameUrl")]
        public string CNameUrl { get; set; }
    }
}
