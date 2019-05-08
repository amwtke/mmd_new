using MD.Lib.Log;
using MD.Model.Configuration.Redis;
using MD.Model.Redis;
using MD.Model.Redis.Objects;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Util;
using Senparc.Weixin;

namespace MD.Lib.DB.Redis
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Redis的配置文件类</typeparam>
    public class RedisManager2<T> where T : RedisConfigBase, new()
    {
        static readonly ConcurrentDictionary<Type, ConnectionMultiplexer> _dic = new ConcurrentDictionary<Type, ConnectionMultiplexer>();
        static object syncobject2 = new object();

        readonly object syncobject = new object();
        readonly ConnectionMultiplexer _con = null;
        public RedisManager2()
        {
            Type t = typeof(T);
            if(!_dic.TryGetValue(t, out _con))
            {
                if(_con == null)
                {
                    lock (syncobject)
                    {
                        if(_con == null)
                        {
                            _con = InitDb();
                            _dic[t] = _con;
                        }
                    }
                }
            }
        }

        public void Close()
        {
            if(_con != null && _con.IsConnected)
            {
                lock (syncobject)
                {
                    if(_con != null && _con.IsConnected)
                    {
                        _con.Close();
                    }
                }
            }
        }

        public static void CloseAll()
        {
            foreach(var client in _dic.Values)
            {
                if(client != null && client.IsConnected)
                {
                    lock (syncobject2)
                    {
                        if(client != null && client.IsConnected)
                        {
                            client.Close();
                        }
                    }
                }
            }
        }

        public IDatabase GetDb(int dbNumber, object asyncObject)
        {
            return _con.GetDatabase(dbNumber, asyncObject);
        }

        public IDatabase GetDb<RedisObjectType>()
        {
            try
            {
                Type t = typeof(RedisObjectType);
                RedisDBNumberAttribute number = t.GetCustomAttribute(typeof(RedisDBNumberAttribute), false) as RedisDBNumberAttribute;
                int numberdb = Convert.ToInt32(number.DBNumber);

                var db = GetDb(numberdb, null);
                return db;
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }

        private ConnectionMultiplexer InitDb()
        {
            T config = MdConfigurationManager.GetConfig<T>();
            ConfigurationOptions op = new ConfigurationOptions();
            if(config == null)
                throw new Exception("Redis配置:" + config.ToString() + ",读取失败，请检查数据库！");
            //add master
            op.EndPoints.Add(config.MasterHostAndPort);
            string[] slaves = config.SlaveHostsAndPorts.Split(config.StringSeperator[0]);
            foreach(var s in slaves)
            {
                if(!string.IsNullOrEmpty(s))
                    op.EndPoints.Add(s);
            }
            op.Password = config.Password; op.AllowAdmin = true;

            op.ClientName = config.GetType().ToString() + "-ip:" + CommonHelper.GetLocalIp() + "-" + DateTime.Now.ToString();
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(op);
            return redis;
        }


        #region ORMapping

        #region save object
        /// <summary>
        /// 注意，Object一定要带上RedisDB特有的标签，否则会爆出异常！而且[RedisKeyAttribute]标签的属性必须要带上值，不然会save失败。
        /// </summary>
        /// <param name="o">注意使用标签没</param>
        /// <returns></returns>
        public async Task<bool> SaveObjectAsync(Object o)
        {
            int dbNumber; string HashName = null;

            //获取number属性
            RedisDBNumberAttribute numberAtt = o.GetType().GetCustomAttribute(typeof(RedisDBNumberAttribute), false) as RedisDBNumberAttribute;
            if(numberAtt == null)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), new Exception("Redis的存储对象没有指定dbNumber属性！"));
                throw new Exception("Redis的存储对象没有指定dbNumber属性！");
            }
            dbNumber = Convert.ToInt32(numberAtt.DBNumber);

            //获取hushname
            RedisHashAttribute hashatt = o.GetType().GetCustomAttribute(typeof(RedisHashAttribute), false) as RedisHashAttribute;
            //如果有则赋值，没有则传入空值
            if(hashatt != null)
                HashName = hashatt.Name;

            //遍历每个property
            bool ret = await saveByAttAsync(dbNumber, HashName, o);
            return ret;
        }
        /// <summary>
        /// 可以没有hashname，但是不能没有ReidsKeyAttribute的属性！
        /// </summary>
        /// <param name="dbno"></param>
        /// <param name="hashName"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        async Task<bool> saveByAttAsync(int dbno, string hashName, object o)
        {
            Type t = o.GetType(); string KeyValue = null;
            bool isNeedHash = !string.IsNullOrEmpty(hashName);

            //不能没有ReidsKeyAttribute
            KeyValue = getKeyAttributeValue(o);
            if(string.IsNullOrEmpty(KeyValue))
                return false;

            var db = GetDb(dbno, null);
            List<HashEntry> _pairs = new List<HashEntry>();

            foreach(var p in t.GetProperties())
            {
                //获取属性值
                var vo = p.GetValue(o);
                if(vo == null)
                    continue;
                string value = vo.ToString();

                foreach(var att in p.GetCustomAttributes())
                {
                    //将属性值添加到set表
                    if(att != null && att is RedisSetAttribute)
                    {
                        var setAtt = att as RedisSetAttribute;
                        string setKey = setAtt.Name;

                        //如果为Every_key,则每个Key值创建一个set.格式为type.id.set。
                        if(setKey.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                            await db.SetAddAsync(ObjectHelper.MakeKeyIfEveryKey_Set(att.GetType(), KeyValue), value);
                        else
                            await db.SetAddAsync(setKey, value);
                    }

                    //将属性值与分数添加到zset表
                    if(att != null && att is RedisZSetAttribute)
                    {
                        var zsetAtt = att as RedisZSetAttribute;
                        string zsetName = zsetAtt.Name;
                        if(zsetName.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                        {
                            zsetName = ObjectHelper.MakeKeyIfEveryKey_Zset(att.GetType(), KeyValue);
                        }
                        string scoreField = zsetAtt.ScoreFieldName;

                        //如果不需要存储值，scoreField字段留空。
                        if(string.IsNullOrEmpty(scoreField))
                        {
                            continue;
                        }

                        //没有找到property
                        PropertyInfo pp = t.GetProperty(scoreField);
                        if(pp == null) continue;

                        //property value是空或者不是double
                        object scoreValue = pp.GetValue(o);
                        if(scoreValue == null || !(scoreValue is double)) continue;

                        double score = Convert.ToDouble(scoreValue);
                        await db.SortedSetAddAsync(zsetName, value, score);
                    }

                    //将属性值添加到list中
                    if(att != null && att is RedisListAttribute)
                    {
                        var listAtt = att as RedisListAttribute;
                        string listkey = listAtt.Name;
                        //如果listkey名字指定为EveryKey
                        if(listkey.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                        {
                            //生成Key
                            listkey = ObjectHelper.MakeKeyIfEveryKey_List(att.GetType(), KeyValue);
                        }

                        if(listAtt.Push.Equals(ListPush.Left))
                            await db.ListLeftPushAsync(listkey, value);
                        else
                            await db.ListRightPushAsync(listkey, value);

                    }

                    if(att != null && att is RedisStringAttribute)
                    {
                        var stringAtt = att as RedisStringAttribute;
                        string stringKey = stringAtt.Name;
                        if(stringKey.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                            await db.StringSetAsync(ObjectHelper.MakeKeyIfEveryKey_String(att.GetType(), KeyValue), value);
                        else
                            await db.StringSetAsync(stringKey, value);
                    }

                    if(isNeedHash && att != null && att is RedisHashEntryAttribute && !string.IsNullOrEmpty(value))
                    {
                        var entryAtt = att as RedisHashEntryAttribute;
                        string entryName = entryAtt.Name;
                        HashEntry en = new KeyValuePair<RedisValue, RedisValue>(ObjectHelper.MakeField(KeyValue, entryName), value);
                        _pairs.Add(en);
                    }
                }
            }
            try
            {
                if(isNeedHash && _pairs.Count > 0)
                    await db.HashSetAsync(hashName, _pairs.ToArray());
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return false;
            }

            return true;
        }

        #region 同步方法
        /// <summary>
        /// 注意，Object一定要带上RedisDB特有的标签，否则会爆出异常！而且[RedisKeyAttribute]标签的属性必须要带上值，不然会save失败。
        /// </summary>
        /// <param name="o">注意使用标签没</param>
        /// <returns></returns>
        public bool SaveObject(Object o)
        {
            int dbNumber; string HashName = null;

            //获取number属性
            RedisDBNumberAttribute numberAtt = o.GetType().GetCustomAttribute(typeof(RedisDBNumberAttribute), false) as RedisDBNumberAttribute;
            if (numberAtt == null)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), new Exception("Redis的存储对象没有指定dbNumber属性！"));
                throw new Exception("Redis的存储对象没有指定dbNumber属性！");
            }
            dbNumber = Convert.ToInt32(numberAtt.DBNumber);

            //获取hushname
            RedisHashAttribute hashatt = o.GetType().GetCustomAttribute(typeof(RedisHashAttribute), false) as RedisHashAttribute;
            //如果有则赋值，没有则传入空值
            if (hashatt != null)
                HashName = hashatt.Name;

            //遍历每个property
            bool ret = saveByAtt(dbNumber, HashName, o);
            return ret;
        }

        bool saveByAtt(int dbno, string hashName, object o)
        {
            Type t = o.GetType(); string KeyValue = null;
            bool isNeedHash = !string.IsNullOrEmpty(hashName);

            //不能没有ReidsKeyAttribute
            KeyValue = getKeyAttributeValue(o);
            if (string.IsNullOrEmpty(KeyValue))
                return false;

            var db = GetDb(dbno, null);
            List<HashEntry> _pairs = new List<HashEntry>();

            foreach (var p in t.GetProperties())
            {
                //获取属性值
                var vo = p.GetValue(o);
                if (vo == null)
                    continue;
                string value = vo.ToString();

                foreach (var att in p.GetCustomAttributes())
                {
                    //将属性值添加到set表
                    if (att != null && att is RedisSetAttribute)
                    {
                        var setAtt = att as RedisSetAttribute;
                        string setKey = setAtt.Name;

                        //如果为Every_key,则每个Key值创建一个set.格式为type.id.set。
                        if (setKey.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                            db.SetAdd(ObjectHelper.MakeKeyIfEveryKey_Set(att.GetType(), KeyValue), value);
                        else
                            db.SetAdd(setKey, value);
                    }

                    //将属性值与分数添加到zset表
                    if (att != null && att is RedisZSetAttribute)
                    {
                        var zsetAtt = att as RedisZSetAttribute;
                        string zsetName = zsetAtt.Name;
                        if (zsetName.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                        {
                            zsetName = ObjectHelper.MakeKeyIfEveryKey_Zset(att.GetType(), KeyValue);
                        }
                        string scoreField = zsetAtt.ScoreFieldName;

                        //如果不需要存储值，scoreField字段留空。
                        if (string.IsNullOrEmpty(scoreField))
                        {
                            continue;
                        }

                        //没有找到property
                        PropertyInfo pp = t.GetProperty(scoreField);
                        if (pp == null) continue;

                        //property value是空或者不是double
                        object scoreValue = pp.GetValue(o);
                        if (scoreValue == null || !(scoreValue is double)) continue;

                        double score = Convert.ToDouble(scoreValue);
                        db.SortedSetAdd(zsetName, value, score);
                    }

                    //将属性值添加到list中
                    if (att != null && att is RedisListAttribute)
                    {
                        var listAtt = att as RedisListAttribute;
                        string listkey = listAtt.Name;
                        //如果listkey名字指定为EveryKey
                        if (listkey.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                        {
                            //生成Key
                            listkey = ObjectHelper.MakeKeyIfEveryKey_List(att.GetType(), KeyValue);
                        }

                        if (listAtt.Push.Equals(ListPush.Left))
                            db.ListLeftPush(listkey, value);
                        else
                            db.ListRightPush(listkey, value);

                    }

                    if (att != null && att is RedisStringAttribute)
                    {
                        var stringAtt = att as RedisStringAttribute;
                        string stringKey = stringAtt.Name;
                        if (stringKey.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                            db.StringSet(ObjectHelper.MakeKeyIfEveryKey_String(att.GetType(), KeyValue), value);
                        else
                            db.StringSet(stringKey, value);
                    }

                    if (isNeedHash && att != null && att is RedisHashEntryAttribute && !string.IsNullOrEmpty(value))
                    {
                        var entryAtt = att as RedisHashEntryAttribute;
                        string entryName = entryAtt.Name;
                        HashEntry en = new KeyValuePair<RedisValue, RedisValue>(ObjectHelper.MakeField(KeyValue, entryName), value);
                        _pairs.Add(en);
                    }
                }
            }
            try
            {
                if (isNeedHash && _pairs.Count > 0)
                    db.HashSet(hashName, _pairs.ToArray());
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return false;
            }

            return true;
        }

        #endregion
        #endregion

        #region get object
        /// <summary>
        /// 获取reids对象的Hash数据结构中的对象。
        /// </summary>
        /// <typeparam name="RedisObjectType"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<RedisObjectType> GetObjectFromRedisHash<RedisObjectType>(string key) where RedisObjectType : new()
        {
            try
            {
                string hashName = GetHashName<RedisObjectType>();
                string keyValue = key;
                if(string.IsNullOrEmpty(keyValue) || string.IsNullOrEmpty(hashName))
                    return default(RedisObjectType);
                RedisObjectType obj = new RedisObjectType();

                var db = GetDb<RedisObjectType>();
                List<RedisValue> _fields = new List<RedisValue>();
                Dictionary<string, string> _dic = new Dictionary<string, string>();

                foreach(PropertyInfo p in obj.GetType().GetProperties())
                {
                    var att = p.GetCustomAttribute(typeof(RedisHashEntryAttribute), false);
                    if(att == null)
                        continue;
                    var entryAtt = att as RedisHashEntryAttribute;
                    string field = ObjectHelper.MakeField(keyValue, entryAtt.Name);

                    _dic[field] = entryAtt.Name;
                    _fields.Add(field);
                }

                //一次性获取
                if(_fields.Count > 0)
                {
                    RedisValue[] values = await db.HashGetAsync(hashName, _fields.ToArray());

                    if(values != null && values.Length > 0)
                    {
                        for(int i = 0; i < values.Length; i++)
                        {
                            var p = obj.GetType().GetProperty(_dic[_fields[i]]);
                            if(values[i].IsNull)
                                continue;

                            if(p.PropertyType == typeof(string))
                                p.SetValue(obj, values[i].ToString());
                            else
                            if(p.PropertyType == typeof(int))
                                p.SetValue(obj, Convert.ToInt32(values[i].ToString()));
                            else
                            if(p.PropertyType == typeof(double))
                                p.SetValue(obj, Convert.ToDouble(values[i].ToString()));
                            else
                            if(p.PropertyType == typeof(decimal))
                                p.SetValue(obj, Convert.ToDecimal(values[i].ToString()));
                            else
                            if(p.PropertyType == typeof(DateTime))
                                p.SetValue(obj, Convert.ToDateTime(values[i].ToString()));
                        }
                    }
                }
                //最后别忘了给Key字段赋值
                setKeyPropertyValue<RedisObjectType>(obj, keyValue);
                return obj;
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                throw ex;
            }
        }

        public RedisObjectType GetObjectFromRedisHash_TongBu<RedisObjectType>(string key) where RedisObjectType : new()
        {
            try
            {
                string hashName = GetHashName<RedisObjectType>();
                string keyValue = key;
                if(string.IsNullOrEmpty(keyValue) || string.IsNullOrEmpty(hashName))
                    return default(RedisObjectType);
                RedisObjectType obj = new RedisObjectType();

                var db = GetDb<RedisObjectType>();
                List<RedisValue> _fields = new List<RedisValue>();
                Dictionary<string, string> _dic = new Dictionary<string, string>();

                foreach(PropertyInfo p in obj.GetType().GetProperties())
                {
                    var att = p.GetCustomAttribute(typeof(RedisHashEntryAttribute), false);
                    if(att == null)
                        continue;
                    var entryAtt = att as RedisHashEntryAttribute;
                    string field = ObjectHelper.MakeField(keyValue, entryAtt.Name);

                    _dic[field] = entryAtt.Name;
                    _fields.Add(field);
                }

                //一次性获取
                if(_fields.Count > 0)
                {
                    RedisValue[] values = db.HashGet(hashName, _fields.ToArray());

                    if(values != null && values.Length > 0)
                    {
                        for(int i = 0; i < values.Length; i++)
                        {
                            var p = obj.GetType().GetProperty(_dic[_fields[i]]);
                            if(values[i].IsNull)
                                continue;

                            if(p.PropertyType.Equals(typeof(string)))
                                p.SetValue(obj, values[i].ToString());
                            else
                            if(p.PropertyType.Equals(typeof(int)))
                                p.SetValue(obj, Convert.ToInt32(values[i].ToString()));
                            else
                            if(p.PropertyType.Equals(typeof(double)))
                                p.SetValue(obj, Convert.ToDouble(values[i].ToString()));
                            else
                            if(p.PropertyType.Equals(typeof(decimal)))
                                p.SetValue(obj, Convert.ToDecimal(values[i].ToString()));
                            else
                            if(p.PropertyType.Equals(typeof(DateTime)))
                                p.SetValue(obj, Convert.ToDateTime(values[i].ToString()));
                        }
                    }
                }
                //最后别忘了给Key字段赋值
                setKeyPropertyValue<RedisObjectType>(obj, keyValue);
                return obj;
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                throw ex;
            }
        }
        #endregion


        #region op redis attribute

        #region get set keyattribute
        RedisObjectType setKeyPropertyValue<RedisObjectType>(RedisObjectType o, string value) where RedisObjectType : new()
        {
            Type t = typeof(RedisObjectType);
            t.GetProperties().ToList().ForEach(
                delegate (PropertyInfo p) {
                    Attribute a = p.GetCustomAttribute(typeof(RedisKeyAttribute), false);
                    if(a != null)
                    {
                        if(!string.IsNullOrEmpty(value))
                            p.SetValue(o, value);
                    }
                });
            return o;
        }
        string getKeyAttributeValue(object o)
        {
            foreach(var p in o.GetType().GetProperties())
            {
                var att = p.GetCustomAttribute(typeof(RedisKeyAttribute), false);
                if(att != null)
                {
                    object value = p.GetValue(o);
                    if(value == null)
                    {
                        MDLogger.LogErrorAsync(typeof(RedisManager2<T>), new Exception("type=" + o.GetType() + "在saveByAttAsync时，没有指定Key！"));
                        throw new Exception("type=" + o.GetType() + "在saveByAttAsync时，没有指定Key！");
                    }

                    return value.ToString();
                }
            }
            return null;
        }
        #endregion

        /// <summary>
        /// 获取一个对象上Redis中Hash结构的key值。
        /// </summary>
        /// <typeparam name="RedisObjectType"></typeparam>
        /// <returns>没有则返回空</returns>
        public string GetHashName<RedisObjectType>()
        {
            try
            {
                Attribute hashatt = typeof(RedisObjectType).GetCustomAttribute(typeof(RedisHashAttribute), false);
                if(hashatt == null)
                    return null;
                var hashAt = hashatt as RedisHashAttribute;
                return hashAt.Name;
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                throw ex;
            }
        }

        /// <summary>
        /// 根据对象以及对象上的标签来获取对应Redis对象中Property上的特定数据结构的key名称。
        /// </summary>
        /// <typeparam name="RedisObjectType">Redis对象的type</typeparam>
        /// <typeparam name="AttType">Redis对象中某个数据结构的Att。在MD.Model.Redis.Att.CustomAtts*命名空间下.</typeparam>
        /// <returns></returns>
        public string GetKeyName<RedisObjectType, AttType>() where AttType : RedisBaseAttribute
        {
            Type oT = typeof(RedisObjectType);
            Type aT = typeof(AttType);
            string ret = null;
            oT.GetProperties().ToList().ForEach(delegate (PropertyInfo p) {
                var a = p.GetCustomAttribute(aT);
                if(a != null)
                {
                    var at = a as RedisBaseAttribute;
                    ret = at.Name;
                }
            });
            return ret;
        }

        /// <summary>
        /// EVERY_KEY 兼容。
        /// </summary>
        /// <typeparam name="RedisObjectType"></typeparam>
        /// <typeparam name="AttType"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        public string GetKeyName<AttType>(object o)
        {
            Type oT = o.GetType();
            Type aT = typeof(AttType);
            string ret = null;
            oT.GetProperties().ToList().ForEach(delegate (PropertyInfo p) {
                var a = p.GetCustomAttribute(aT);
                if(a != null)
                {
                    var at = a as RedisBaseAttribute;
                    ret = at.Name;
                }
            });
            if(ret != null && !ret.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                return ret;

            string keyValue = getKeyAttributeValue(o);
            if(string.IsNullOrEmpty(keyValue))
                return null;
            if(aT.ToString().ToLower().Contains("list"))
                ret = ObjectHelper.MakeKeyIfEveryKey_List(aT, keyValue);
            if(aT.ToString().ToLower().Contains("set"))
                ret = ObjectHelper.MakeKeyIfEveryKey_Set(aT, keyValue);
            if(aT.ToString().ToLower().Contains("zset"))
                ret = ObjectHelper.MakeKeyIfEveryKey_Zset(aT, keyValue);
            if(aT.ToString().ToLower().Contains("string"))
                ret = ObjectHelper.MakeKeyIfEveryKey_String(aT, keyValue);

            return ret;
        }

        #endregion

        #region operation db
        public async Task<bool> IsContainedInSetByPropertyName<RedisObjectType>(string KeyName, string value)
        {
            var db = GetDb<RedisObjectType>();
            return await db.SetContainsAsync(KeyName, value);
        }

        /// <summary>
        /// 对RedisObjectType类型propertyName属性上的Zset数据库中Member元素的Score增加increaseby。
        /// </summary>
        /// <typeparam name="RedisObjectType"></typeparam>
        /// <param name="keyName"></param>
        /// <param name="member"></param>
        /// <param name="increaseBy"></param>
        /// <returns></returns>
        public async Task<double> IncreaseScoreAsync<RedisObjectType>(string keyName, string member, double increaseBy)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return await db.SortedSetIncrementAsync(keyName, member, increaseBy);
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return Double.NaN;
            }
        }

        public double IncreaseScore<RedisObjectType>(string keyName, string member, double increaseBy)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return db.SortedSetIncrement(keyName, member, increaseBy);
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return Double.NaN;
            }
        }

        /// <summary>
        /// 减小分数
        /// </summary>
        /// <typeparam name="RedisObjectType"></typeparam>
        /// <param name="Name"></param>
        /// <param name="member"></param>
        /// <param name="decreaseBy"></param>
        /// <returns></returns>
        public async Task<double> DecreaseScore<RedisObjectType>(string KeyName, string member, double decreaseBy)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return await db.SortedSetDecrementAsync(KeyName, member, decreaseBy);
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return Double.NaN;
            }
        }

        /// <summary>
        /// 按照Score的打分区间来取数据
        /// </summary>
        /// <typeparam name="RedisObjectType">对象的tpye</typeparam>
        /// <param name="keyName">zset定义在那个字段上</param>
        /// <param name="offSet">从第几个元素开始</param>
        /// <param name="top">共取多少个元素</param>
        /// <param name="orderWay">升序或者逆序</param>
        /// <param name="Scorefrom">最小的score</param>
        /// <param name="Scoreto">最大的score</param>
        /// <returns></returns>
        private async Task<KeyValuePair<string, double>[]> GetRangeByScoreWithScoreAsync<RedisObjectType>(string keyName, long offSet, int top, Order orderWay = Order.Descending, double Scorefrom = double.NegativeInfinity, double Scoreto = double.PositiveInfinity)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return ConvertSortedSetEntryToKeyValuePair(await db.SortedSetRangeByScoreWithScoresAsync(keyName, order: orderWay, take: top, start: Scorefrom, stop: Scoreto, skip: offSet));
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }

        /// <summary>
        /// 按照Score的打分区间来取数据
        /// </summary>
        /// <typeparam name="RedisObjectType">对象的tpye</typeparam>
        /// <param name="keyName">zset定义在那个字段上</param>
        /// <param name="offSet">从第几个元素开始</param>
        /// <param name="top">共取多少个元素</param>
        /// <param name="orderWay">升序或者逆序</param>
        /// <param name="Scorefrom">最小的score</param>
        /// <param name="Scoreto">最大的score</param>
        /// <returns></returns>
        private KeyValuePair<string, double>[] GetRangeByScoreWithScore<RedisObjectType>(string keyName, long offSet, int top, Order orderWay = Order.Descending, double Scorefrom = double.NegativeInfinity, double Scoreto = double.PositiveInfinity)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return ConvertSortedSetEntryToKeyValuePair(db.SortedSetRangeByScoreWithScores(keyName, order: orderWay, take: top, start: Scorefrom, stop: Scoreto, skip: offSet));
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }

        /// <summary>
        /// 按照Score的打分区间来取数据.不带score的方法。
        /// </summary>
        /// <typeparam name="RedisObjectType">对象的tpye</typeparam>
        /// <param name="keyName">zset定义在那个字段上</param>
        /// <param name="offSet">从第几个元素开始</param>
        /// <param name="top">共取多少个元素</param>
        /// <param name="orderWay">升序或者逆序</param>
        /// <param name="Scorefrom">最小的score</param>
        /// <param name="Scoreto">最大的score</param>
        /// <returns></returns>
        private async Task<List<string>> GetRangeByScoreAsync<RedisObjectType>(string keyName, long offSet, int top, Order orderWay = Order.Descending, double Scorefrom = double.NegativeInfinity, double Scoreto = double.PositiveInfinity)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return ConvertRedisValueToString(await db.SortedSetRangeByScoreAsync(keyName, order: orderWay, take: top, start: Scorefrom, stop: Scoreto, skip: offSet));
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }

        /// <summary>
        /// 按照Score的打分区间来取数据.不带score的方法。
        /// </summary>
        /// <typeparam name="RedisObjectType">对象的tpye</typeparam>
        /// <param name="keyName">zset定义在那个字段上</param>
        /// <param name="offSet">从第几个元素开始</param>
        /// <param name="top">共取多少个元素</param>
        /// <param name="orderWay">升序或者逆序</param>
        /// <param name="Scorefrom">最小的score</param>
        /// <param name="Scoreto">最大的score</param>
        /// <returns></returns>
        private List<string> GetRangeByScore<RedisObjectType>(string keyName, long offSet, int top, Order orderWay = Order.Descending, double Scorefrom = double.NegativeInfinity, double Scoreto = double.PositiveInfinity)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return ConvertRedisValueToString(db.SortedSetRangeByScore(keyName, order: orderWay, take: top, start: Scorefrom, stop: Scoreto, skip: offSet));
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }

        /// <summary>
        /// 按照打分的排名来取。
        /// </summary>
        /// <typeparam name="RedisObjectType"></typeparam>
        /// <param name="keyName"></param>
        /// <param name="orderWay"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [Obsolete]
        public async Task<List<string>> GetRangeByRankAsync<RedisObjectType>(string keyName, Order orderWay = Order.Descending, long from = 0, long to = -1)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return ConvertRedisValueToString(await db.SortedSetRangeByRankAsync(keyName, order: orderWay, start: from, stop: to));
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }

        /// <summary>
        /// 按照打分的排名来取。带Score值。
        /// </summary>
        /// <typeparam name="RedisObjectType"></typeparam>
        /// <param name="keyName"></param>
        /// <param name="orderWay"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [Obsolete]
        public async Task<KeyValuePair<string, double>[]> GetRangeByRankWithScoreAsync<RedisObjectType>(string keyName, Order orderWay = Order.Descending, long from = 0, long to = -1)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return ConvertSortedSetEntryToKeyValuePair(await db.SortedSetRangeByRankWithScoresAsync(keyName, order: orderWay, start: from, stop: to));
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }

        [Obsolete]
        public KeyValuePair<string, double>[] GetRangeByRankWithScore<RedisObjectType>(string keyName, Order orderWay = Order.Descending, long from = 0, long to = -1)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return ConvertSortedSetEntryToKeyValuePair(db.SortedSetRangeByRankWithScores(keyName, order: orderWay, start: from, stop: to));
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }

        public async Task<List<string>> GetAllMembers<RedisObjectType>(string keyName)
        {
            try
            {
                var db = GetDb<RedisObjectType>();
                return ConvertRedisValueToString(await db.SetMembersAsync(keyName));
            }
            catch(Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), ex);
                return null;
            }
        }
        #region converter
        public KeyValuePair<string, double>[] ConvertSortedSetEntryToKeyValuePair(SortedSetEntry[] source)
        {
            SortedSetEntry[] es = source;
            if(es != null && es.Length > 0)
            {
                List<KeyValuePair<string, double>> pair = new List<KeyValuePair<string, double>>();
                foreach(var e in es)
                {
                    pair.Add(new KeyValuePair<string, double>(e.Element, e.Score));
                }
                return pair.ToArray();
            }
            return null;
        }

        public List<string> ConvertRedisValueToString(RedisValue[] source)
        {
            RedisValue[] es = source;
            if(es != null && es.Length > 0)
            {
                List<string> list = new List<string>();
                foreach(var e in es)
                {
                    if(e.IsNullOrEmpty)
                        continue;
                    list.Add(e.ToString());
                }
                return list;
            }
            return null;
        }
        #endregion


        #endregion

        #region zset 通用方法

        /// <summary>
        /// 如果有every_key就赋值，否则可以给null。
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <typeparam name="ZsetAtt"></typeparam>
        /// <param name="every_key"></param>
        /// <returns></returns>
        public async Task<long> GetZsetCountAsync<RedisObject, ZsetAtt>(string every_key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            if(!string.IsNullOrEmpty(every_key))
                o = setKeyPropertyValue<RedisObject>(o, every_key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return await db.SortedSetLengthAsync(zsetName);
        }

        public long GetZsetCount<RedisObject, ZsetAtt>(string every_key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            if (!string.IsNullOrEmpty(every_key))
                o = setKeyPropertyValue<RedisObject>(o, every_key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return db.SortedSetLength(zsetName);
        }

        public async Task<double> AddScoreAsync<RedisObject, ZsetAtt>(string key, double score) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return await db.SortedSetIncrementAsync(zsetName, key, score);
        }

        public async Task<double> AddScoreEveryKeyAsync<RedisObject, ZsetAtt>(string every_key_value, string key, double score) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return await db.SortedSetIncrementAsync(zsetName, key, score);
        }

        public double AddScore<RedisObject, ZsetAtt>(string key, double score) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return db.SortedSetIncrement(zsetName, key, score);
        }

        public double AddScoreEveryKey<RedisObject, ZsetAtt>(string every_key_value, string key, double score) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return db.SortedSetIncrement(zsetName, key, score);
        }

        public bool SetScore<RedisObject, ZsetAtt>(string key, double score) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return db.SortedSetAdd(zsetName, key, score);
        }

        public bool SetScoreEveryKey<RedisObject, ZsetAtt>(string every_key_value, string key,double score) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return db.SortedSetAdd(zsetName, key, score);
        }

        public async Task<bool> SetScoreAsync<RedisObject, ZsetAtt>(string key, double score) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return await db.SortedSetAddAsync(zsetName, key, score);
        }

        public async Task<bool> SetScoreEveryKeyAsync<RedisObject, ZsetAtt>(string Every_Key_value, string key,double score) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, Every_Key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");

            return await db.SortedSetAddAsync(zsetName, key, score);
        }

        public async Task<bool> CleanScoreAsync<RedisObject, ZsetAtt>(string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return await db.SortedSetAddAsync(zsetName, key, 0);
        }

        public async Task<bool> CleanScoreEveryKeyAsync<RedisObject, ZsetAtt>(string Every_Key_Value,string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, Every_Key_Value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return await db.SortedSetAddAsync(zsetName, key, 0);
        }

        public bool CleanScore<RedisObject, ZsetAtt>(string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return db.SortedSetAdd(zsetName, key, 0);
        }

        public bool CleanScoreEverykey<RedisObject, ZsetAtt>(string every_key_value,string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return db.SortedSetAdd(zsetName, key, 0);
        }

        public async Task<double> GetScoreAsync<RedisObject, ZsetAtt>(string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            double? value = await db.SortedSetScoreAsync(zsetName, key);
            if(value.HasValue)
                return value.Value;
            return 0;
        }

        public async Task<double> GetScoreEveryKeyAsync<RedisObject, ZsetAtt>(string every_key_value,string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            double? value = await db.SortedSetScoreAsync(zsetName, key);
            if (value.HasValue)
                return value.Value;
            return 0;
        }

        public double GetScore<RedisObject, ZsetAtt>(string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            double? value = db.SortedSetScore(zsetName, key);
            if(value.HasValue)
                return value.Value;
            return 0;
        }

        public double GetScoreEverykey<RedisObject, ZsetAtt>(string every_key_value,string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            double? value = db.SortedSetScore(zsetName, key);
            if (value.HasValue)
                return value.Value;
            return 0;
        }
        /// <summary>
        /// 如果zset名称是every_key则必须传入key。其他情况就不需要给key赋值。
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <typeparam name="ZsetAtt"></typeparam>
        /// <param name="key"></param>
        /// <param name="orderWay"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<string, double>[]> GetRangeByRankAsync<RedisObject, ZsetAtt>(string every_key, Order orderWay = Order.Descending, long from = 0, long to = -1) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            if(!string.IsNullOrEmpty(every_key))
                o = setKeyPropertyValue<RedisObject>(o, every_key);
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return await GetRangeByRankWithScoreAsync<RedisObject>(zsetName, orderWay, from, to);
        }


        /// <summary>
        /// 如果zset名称是every_key则必须传入key。其他情况就不需要给key赋值。
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <typeparam name="ZsetAtt"></typeparam>
        /// <param name="key"></param>
        /// <param name="orderWay"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public KeyValuePair<string, double>[] GetRangeByRank<RedisObject, ZsetAtt>(string every_key, Order orderWay = Order.Descending, long from = 0, long to = -1) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            if(!string.IsNullOrEmpty(every_key))
                o = setKeyPropertyValue<RedisObject>(o, every_key);
            string zsetName = GetKeyName<ZsetAtt>(o);
            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return GetRangeByRankWithScore<RedisObject>(zsetName, orderWay, from, to);
        }

        /// <summary>
        /// 如果zset名称是every_key则必须传入key。其他情况就不需要给key赋值。
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <typeparam name="ZsetAtt"></typeparam>
        /// <param name="every_key"></param>
        /// <param name="offSet"></param>
        /// <param name="top"></param>
        /// <param name="orderWay"></param>
        /// <param name="Scorefrom"></param>
        /// <param name="Scoreto"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<string, double>[]> GetRangeByScoreAsync<RedisObject, ZsetAtt>(string every_key, long offSet, int top, Order orderWay = Order.Descending, double Scorefrom = double.NegativeInfinity, double Scoreto = double.PositiveInfinity) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            if(!string.IsNullOrEmpty(every_key))
                o = setKeyPropertyValue<RedisObject>(o, every_key);
            string zsetName = GetKeyName<ZsetAtt>(o);

            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return await GetRangeByScoreWithScoreAsync<RedisObject>(zsetName, offSet, top, orderWay, Scorefrom, Scoreto);
        }
        /// <summary>
        /// 如果zset名称是every_key则必须传入key。其他情况就不需要给key赋值。
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <typeparam name="ZsetAtt"></typeparam>
        /// <param name="every_key"></param>
        /// <param name="offSet"></param>
        /// <param name="top"></param>
        /// <param name="orderWay"></param>
        /// <param name="Scorefrom"></param>
        /// <param name="Scoreto"></param>
        /// <returns></returns>
        public KeyValuePair<string, double>[] GetRangeByScore<RedisObject, ZsetAtt>(string every_key, long offSet, int top, Order orderWay = Order.Descending, double Scorefrom = double.NegativeInfinity, double Scoreto = double.PositiveInfinity) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            if(!string.IsNullOrEmpty(every_key))
                o = setKeyPropertyValue<RedisObject>(o, every_key);
            string zsetName = GetKeyName<ZsetAtt>(o);

            if(string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return GetRangeByScoreWithScore<RedisObject>(zsetName, offSet, top, orderWay, Scorefrom, Scoreto);
        }



        /// <summary>
        /// 如果zset名称是every_key则必须传入key。其他情况就不需要给key赋值。
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <typeparam name="ZsetAtt"></typeparam>
        /// <param name="every_key"></param>
        /// <param name="offSet"></param>
        /// <param name="top"></param>
        /// <param name="orderWay"></param>
        /// <param name="Scorefrom"></param>
        /// <param name="Scoreto"></param>
        /// <returns></returns>
        public async Task<List<string>> GetRangeByScoreWithoutScoreAsync<RedisObject, ZsetAtt>(string every_key, long offSet, int top, Order orderWay = Order.Descending, double Scorefrom = double.NegativeInfinity, double Scoreto = double.PositiveInfinity) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            if (!string.IsNullOrEmpty(every_key))
                o = setKeyPropertyValue<RedisObject>(o, every_key);
            string zsetName = GetKeyName<ZsetAtt>(o);

            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return await GetRangeByScoreAsync<RedisObject>(zsetName, offSet, top, orderWay, Scorefrom, Scoreto);
        }

        /// <summary>
        /// 如果zset名称是every_key则必须传入key。其他情况就不需要给key赋值。
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <typeparam name="ZsetAtt"></typeparam>
        /// <param name="every_key"></param>
        /// <param name="offSet"></param>
        /// <param name="top"></param>
        /// <param name="orderWay"></param>
        /// <param name="Scorefrom"></param>
        /// <param name="Scoreto"></param>
        /// <returns></returns>
        public List<string> GetRangeByScoreWithoutScore<RedisObject, ZsetAtt>(string every_key, long offSet, int top, Order orderWay = Order.Descending, double Scorefrom = double.NegativeInfinity, double Scoreto = double.PositiveInfinity) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            if (!string.IsNullOrEmpty(every_key))
                o = setKeyPropertyValue<RedisObject>(o, every_key);
            string zsetName = GetKeyName<ZsetAtt>(o);

            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return GetRangeByScore<RedisObject>(zsetName, offSet, top, orderWay, Scorefrom, Scoreto);
        }
        #endregion

        #region set 通用方法
        public async Task<bool> SetAddAsync<RedisObject, SetAtt>(string redisObjectKeyValue,string member) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return await db.SetAddAsync(setName, member);
        }

        public bool SetAdd<RedisObject, SetAtt>(string redisObjectKeyValue, string member) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return db.SetAdd(setName, member);
        }

        public async Task<bool> SetRemoveAsync<RedisObject, SetAtt>(string redisObjectKeyValue, string member) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return await db.SetRemoveAsync(setName, member);
        }

        public bool SetRemove<RedisObject, SetAtt>(string redisObjectKeyValue, string member) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return db.SetRemove(setName, member);
        }

        public long SetLenth<RedisObject, SetAtt>(string redisObjectKeyValue) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return db.SetLength(setName);
        }

        public async Task<long> SetLenthAsync<RedisObject, SetAtt>(string redisObjectKeyValue) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return await db.SetLengthAsync(setName);
        }

        public bool SetContain<RedisObject, SetAtt>(string redisObjectKeyValue,string member) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return db.SetContains(setName, member);
        }

        public async Task<bool> SetContainAsync<RedisObject, SetAtt>(string redisObjectKeyValue,string member) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return await db.SetContainsAsync(setName,member);
        }


        public List<RedisValue> SetMembers<RedisObject, SetAtt>(string redisObjectKeyValue) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return db.SetMembers(setName).ToList();
        }

        public async Task<List<RedisValue>> SetMembersAsync<RedisObject, SetAtt>(string redisObjectKeyValue) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, redisObjectKeyValue);

            var db = GetDb<RedisObject>();
            string setName = GetKeyName<SetAtt>(o);
            if (string.IsNullOrEmpty(setName))
                throw new Exception("没有找到setName");

            return (await db.SetMembersAsync(setName)).ToList();
        }

        #endregion

        #region 删除member
        public async Task<bool> DeleteKeyZsetAsync<RedisObject, ZsetAtt>(string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return await db.SortedSetRemoveAsync(zsetName, key);
        }

        public bool DeleteKeyZset<RedisObject, ZsetAtt>(string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return db.SortedSetRemove(zsetName, key);
        }

        public async Task<bool> DeleteKeyZsetEveryKeyAsync<RedisObject, ZsetAtt>(string every_key_value,string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return await db.SortedSetRemoveAsync(zsetName, key);
        }

        public bool DeleteKeyZsetEveryKey<RedisObject, ZsetAtt>(string every_key_value, string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return db.SortedSetRemove(zsetName, key);
        }
        #endregion

        #region 获取zset名称

        public string GetZsetName<RedisObject, ZsetAtt>(string key) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return zsetName;
        }

        public string GetZsetNameEveryKey<RedisObject, ZsetAtt>(string every_key_value) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string zsetName = GetKeyName<ZsetAtt>(o);
            if (string.IsNullOrEmpty(zsetName))
                throw new Exception("没有找到zsetName");
            return zsetName;
        }

        #endregion

        #endregion
        #region list特殊方法
        public async Task<bool> ReplaceObjectInListAsync(object n, object o)
        {
            int dbNumber; string HashName = null;

            //获取number属性
            RedisDBNumberAttribute numberAtt = o.GetType().GetCustomAttribute(typeof(RedisDBNumberAttribute), false) as RedisDBNumberAttribute;
            if (numberAtt == null)
            {
                MDLogger.LogErrorAsync(typeof(RedisManager2<T>), new Exception("Redis的存储对象没有指定dbNumber属性！"));
                throw new Exception("Redis的存储对象没有指定dbNumber属性！");
            }
            dbNumber = Convert.ToInt32(numberAtt.DBNumber);

            //获取hushname
            RedisHashAttribute hashatt = o.GetType().GetCustomAttribute(typeof(RedisHashAttribute), false) as RedisHashAttribute;
            //如果有则赋值，没有则传入空值
            if (hashatt != null)
                HashName = hashatt.Name;

            Type t = o.GetType(); string KeyValue = null;
            //不能没有ReidsKeyAttribute
            KeyValue = getKeyAttributeValue(o);
            if (string.IsNullOrEmpty(KeyValue))
                return false;

            var db = GetDb(dbNumber, null);

            foreach (var p in t.GetProperties())
            {
                //获取属性值
                var vo = p.GetValue(o);
                var vn = p.GetValue(n);
                if (vo == null || vn == null)
                    continue;
                string value = vo.ToString();
                string nvalue = vn.ToString();

                foreach (var att in p.GetCustomAttributes())
                {
                    //将属性值添加到list中
                    if (att != null && att is RedisListAttribute)
                    {
                        var listAtt = att as RedisListAttribute;
                        string listkey = listAtt.Name;
                        //如果listkey名字指定为EveryKey
                        if (listkey.Equals(RedisSpecialStrings.EVERY_KEY.ToString()))
                        {
                            //生成Key
                            listkey = ObjectHelper.MakeKeyIfEveryKey_List(att.GetType(), KeyValue);
                        }

                        long step1Result = await db.ListInsertBeforeAsync(listkey, value, nvalue);
                        if (step1Result != -1)
                        {
                            long step2Result = await db.ListRemoveAsync(listkey, value, 0);
                            if (step2Result > 0)
                                return true;
                            else
                            {
                                long failed = await db.ListRemoveAsync(listkey, nvalue, 1);
                            }
                        }
                    }
                }
            }

            return false;
        }
        #endregion

        #region string key

        public async Task<RedisValue> StringGetAsync<RedisObject, StringAtt>() where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            //o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string stringKeyName = GetKeyName<StringAtt>(o);
            if (string.IsNullOrEmpty(stringKeyName))
                throw new Exception("没有找到StringAtt");

            return await db.StringGetAsync(stringKeyName);
        }

        public async Task<RedisValue> StringGetEveryKeyAsync<RedisObject, StringAtt>(string every_key_value) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string stringKeyName = GetKeyName<StringAtt>(o);
            if (string.IsNullOrEmpty(stringKeyName))
                throw new Exception("没有找到StringAtt");

            return await db.StringGetAsync(stringKeyName);
        }

        public RedisValue StringGet<RedisObject, StringAtt>() where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            //o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string stringKeyName = GetKeyName<StringAtt>(o);
            if (string.IsNullOrEmpty(stringKeyName))
                throw new Exception("没有找到StringAtt");

            return db.StringGet(stringKeyName);
        }


        public RedisValue StringGetEveryKey<RedisObject, StringAtt>(string every_key_value) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string stringKeyName = GetKeyName<StringAtt>(o);
            if (string.IsNullOrEmpty(stringKeyName))
                throw new Exception("没有找到StringAtt");

            return db.StringGet(stringKeyName);
        }

        public async Task<bool> StringSetAsync<RedisObject, StringAtt>(RedisValue value) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            //o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string stringKeyName = GetKeyName<StringAtt>(o);
            if (string.IsNullOrEmpty(stringKeyName))
                throw new Exception("没有找到StringAtt");

            return await db.StringSetAsync(stringKeyName, value);
        }

        public async Task<bool> StringSetEveryKeyAsync<RedisObject, StringAtt>(string every_key_value,RedisValue value) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string stringKeyName = GetKeyName<StringAtt>(o);
            if (string.IsNullOrEmpty(stringKeyName))
                throw new Exception("没有找到StringAtt");

            return await db.StringSetAsync(stringKeyName, value);
        }

        public bool StringSet<RedisObject, StringAtt>(RedisValue value) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            //o = setKeyPropertyValue<RedisObject>(o, key);

            var db = GetDb<RedisObject>();
            string stringKeyName = GetKeyName<StringAtt>(o);
            if (string.IsNullOrEmpty(stringKeyName))
                throw new Exception("没有找到StringAtt");

            return db.StringSet(stringKeyName, value);
        }

        public bool StringSetEveryKey<RedisObject, StringAtt>(string every_key_value,RedisValue value) where RedisObject : class, new()
        {
            RedisObject o = new RedisObject();
            o = setKeyPropertyValue<RedisObject>(o, every_key_value);

            var db = GetDb<RedisObject>();
            string stringKeyName = GetKeyName<StringAtt>(o);
            if (string.IsNullOrEmpty(stringKeyName))
                throw new Exception("没有找到StringAtt");

            return db.StringSet(stringKeyName, value);
        }
        #endregion

        #region 删除key
        public async Task<bool> DeleteRedisKeyAsync<RedisObjectType>(string keyname)
        {
            var db = GetDb<RedisObjectType>();
            return await db.KeyDeleteAsync(keyname);
        }

        public bool DeleteRedisKey<RedisObjectType>(string keyname)
        {
            var db = GetDb<RedisObjectType>();
            return db.KeyDelete(keyname);
        }

        #endregion

        #region 删除hash项

        /// <summary>
        /// 删除一个hash表的一项
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <param name="redisKeyValue"></param>
        /// <param name="hashEntryName"></param>
        /// <returns></returns>
        public async Task<bool> DeleteHashItemAsync<RedisObject>(string redisKeyValue,string hashEntryName)
        {
            var hashFieldName = ObjectHelper.MakeField(redisKeyValue, hashEntryName);
            if (string.IsNullOrEmpty(hashFieldName))
                return false;
            var hashKeyName = GetHashName<RedisObject>();
            var db = GetDb<RedisObject>();
            return await db.HashDeleteAsync(hashKeyName, hashFieldName);
        }

        public async Task<bool> HashExistsAsync<RedisObject>(string redisKeyValue, string hashEntryName)
        {
            var hashFieldName = ObjectHelper.MakeField(redisKeyValue, hashEntryName);
            if (string.IsNullOrEmpty(hashFieldName))
                return false;
            var hashKeyName = GetHashName<RedisObject>();
            var db = GetDb<RedisObject>();
            return await db.HashExistsAsync(hashKeyName, hashFieldName);
        }

        public bool DeleteHashItem<RedisObject>(string redisKeyValue, string hashEntryName)
        {
            var hashFieldName = ObjectHelper.MakeField(redisKeyValue, hashEntryName);
            if (string.IsNullOrEmpty(hashFieldName))
                return false;
            var hashKeyName = GetHashName<RedisObject>();
            var db = GetDb<RedisObject>();
            return db.HashDelete(hashKeyName, hashFieldName);
        }

        /// <summary>
        /// 根据一个RedisKeyValue删除一个RedisHash对象。
        /// </summary>
        /// <typeparam name="RedisObject"></typeparam>
        /// <param name="redisKeyValue"></param>
        /// <returns></returns>
        public int DeleteHashObject<RedisObject>(string redisKeyValue)
        {
            Type t = typeof (RedisObject);
            int count = 0;
            
            t.GetProperties().ToList().ForEach(delegate (PropertyInfo p) {
                var a = p.GetCustomAttribute(typeof(RedisHashEntryAttribute));
                if (a != null)
                {
                    var at = a as RedisHashEntryAttribute;
                    if (at != null)
                    {
                        var name = at.Name;
                        if(DeleteHashItem<RedisObject>(redisKeyValue, name))
                        {
                            count++;
                        }
                    }
                }
            });
            return count;
        }

        public async Task<int> DeleteHashObjectAsync<RedisObject>(string redisKeyValue)
        {
            Type t = typeof(RedisObject);
            int count = 0;

            foreach (var p in t.GetProperties().ToList())
            {
                var a = p.GetCustomAttribute(typeof(RedisHashEntryAttribute));
                if (a != null)
                {
                    var at = a as RedisHashEntryAttribute;
                    if (at != null)
                    {
                        var name = at.Name;
                        if (await DeleteHashItemAsync<RedisObject>(redisKeyValue, name))
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }
        #endregion
    }
}
