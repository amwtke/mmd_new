using MD.Model.Configuration.Att;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration
{
    [MDConfig("WeChat", "Default")]
    public class WeixinConfig : IConfigModel
    {
        [MDKey("WeixinToken")]
        public string WeixinToken { get; set; }

        [MDKey("WeixinEncodingAESKey")]
        public string WeixinEncodingAesKey { get; set; }

        [MDKey("WeixinEncodingAESKey")]
        public string WeixinEncodingAESKey { get; set; }

        [MDKey("WeixinAppId")]
        public string WeixinAppId { get; set; }

        [MDKey("WeixinAppSecret")]
        public string WeixinAppSecret { get; set; }

        public void Init()
        {
        }
    }
}
