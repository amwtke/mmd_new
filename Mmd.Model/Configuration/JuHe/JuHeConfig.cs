using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;

namespace MD.Model.Configuration.JuHe
{
    [MDConfig("JuHe", "Default")]
    public class JuHeConfig
    {
        /// <summary>
        /// 邮政数据的appkey
        /// </summary>
        [MDKey("PostAppKey")]
        public string PostAppKey { get; set; }

        /// <summary>
        /// 地理位置服务appkey
        /// </summary>
        [MDKey("GEOAppKey")]
        public string GEOAppKey { get; set; }

    }
}
