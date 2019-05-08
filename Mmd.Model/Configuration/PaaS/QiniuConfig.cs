using MD.Model.Configuration.Att;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration.PaaS
{
    [MDConfig("PaaS", "Qiniu")]
    public class QiniuConfig : IConfigModel
    {
        [MDKey("ACCESS_KEY")]
        public string ACCESS_KEY { get; set; }

        [MDKey("SECRET_KEY")]
        public string SECRET_KEY { get; set; }

        [MDKey("HDPBUCKET")]
        public string HDPBUCKET { get; set; }

        [MDKey("HDPDOMAIN")]
        public string HDPDOMAIN { get; set; }

        [MDKey("ImgBUCKET")]
        public string ImgBUCKET { get; set; }

        [MDKey("ImgDOMAIN")]
        public string ImgDOMAIN { get; set; }

        [MDKey("AttBUCKET")]
        public string AttBUCKET { get; set; }

        [MDKey("AttDOMAIN")]
        public string AttDOMAIN { get; set; }

        [MDKey("EKABUCKET")]
        public string EKABUCKET { get; set; }

        [MDKey("EKADOMAIN")]
        public string EKADOMAIN { get; set; }

        public void Init()
        {
        }
    }
}
