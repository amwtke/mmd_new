using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.Vector;

namespace MD.Lib.Weixin.Vector
{
    public enum EVectorType
    {
        PTCG,   //拼团成功
        TL,     //时间线
    }


    public static class VectorProcessorManager
    {
        static ConcurrentDictionary<string, IVectorProcessor> _dic=new ConcurrentDictionary<string, IVectorProcessor>();
        static readonly object initLockObject = new object();
        static VectorProcessorManager()
        {
            registerProcessors();
        }

        static void registerProcessors()
        {
            if (_dic.Count > 0) return;
            lock (initLockObject)
            {
                if (_dic.Count == 0)
                {
                    AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(delegate (Assembly ass)
                    {
                        if (ass.ToString().ToLower().Contains("mmd.lib"))
                        {
                            ass.GetTypes().ToList().ForEach(delegate (Type at)
                            {
                                at.GetInterfaces().ToList().ForEach(delegate (Type intface)
                                {
                                    if (intface.ToString().Contains("IVectorProcessor"))
                                    {
                                        IVectorProcessor p = at.Assembly.CreateInstance(at.FullName) as IVectorProcessor;
                                        if (p != null)
                                        {
                                            _dic[p.GetVectorType()] = p;
                                        }
                                    }
                                });
                            });
                        }
                    });
                }
            }
        }

        public static void Register(string type, IVectorProcessor processor)
        {
            _dic[type] = processor;
        }

        public static VectorView Parse(Model.DB.Professional.Vector v)
        {
            return _dic[v.type].Parser(v.expression);
        }

        public static async Task Route(Model.DB.Professional.Vector v)
        {
            try
            {
                if (_dic.ContainsKey(v.type))
                {
                    await _dic[v.type].Route(v);
                }
                else
                {
                    MDLogger.LogErrorAsync(typeof(VectorProcessorManager),new Exception($"没有注册vector类型:{v.type}!"));
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(VectorProcessorManager),ex);
            }
        }

        public static async Task<bool> PreRouteAsnyc(Model.DB.Professional.Vector v)
        {
            try
            {
                //存储vector
                using (BizRepository repo = new BizRepository())
                {
                    await repo.VectorAddAsnyc(v);
                }

                //redis
                VectorRedis r = new VectorRedis();
                r.vid = v.vid.ToString();
                r.value = RedisCommonHelper.ObjectToString(v);//r.GenValue(v);
                await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(r);
                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(VectorProcessorManager), ex);
            }
        }
    }
}
