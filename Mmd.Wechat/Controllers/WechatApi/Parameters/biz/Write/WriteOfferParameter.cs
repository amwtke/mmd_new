using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Write
{
    public class WriteOfferParameter : BaseParameter
    {
        /// <summary>
        /// order表oid
        /// </summary>
        public Guid oid { get; set; }

        /// <summary>
        /// writeoffpoint表woid
        /// </summary>
        public Guid woid { get; set; }

        public string realname { get; set; }
        public string phone { get; set; }
    }
}