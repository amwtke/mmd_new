using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects
{
    public static class ObjectHelper
    {
        /// <summary>
        /// 用于生成hash数据结构的field。统一都用keyvalue_propertyName的形式。
        /// </summary>
        /// <param name="keyvalue">对象的key值</param>
        /// <param name="propertyName">property的名称</param>
        /// <returns></returns>
        public static string MakeField(string keyvalue, string propertyName)
        {
            return keyvalue + "_" + propertyName;
        }

        public static string MakeKeyIfEveryKey_Set(Type t, string keyValue)
        {
            return t.Name + "." + keyValue + ".set";
        }

        public static string MakeKeyIfEveryKey_List(Type t, string keyValue)
        {
            return t.Name + "." + keyValue + ".list";
        }

        public static string MakeKeyIfEveryKey_String(Type t, string keyValue)
        {
            return t.Name + "." + keyValue + ".string";
        }

        public static string MakeKeyIfEveryKey_Zset(Type t, string keyValue)
        {
            return t.Name + "." + keyValue + ".zset";
        }
    }
}
