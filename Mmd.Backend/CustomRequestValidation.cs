using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Util;

namespace Mmd.Backend
{
    public class CustomRequestValidation:RequestValidator
    {
        /// <summary>
        /// 此函数不做任何事，只是为了让Post请求通过验证，对HTML标签的过滤在kindEditor的配置项里面设置的
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="requestValidationSource"></param>
        /// <param name="collectionKey"></param>
        /// <param name="validationFailureIndex"></param>
        /// <returns></returns>
        protected override bool IsValidRequestString(HttpContext context, string value, RequestValidationSource requestValidationSource, string collectionKey, out int validationFailureIndex)
        {
            validationFailureIndex = 0;
            return true;
        }
    }
}