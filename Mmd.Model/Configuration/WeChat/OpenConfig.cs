using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;

namespace MD.Model.Configuration
{
    [MDConfig("WeChat", "Open")]
    public class WXOpenConfig
    {
        [MDKey("AppId")]
        public string AppId { get; set; }

        [MDKey("AppSecret")]
        public string AppSecret { get; set; }

        [MDKey("Token")]
        public string Token { get; set; }

        [MDKey("EncodingAESKey")]
        public string EncodingAESKey { get; set; }

        [MDKey("BizDomainUrlPatten")]
        public string BizDomainUrlPatten { get; set; }

        [MDKey("JsSaftyDomainPatten")]
        public string JsSaftyDomainPatten { get; set; }

        [MDKey("PayDirPatten")]
        public string PayDirPatten { get; set; }
    }
}
