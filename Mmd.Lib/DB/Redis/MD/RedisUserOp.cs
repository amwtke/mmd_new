using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Redis;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using StackExchange.Redis;

namespace MD.Lib.DB.Redis.MD
{
    public static class RedisUserOp
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static RedisUserOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }
        /// <summary>
        /// 根据uid去redis取，如果没有则从数据库取
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static async Task<UserInfoRedis> GetByUidAsnyc(Guid uid)
        {
            try
            {
                var ret = await _redis.GetObjectFromRedisHash<UserOpenIdMapRedis>(uid.ToString());
                if (string.IsNullOrEmpty(ret.OpenId))
                {
                    using (var repo = new BizRepository())
                    {
                        var user = await repo.UserGetByUidAsync(uid);
                        if (user != null)
                        {
                            UserOpenIdMapRedis temp = new UserOpenIdMapRedis()
                            {
                                OpenId = user.openid,
                                Uid = uid.ToString()
                            };
                            await _redis.SaveObjectAsync(temp);
                            return await _redis.GetObjectFromRedisHash<UserInfoRedis>(temp.OpenId);
                        }
                        return null;
                    }
                }

                return await _redis.GetObjectFromRedisHash<UserInfoRedis>(ret.OpenId);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(RedisUserOp),ex);
            }
        }

        /// <summary>
        /// 仅用于批量导入关注用户的openid到md.usersub.map.hash，其他勿用
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static async Task<bool> SaveOpenidListAsync(List<HashEntry> list)
        {
            //List<StackExchange.Redis.HashEntry> _pairs = new List<StackExchange.Redis.HashEntry>();

            //foreach (var item in list)
            //{
            //    StackExchange.Redis.HashEntry en = new KeyValuePair<StackExchange.Redis.RedisValue, StackExchange.Redis.RedisValue>(item.OpenId + "_Appid", item.Appid);
            //    _pairs.Add(en);
            //}
            try
            {
                var db = _redis.GetDb(0, null);
                await db.HashSetAsync("md.usersub.map.hash", list.ToArray());
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            
        }

        public static async Task<bool> SaveOpenidAsync(UserSubMapRedis user)
        {
           return await _redis.SaveObjectAsync(user);
        }

        public static bool SaveOpenid(UserSubMapRedis user)
        {
            return  _redis.SaveObject(user);
        }

        public static async Task<bool> IsExistOpenidAsync(string openId)
        {
            return await _redis.HashExistsAsync<UserSubMapRedis>(openId, "Appid");
        }

        public static async Task<bool> DeleteOpenidAsync(string openId)
        {
           return await _redis.DeleteHashItemAsync<UserSubMapRedis>(openId, "Appid");
        }

        public static bool DeleteOpenid(string openId)
        {
            return _redis.DeleteHashItem<UserSubMapRedis>(openId, "Appid");
        }

        /// <summary>
        /// 存储用户在跳转到关注页之前访问的gid或者goid,临时存储，用完即删
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="goid"></param>
        /// <param name="type">开团kt，参团ct</param>
        /// <returns></returns>
        public static bool SaveTmpId(string openId,string goid,string type)
        {
            try
            {
                var db = _redis.GetDb(1, null);
                List<HashEntry> _pairs = new List<HashEntry>();
                HashEntry en = new KeyValuePair<RedisValue, RedisValue>(openId, type  + "," + goid);
                _pairs.Add(en);
                db.HashSet("md.useropenid-goid.hash", _pairs.ToArray());
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string GetTmpId(string openId)
        {
            try
            {
                var db = _redis.GetDb(1, null);
                string goid = db.HashGet("md.useropenid-goid.hash", openId);
                return goid == "nil" ? "" : goid;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static bool DelTmpId(string openId)
        {
            try
            {
                var db = _redis.GetDb(1, null);
                bool flag = db.HashDelete("md.useropenid-goid.hash", openId);
                return flag;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
