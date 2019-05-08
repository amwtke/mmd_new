using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Util.HttpUtil;
using MD.Model.Configuration.JuHe;
using MD.Model.Configuration.Redis;
using MD.Model.Json;

namespace MD.Lib.Util.AddressUtil
{
    public static class AddressHelper
    {
        private static readonly string url;

        static AddressHelper()
        {
            var config = MD.Configuration.MdConfigurationManager.GetConfig<JuHeConfig>();
            url = @"http://v.juhe.cn/postcode/pcd?key=" + config.PostAppKey;
        }

        public static async Task<AddressFromJuHe> GetDataFromJuHe()
        {
            return await Get.GetJsonAsync<AddressFromJuHe>(url, Encoding.UTF8);
        }
    }
}
