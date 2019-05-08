using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Redis;
using MD.Model.DB;
using MD.Model.Redis.RedisObjects.Vector;
using Order = StackExchange.Redis.Order;

namespace MD.Lib.DB.Redis.MD
{
    public static class RedisVectorOp
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static RedisVectorOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        #region Type PTCG
        public static async Task<List<KeyValuePair<Guid, double>>> GetPTCGTopAsnyc(Guid uid,int index,int size)
        {
            try
            {
                if (index <= 0)
                    return null;
                int from = (index - 1)*size;

                var ret =
                    await
                        _redis.GetRangeByRankAsync<VectorQMRedis, VectorUserQMZsetAttribute>(uid.ToString(),
                            Order.Descending, from, size);

                List<KeyValuePair<Guid, double>> _list = new List<KeyValuePair<Guid, double>>();

                if (ret != null && ret.Length > 0)
                {

                    foreach (var v in ret)
                    {
                        _list.Add(new KeyValuePair<Guid, double>(Guid.Parse(v.Key),v.Value));
                    }
                    return _list;
                }
                return _list;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(RedisVectorOp),ex);
            }
        }

        /// <summary>
        /// 判断一个人是不是我的好友
        /// </summary>
        /// <param name="myUid">我的uid</param>
        /// <param name="uid">比较的uid</param>
        /// <returns></returns>
        public static async Task<bool> IsExistsQM(Guid myUid, Guid uid)
        {
            var ret = await
                       _redis.GetScoreEveryKeyAsync<VectorQMRedis, VectorUserQMZsetAttribute>(myUid.ToString(),uid.ToString());
            return ret > 0;
        }

        public static async Task<Tuple<int, List<KeyValuePair<string, double>>>> GetPTCGTop2Asnyc(Guid uid, int index, int size = 10)
        {
            try
            {
                if (index <= 0)
                    return null;

                int from = (index - 1) * size;
                int to = from + size-1;
                var ret =
                    await
                        _redis.GetRangeByRankAsync<VectorQMRedis, VectorUserQMZsetAttribute>(uid.ToString(),
                            Order.Descending, from, to);

                List<KeyValuePair<string, double>> _list = new List<KeyValuePair<string, double>>();

                int totalCount =
                    (int)await _redis.GetZsetCountAsync<VectorQMRedis, VectorUserQMZsetAttribute>(uid.ToString());

                if (ret != null && ret.Length > 0)
                {
                    List<Guid> _temp = new List<Guid>();
                    foreach (var v in ret)
                    {
                        _temp.Add(Guid.Parse(v.Key));
                    }

                    using (var repo = new BizRepository())
                    {
                        var _temp2 = await repo.UserGetByGuids(_temp);
                        for (int i = 0; i < ret.Length; i++)
                        {
                            var openid = _temp2[ret[i].Key].openid;
                            _list.Add(new KeyValuePair<string, double>(openid, ret[i].Value));
                        }
                        return Tuple.Create(totalCount,_list);
                    }
                }
                return Tuple.Create(0, _list);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(RedisVectorOp), ex);
            }
        }

        public static async Task<int> GetQMCount(Guid uid)
        {
            try
            {
                int totalCount =
                    (int)await _redis.GetZsetCountAsync<VectorQMRedis, VectorUserQMZsetAttribute>(uid.ToString());
                return totalCount;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(RedisVectorOp), new Exception($"fun:GetQMCount,ex:{ex.Message}"));
            }
        }

        #endregion
    }
}
