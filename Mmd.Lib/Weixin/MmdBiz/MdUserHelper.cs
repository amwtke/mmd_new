using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Redis.MD.WxStatistics;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration.Redis;
using MD.Model.Index.MD;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using Senparc.Weixin.Exceptions;

namespace MD.Lib.Weixin.MmdBiz
{
    /// <summary>
    /// 用于处理微信用户的包装类
    /// </summary>
    public static class MdUserHelper
    {

        public static async Task<UserInfoRedis> SaveUserFromCallback(string appid,string codeFromCallback)
        {
            try
            {
                UserInfoRedis u = await WXComponentUserHelper.GetUserInfoAsync(appid, codeFromCallback);
                if (u == null)
                {
                    MDLogger.LogErrorAsync(typeof(MdUserHelper),
                            new Exception($"存user到Redis报错！appid:{appid},code:{codeFromCallback}"));
                    return null;
                }
                //存入db
                using (var repo = new BizRepository())
                {
                    var mer = await repo.GetMerchantByAppidAsync(appid);
                    if (mer == null || !mer.wx_appid.Equals(appid))
                    {
                        MDLogger.LogErrorAsync(typeof(MdUserHelper),
                            new Exception($"appid:{appid}与mer:appid:{appid}冲突，或者mer为null!"));
                    }
                    var user = new Model.DB.User();
                    user.name = u.NickName;
                    user.openid = u.Openid;
                    user.wx_appid = appid;
                    user.mid = mer.mid;
                    user.sex = int.Parse(u.Sex);
                    await repo.SaveOrUpdateUserAsnyc(user);

                    //存入es,更新redis的uid字段
                    user = await repo.UserGetByOpenIdAsync(user.openid);
                    if (user == null)
                    {
                        MDLogger.LogErrorAsync(typeof(MdUserHelper),
                            new Exception($"存user到ES报错！取不到openid为:{user.openid}的user！"));
                        //return Content($"存user到ES报错！取不到openid为:{user.openid}的user！");
                    }

                    //更新redis的uid
                    var uFromRedis =
                        await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(user.openid);
                    if (uFromRedis != null && uFromRedis.Openid.Equals(user.openid))
                    {
                        uFromRedis.Uid = user.uid.ToString();
                        await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(uFromRedis);
                    }

                    //更新es
                    var indexUser = EsUserManager.GenObject(user);
                    if (indexUser == null)
                    {
                        MDLogger.LogErrorAsync(typeof(MdUserHelper),
                            new Exception($"存user到ES报错！转换成index错误1！openid:{user.openid}"));
                    }
                    if (await EsUserManager.AddOrUpdateAsync(indexUser))
                    {
                        return u;
                    }
                    MDLogger.LogErrorAsync(typeof(MdUserHelper),
                            new Exception($"存user到ES报错！转换成index错误2！openid:{user.openid}"));
                    return null;
                }
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(MdUserHelper), ex);
            }
        }
        /// <summary>
        /// 1-redis;2-db;3-index
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="codeFromCallback"></param>
        /// <returns></returns>
        public static async Task<Tuple<UserInfoRedis,Model.DB.User,IndexUser>> SaveUserFromCallback2(string appid, string codeFromCallback)
        {
            try
            {
                UserInfoRedis u = await WXComponentUserHelper.GetUserInfoAsync(appid, codeFromCallback);
                if (u == null)
                {
                    MDLogger.LogErrorAsync(typeof(MdUserHelper),
                            new Exception($"存user到Redis报错！appid:{appid},code:{codeFromCallback}"));
                    return null;
                }
                //存入db redis es
                using (var repo = new BizRepository())
                {
                    var mer = await repo.GetMerchantByAppidAsync(appid);
                    if (mer == null || !mer.wx_appid.Equals(appid))
                    {
                        MDLogger.LogErrorAsync(typeof(MdUserHelper),
                            new Exception($"appid:{appid}与mer:appid:{appid}冲突，或者mer为null!"));
                        return null;
                    }

                    //更新user db与user es。
                    var user = new Model.DB.User();
                    user.name = EmojiFilter.FilterEmoji(u.NickName);
                    user.openid = u.Openid;
                    user.wx_appid = appid;
                    user.mid = mer.mid;
                    user.sex = int.Parse(u.Sex);
                    await repo.SaveOrUpdateUserAsnyc(user);

                    //更新redis的uid字段
                    user = await repo.UserGetByOpenIdAsync(user.openid);
                    if (user == null)
                    {
                        MDLogger.LogErrorAsync(typeof(MdUserHelper),
                            new Exception($"存user到ES报错！取不到openid为:{user.openid}的user！"));
                    }

                    //更新redis的uid
                    var uFromRedis =
                        await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(user.openid);
                    if (uFromRedis != null && uFromRedis.Openid.Equals(user.openid))
                    {
                        uFromRedis.Uid = user.uid.ToString();
                        await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(uFromRedis);
                    }

                    //更新es
                    var indexUser = EsUserManager.GetById(user.uid);
                    int times = 0;
                    while (indexUser == null && times<=5)
                    {
                        times++;
                        Thread.Sleep(times*100);
                        indexUser = EsUserManager.GetById(user.uid);
                    }
                    if (indexUser == null)
                    {
                        throw new MDException(typeof(MdUserHelper), $"存user到ES报错！转换成index错误3！openid:{user.openid},uid:{user.uid},times:{times}");
                    }

                    return Tuple.Create(u, user, indexUser);

                    //if (await EsUserManager.AddOrUpdateAsync(indexUser))
                    //{
                    //    return Tuple.Create(u,user,indexUser);
                    //}
                    //MDLogger.LogErrorAsync(typeof(MdUserHelper),
                    //        new Exception($"存user到ES报错！转换成index错误2！openid:{user.openid}"));
                    //return null;
                }
            }
            catch (ErrorJsonResultException)
            {
                throw;
                //throw new MDException(typeof(GroupController), ex);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(MdUserHelper), ex);
            }
        }

        /// <summary>
        /// Redis中读取
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        public static async Task<Guid> GetUidByOpenidAsync(string openId)
        {
            try
            {
                if (string.IsNullOrEmpty(openId))
                    return Guid.Empty;
                var u = await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(openId);
                if (u == null)
                    return Guid.Empty;
                return Guid.Parse(u.Uid);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(MdUserHelper), ex);
            }
        }
    }
}
