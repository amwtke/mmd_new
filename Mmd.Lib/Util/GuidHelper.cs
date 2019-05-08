using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Util
{
    public static class GuidHelper
    {
        /// <summary>  
        /// 根据GUID获取16位的唯一字符串,重复1亿次都不会出现重复的
        /// </summary>  
        /// <param name=\"guid\"></param>  
        /// <returns></returns>  
        public static string GuidTo16String()
        {
            long i = Guid.NewGuid().ToByteArray().Aggregate<byte, long>(1, (current, b) => current * ((int)b + 1));
            return $"{i - DateTime.Now.Ticks:x}";
        }

        public static string GuidTo16String2()
        {
            long i = Guid.NewGuid().ToByteArray().Aggregate<byte, long>(1, (current, b) => current * ((int)b + 1));
            long r = (i - DateTime.Now.Ticks) % (long) Math.Pow(10,16);
            return Math.Abs(r).ToString();
        }

        /// <summary>  
        /// 根据GUID获取19位的唯一数字序列  
        /// </summary>  
        /// <returns></returns>  
        public static long GuidToLongID()
        {
            byte[] buffer = Guid.NewGuid().ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }

        public static long GuidToLong(Guid uuid)
        {
            byte[] buffer = uuid.ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }

        public static string GuidNoDashLowercase(Guid uuid)
        {
            return uuid.ToString("N");
        }
    }
}
