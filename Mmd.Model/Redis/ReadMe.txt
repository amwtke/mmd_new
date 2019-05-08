对于Every_key的zset，list，set，string等对于一个RedisObject只能有一个。因为makekey的时候是
public static string MakeKeyIfEveryKey_Set(Type t, string keyValue)
        {
            return t.ToString() + "." + keyValue + ".set";
        }

        public static string MakeKeyIfEveryKey_List(Type t, string keyValue)
        {
            return t.ToString() + "." + keyValue + ".list";
        }

        public static string MakeKeyIfEveryKey_String(Type t, string keyValue)
        {
            return t.ToString() + "." + keyValue + ".string";
        }

        public static string MakeKeyIfEveryKey_Zset(Type t, string keyValue)
        {
            return t.ToString() + "." + keyValue + ".zset";
        }