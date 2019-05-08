using MD.Model.Configuration.Redis;
using MD.Model.Redis;
using MD.Model.Redis.Objects.Ids;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.DB.Redis.Objects
{
    public static class OpenIdToUserUUIDOp
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static OpenIdToUserUUIDOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        public static async Task<bool> SaveAsync(string openid,Guid userid)
        {
            OpenIdToUserUuidHash o = new OpenIdToUserUuidHash();
            o.OpenId = openid;
            o.UserUuid = userid.ToString().ToUpper();
            return await _redis.SaveObjectAsync(o);
        }

        public static async Task<Guid> GetUuidByOpenIdAsync(string openid)
        {
            var o = await _redis.GetObjectFromRedisHash<OpenIdToUserUuidHash>(openid);
            if (string.IsNullOrEmpty(o.UserUuid))
                return Guid.Empty;
            return Guid.Parse(o.UserUuid);
        }

        public static async Task<bool> DeleteItemAsync(string openid)
        {
            return await _redis.DeleteHashItemAsync<OpenIdToUserUuidHash>(openid, "UserUuid");
        }
    }
}
