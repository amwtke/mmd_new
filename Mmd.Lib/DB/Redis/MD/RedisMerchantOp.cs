using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Redis;
using MD.Model.DB;
using MD.Model.Redis.RedisObjects.WeChat.Biz.Merchant;
using Nest;

namespace MD.Lib.DB.Redis.MD
{
    public static class RedisMerchantOp
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static RedisMerchantOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        static MerchantRedis GenObject(Merchant obj)
        {
            if (obj == null || obj.mid.Equals(Guid.Empty))
                return null;

            MerchantRedis r = new MerchantRedis()
            {
                address = obj.address,
                advertise_pic_url = obj.advertise_pic_url,
                biz_licence_url = obj.biz_licence_url,
                brief_introduction = obj.brief_introduction,
                cell_phone = obj.cell_phone==null?null:obj.cell_phone.ToString(),
                contact_person = obj.contact_person,
                default_post_company = obj.default_post_company==null?null:obj.default_post_company.ToString(),
                logo_url = obj.logo_url,
                mid = obj.mid.ToString(),
                name = obj.name,
                order_quota = obj.order_quota==null?null:obj.order_quota.ToString(),
                qr_url = obj.qr_url,
                register_date = obj.register_date==null?null:CommonHelper.FromUnixTime(obj.register_date.Value).ToString("yyyy-MM-dd HH:mm:ss"),
                service_intro = obj.service_intro,
                service_region = obj.service_region,
                slogen = obj.slogen,
                status = obj.status==null?null:obj.status.ToString(),
                tel = obj.tel,
                title = obj.title,
                wx_apikey = obj.wx_apikey,
                wx_appid = obj.wx_appid,
                wx_biz_dir = obj.wx_biz_dir,
                wx_jspay_dir = obj.wx_jspay_dir,
                wx_mch_id = obj.wx_mch_id,
                wx_mp_id = obj.wx_mp_id,
                wx_p12_dir = obj.wx_p12_dir,
                wx_pay_dir = obj.wx_pay_dir,
                extension_1 = obj.extension_1,
                extension_2 = obj.extension_2,
                extension_3 = obj.extension_3
            };
            return r;
        }

        public static async Task<MerchantRedis> GetByAppidAsync(string appid)
        {
            if (string.IsNullOrEmpty(appid))
                return null;
            var appidMap = await _redis.GetObjectFromRedisHash<MerchantAppidMapRedis>(appid);
            if (string.IsNullOrEmpty(appidMap.mid))
            {
                using (var repo = new BizRepository())
                {
                    var mer = await repo.GetMerchantByAppidAsync(appid);
                    if (mer == null)
                        return null;
                    appidMap.mid = mer.mid.ToString();
                    await _redis.SaveObjectAsync(appidMap);
                }

                appidMap = await _redis.GetObjectFromRedisHash<MerchantAppidMapRedis>(appid);
            }

            return await _redis.GetObjectFromRedisHash<MerchantRedis>(appidMap.mid);
        }

        public static async Task<MerchantRedis> GetByMidAsync(Guid mid)
        {
            try
            {
                var r = await _redis.GetObjectFromRedisHash<MerchantRedis>(mid.ToString());
                if (string.IsNullOrEmpty(r.wx_appid))
                {
                    using (var repo = new BizRepository())
                    {
                        var mer = await repo.GetMerchantByMidAsync(mid);
                        if (!string.IsNullOrEmpty(mer?.wx_appid))
                        {
                            r = GenObject(mer);
                            if (r != null)
                            {
                                await _redis.SaveObjectAsync(r);
                                return r;
                            }
                        }
                        return null;
                    }
                }
                return r;
            }
            catch (Exception ex)
            {
                
                throw new MDException(typeof(RedisMerchantOp),ex);
            }
        }

        public static MerchantRedis GetByMid(Guid mid)
        {
            try
            {
                var r = _redis.GetObjectFromRedisHash_TongBu<MerchantRedis>(mid.ToString());
                if (string.IsNullOrEmpty(r.wx_appid))
                {
                    using (var repo = new BizRepository())
                    {
                        var mer = repo.GetMerchantByMid(mid);
                        if (!string.IsNullOrEmpty(mer?.wx_appid))
                        {
                            r = GenObject(mer);
                            if (r != null)
                            {
                                _redis.SaveObject(r);
                                return r;
                            }
                        }
                        return null;
                    }
                }
                return r;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(RedisMerchantOp), ex);
            }
        }

        public static async Task<MerchantRedis> UpdateFromDbAsync(Guid mid)
        {
            try
            {
                using (var repo = new BizRepository())
                {
                    var mer = await repo.GetMerchantByMidAsync(mid);
                    var ret = GenObject(mer);
                    if (!string.IsNullOrEmpty(ret?.wx_appid))
                    {
                        await _redis.SaveObjectAsync(ret);
                        return ret;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(RedisMerchantOp), ex);
            }
        }

        public static async Task<MerchantRedis> UpdateFromDbAsync(Merchant dbObj)
        {
            try
            {
                var redisObject = GenObject(dbObj);
                if (!string.IsNullOrEmpty(redisObject?.wx_appid))
                {
                    await _redis.SaveObjectAsync(redisObject);
                    return redisObject;
                }
                return null;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(RedisMerchantOp), ex);
            }
        }

        public static bool IsSupperUser(Guid mid)
        {
            try
            {
                var mer = GetByMid(mid);
                if (string.IsNullOrEmpty(mer?.extension_1))
                {
                    return false;
                }
                return mer.extension_1.Equals("1");
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(RedisMerchantOp), ex);
            }
        }

        public static bool IsSupperUser(string mid)
        {
            try
            {
                if (string.IsNullOrEmpty(mid))
                    return false;

                var mer = GetByMid(Guid.Parse(mid));
                if (string.IsNullOrEmpty(mer?.extension_1))
                {
                    return false;
                }
                return mer.extension_1.Equals("1");
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(RedisMerchantOp), ex);
            }
        }
    }
}
