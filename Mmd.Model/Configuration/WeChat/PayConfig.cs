using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;

namespace MD.Model.Configuration.WeChat
{
    [MDConfig("WeChat", "Pay")]
    public class WxPayConfig
    {
        [MDKey("CertDir")]
        public string CertDir { get; set; }

        [MDKey("NotifyCallbackUrl")]
        public string NotifyCallbackUrl { get; set; }
    }
}
