using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MD.Lib.Util
{
    public static class CommonHelper
    {
        public const string JWTsecretKey = "1E39429F105B41D897090B69694D9F2B63FB38B4A7E44FC4A04052074DE09D9A8E0E13487B6C4215BDFE089E9D5F878E";
        public static string GetLocalIp()
        {
            string hostname = Dns.GetHostName();
            IPHostEntry localhost = Dns.GetHostEntry(hostname);
            IPAddress localaddr = localhost.AddressList[1];
            return localaddr.ToString();
        }

        public static System.DateTime FromUnixTime(double d)
        {
            System.DateTime time = System.DateTime.MinValue;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            time = startTime.AddSeconds(d);
            return time;
        }
        /// <summary>
        /// 将c# DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns>double</returns>
        public static double ToUnixTime(System.DateTime time)
        {
            double intResult = 0;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            intResult = (time - startTime).TotalSeconds;
            return intResult;
        }

        public static double GetUnixTimeNow()
        {
            return ToUnixTime(DateTime.Now);
        }

        /// <summary>
        /// 通过文件流获取md5码
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public static string GetFileMD5String(Stream fileStream)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(fileStream);
                return Encoding.Default.GetString(bytes);
            }
        }
        public static string GetMD5(string myString)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] fromData = System.Text.Encoding.Unicode.GetBytes(myString);
            byte[] targetData = md5.ComputeHash(fromData);
            string byte2String = null;

            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String += targetData[i].ToString("x");
            }

            return byte2String;
        }
        /// <summary>
        /// 生成N个不重复的随机数
        /// </summary>
        /// <param name="from">要生成的随机数下线</param>
        /// <param name="to">要生成的随机数下线</param>
        /// <param name="number">生成个数</param>
        /// <returns>N个不重复的随机数</returns>
        public static List<int> GetRandomList(int from, int to, int number)
        {
            if (number > 0)
            {
                var list = new List<int>();
                while (list.Count < number)
                {
                    var temp = GetRandomNumber(from, to);
                    if (!list.Contains(temp))
                        list.Add(temp);
                }
                return list;
            }
            return null;
        }
        /// <summary>
        /// 生成1个随机数
        /// </summary>
        /// <param name="from">要生成的随机数下界</param>
        /// <param name="to">要生成的随机数上界（随机数不能取该上界值）</param>
        /// <returns>1个随机数</returns>
        public static int GetRandomNumber(int from, int to)
        {
            return new Random().Next(from, to);
        }

        public static T GenFromParent<T>(object parent) where T : class, new()
        {
            Type t = parent.GetType();
            if (typeof(T).BaseType == t)
            {
                T ret = new T();
                foreach (var p in t.GetProperties())
                {
                    var v = t.GetProperty(p.Name).GetValue(parent);
                    if (v != null)
                    {
                        p.SetValue(ret, v);
                    }
                }
                return (T)ret;
            }
            return new T();
        }

        /// <summary>
        /// 去掉字符串中的字母和数字，只保留文字
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string ReplaceHtmlStr(string html)
        {
            Regex reg = new Regex("[^\\x00-\\xff]");
            var matches = reg.Matches(html);
            string res = "";
            foreach (var item in matches)
            {
                res += item.ToString();
            }
            return res;
        }

        #region 本机信息
        public static string GetHostName()
        {
            return Environment.MachineName;
        }

        public static string GetLoggerName(Type t)
        {
            return t.ToString();
        }

        public static string GetThreadId()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
        }

        public static string GetOSName()
        {
            return Environment.OSVersion.ToString();
        }

        public static string GetDomain()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

        public static string GetLoggerDateTime(DateTime dt)
        {
            return dt.ToString("O");
        }
        #endregion

        #region 时间
        /// <summary>
        /// 返回14位的时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetNowyyyyMMddHHmmss()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        /// <summary>
        /// 生成32位的id，可以用于团订单与用户订单，兼容微信支付的outtradeno字段
        /// out_trade_no	String(32)	20150806125346	商户系统内部的订单号，当没提供transaction_id时需要传这个。
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string GetId32(EIdPrefix prefix)
        {
            var ret = prefix.ToString() + GetNowyyyyMMddHHmmss() + GuidHelper.GuidTo16String2();
            return ret;
        }
        #endregion
    }

    public enum EIdPrefix
    {
        /// <summary>
        /// 团购订单GroupOrder
        /// </summary>
        GO = 0,//团购订单GroupOrder

        /// <summary>
        /// 用户订单Order
        /// </summary>
        OD = 1,//用户订单Order
        /// <summary>
        /// 汇款单号
        /// </summary>
        RF = 2,
    }
}
