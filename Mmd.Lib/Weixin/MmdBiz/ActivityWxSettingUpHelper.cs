using MD.Lib.Weixin.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Weixin.MmdBiz
{
  public static  class ActivityWxSettingUpHelper
    {
        /// <summary>
        /// 生成宝箱核销的URL
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        public static string GenWriteOffFindBoxUrl(string appid, string utid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, utid, "state", "WriteOff", "WriteOffFindBox");
            return url;
        }

    }
}
