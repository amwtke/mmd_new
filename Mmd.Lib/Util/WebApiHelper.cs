using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MD.Lib.Util
{
    public class JsonResponseHelper
    {

        //public static HttpResponseMessage HttpRMtoJson(object obj, HttpStatusCode statusCode, ECustomStatus customStatus)
        //{
        //    string str;
        //    ResponseJsonMessage rjm = new ResponseJsonMessage(customStatus.ToString(), obj);
        //    JavaScriptSerializer serializer = new JavaScriptSerializer();
        //    str = serializer.Serialize(rjm);
        //    HttpResponseMessage result = new HttpResponseMessage() { StatusCode = statusCode, Content = new StringContent(str, Encoding.GetEncoding("UTF-8"), "application/json") };
        //    return result;
        //}
        public static HttpResponseMessage HttpRMtoJson(object obj, HttpStatusCode statusCode, ECustomStatus customStatus)
        {
            string str;
            ResponseJsonMessage rjm = new ResponseJsonMessage(customStatus.ToString(), obj);
            str = JsonConvert.SerializeObject(rjm);
            HttpResponseMessage result = new HttpResponseMessage() { StatusCode = statusCode, Content = new StringContent(str, Encoding.GetEncoding("UTF-8"), "application/json") };
            return result;
        }
        public static HttpResponseMessage HttpRMtoJson(string jsonpCallback, object obj, HttpStatusCode statusCode, ECustomStatus customStatus)
        {
            string str;
            ResponseJsonMessage rjm = new ResponseJsonMessage(customStatus.ToString(), obj);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            if(string.IsNullOrEmpty(jsonpCallback))
                str = serializer.Serialize(rjm);
            else
                str = jsonpCallback + "(" + serializer.Serialize(rjm) + ");";
            HttpResponseMessage result = new HttpResponseMessage() { StatusCode = statusCode, Content = new StringContent(str, Encoding.GetEncoding("UTF-8"), "application/json") };
            return result;
        }
        [Serializable]
        public class ResponseJsonMessage
        {
            public string CustomStatus = "";
            public object Message = null;
            public ResponseJsonMessage(string customStatus, object message)
            {
                CustomStatus = customStatus;
                Message = message;
            }
        }

        public static HttpResponseMessage ServerError(string errorMsg)
        {
            return HttpRMtoJson(new { errorMsg = errorMsg }, HttpStatusCode.InternalServerError, ECustomStatus.Fail);
        }
    }

    public enum ECustomStatus
    {
        InvalidArguments,
        Forbidden,
        Inactive,
        Success,
        WrongPassowrd,
        NotFound,
        AccountExist,
        Fail,
        ErrorValidationCode,
        NoValidationCode,
        #region 拼团的状态
        /// <summary>
        /// 库存不足，无法开团
        /// </summary>
        PT_FAIL_KCBZ,

        /// <summary>
        /// 拼团成功
        /// </summary>
        PT_OK,
        /// <summary>
        /// 用户没有关注
        /// </summary>
        UserNotSubscribe
        #endregion
    }

}
