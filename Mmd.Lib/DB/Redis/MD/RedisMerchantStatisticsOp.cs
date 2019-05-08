using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Redis;
using MD.Model.DB.Code;
using MD.Model.Redis.RedisObjects.WeChat.Biz;

namespace MD.Lib.DB.Redis.MD
{
    public static class RedisMerchantStatisticsOp
    {
        static RedisManager2<WeChatRedisConfig> _redis = null;
        static RedisMerchantStatisticsOp()
        {
            _redis = new RedisManager2<WeChatRedisConfig>();
        }

        public static async Task<MerchantStatisticsRedis> GetObjectAsync(string mid)
        {
            if (string.IsNullOrEmpty(mid))
                return null;
            var redisObject = await _redis.GetObjectFromRedisHash<MerchantStatisticsRedis>(mid);

            bool needsave = string.IsNullOrEmpty(redisObject.DaoDianInComeTotal) ||
                string.IsNullOrEmpty(redisObject.GroupOrderOkTotal) || string.IsNullOrEmpty(redisObject.GroupOrderTotal) ||
                string.IsNullOrEmpty(redisObject.InComeTotal) || string.IsNullOrEmpty(redisObject.OrderOkTotal) ||
                string.IsNullOrEmpty(redisObject.OrderTotal) || string.IsNullOrEmpty(redisObject.WuLiuInComeTotal);

            //redisObject.mid = mid;
            redisObject.DaoDianInComeTotal = string.IsNullOrEmpty(redisObject.DaoDianInComeTotal)
                ? "0"
                : redisObject.DaoDianInComeTotal;

            redisObject.GroupOrderOkTotal = string.IsNullOrEmpty(redisObject.GroupOrderOkTotal)
                ? "0"
                : redisObject.GroupOrderOkTotal;

            redisObject.GroupOrderTotal = string.IsNullOrEmpty(redisObject.GroupOrderTotal)
                ? "0"
                : redisObject.GroupOrderTotal;

            redisObject.InComeTotal = string.IsNullOrEmpty(redisObject.InComeTotal)
                ? "0"
                : redisObject.InComeTotal;

            redisObject.OrderOkTotal = string.IsNullOrEmpty(redisObject.OrderOkTotal)
                ? "0"
                : redisObject.OrderOkTotal;

            redisObject.OrderTotal = string.IsNullOrEmpty(redisObject.OrderTotal)
                ? "0"
                : redisObject.OrderTotal;

            redisObject.WuLiuInComeTotal = string.IsNullOrEmpty(redisObject.WuLiuInComeTotal)
                ? "0"
                : redisObject.WuLiuInComeTotal;

            if(needsave)
                await _redis.SaveObjectAsync(redisObject);
            return redisObject;
        }

        #region basic op

        public static async Task<int> AddGroupOrderCountAsync(string mid)
        {
            if (string.IsNullOrEmpty(mid))
                return 0;

            var redisObject = await GetObjectAsync(mid);
            int count = int.Parse(redisObject.GroupOrderTotal);
            count += 1;
            redisObject.GroupOrderTotal = (count).ToString();
            await _redis.SaveObjectAsync(redisObject);
            return count;
        }

        public static async Task<bool> SetGroupOrderCountAsync(string mid,int count)
        {
            if (string.IsNullOrEmpty(mid))
                return false;

            var redisObject = await GetObjectAsync(mid);
            redisObject.GroupOrderTotal = count.ToString();
            return await _redis.SaveObjectAsync(redisObject);
        }


        public static async Task<int> AddGroupOrderOkCountAsync(string mid)
        {
            if (string.IsNullOrEmpty(mid))
                return 0;

            var redisObject = await GetObjectAsync(mid);
            int count = int.Parse(redisObject.GroupOrderOkTotal);
            count += 1;
            redisObject.GroupOrderOkTotal = (count).ToString();
            await _redis.SaveObjectAsync(redisObject);
            return count;
        }

        public static async Task<bool> SetGroupOrderOkCountAsync(string mid, int count)
        {
            if (string.IsNullOrEmpty(mid))
                return false;

            var redisObject = await GetObjectAsync(mid);
            redisObject.GroupOrderOkTotal = count.ToString();
            return await _redis.SaveObjectAsync(redisObject);
        }


        public static async Task<int> AddOrderTotalCountAsync(string mid)
        {
            if (string.IsNullOrEmpty(mid))
                return 0;

            var redisObject = await GetObjectAsync(mid);
            int count = int.Parse(redisObject.OrderTotal);
            count += 1;
            redisObject.OrderTotal = (count).ToString();
            await _redis.SaveObjectAsync(redisObject);
            return count;
        }

        public static async Task<bool> SetOrderTotalCountAsync(string mid, int count)
        {
            if (string.IsNullOrEmpty(mid))
                return false;

            var redisObject = await GetObjectAsync(mid);
            redisObject.OrderTotal = count.ToString();
            return await _redis.SaveObjectAsync(redisObject);
        }


        public static async Task<int> AddOrderOkTotalCountAsync(string mid)
        {
            if (string.IsNullOrEmpty(mid))
                return 0;

            var redisObject = await GetObjectAsync(mid);
            int count = int.Parse(redisObject.OrderOkTotal);
            count += 1;
            redisObject.OrderOkTotal = (count).ToString();
            await _redis.SaveObjectAsync(redisObject);
            return count;
        }

        public static async Task<bool> SetOrderOkTotalCountAsync(string mid, int count)
        {
            if (string.IsNullOrEmpty(mid))
                return false;

            var redisObject = await GetObjectAsync(mid);
            redisObject.OrderOkTotal = count.ToString();
            return await _redis.SaveObjectAsync(redisObject);
        }


        public static async Task<int> AddIncomeTotalCountAsync(string mid,int price)
        {
            if (string.IsNullOrEmpty(mid))
                return 0;

            var redisObject = await GetObjectAsync(mid);
            int count = int.Parse(redisObject.InComeTotal);
            count += price;
            redisObject.InComeTotal = (count).ToString();
            await _redis.SaveObjectAsync(redisObject);
            return count;
        }

        public static async Task<bool> SetIncomeTotalCountAsync(string mid, int count)
        {
            if (string.IsNullOrEmpty(mid))
                return false;

            var redisObject = await GetObjectAsync(mid);
            redisObject.InComeTotal = count.ToString();
            return await _redis.SaveObjectAsync(redisObject);
        }

        public static async Task<int> AddIncomeDaoDianTotalCountAsync(string mid,int price)
        {
            if (string.IsNullOrEmpty(mid))
                return 0;

            var redisObject = await GetObjectAsync(mid);
            int count = int.Parse(redisObject.DaoDianInComeTotal);
            count += price;
            redisObject.DaoDianInComeTotal = (count).ToString();
            await _redis.SaveObjectAsync(redisObject);
            return count;
        }

        public static async Task<bool> SetIncomeDaoDianTotalCountAsync(string mid, int count)
        {
            if (string.IsNullOrEmpty(mid))
                return false;

            var redisObject = await GetObjectAsync(mid);
            redisObject.DaoDianInComeTotal = count.ToString();
            return await _redis.SaveObjectAsync(redisObject);
        }

        public static async Task<int> AddIncomeWuLiuTotalCountAsync(string mid,int price)
        {
            if (string.IsNullOrEmpty(mid))
                return 0;

            var redisObject = await GetObjectAsync(mid);
            int count = int.Parse(redisObject.WuLiuInComeTotal);
            count += price;
            redisObject.WuLiuInComeTotal = (count).ToString();
            await _redis.SaveObjectAsync(redisObject);
            return count;
        }

        public static async Task<bool> SetIncomeWuLiuTotalCountAsync(string mid, int count)
        {
            if (string.IsNullOrEmpty(mid))
                return false;

            var redisObject = await GetObjectAsync(mid);
            redisObject.WuLiuInComeTotal = count.ToString();
            return await _redis.SaveObjectAsync(redisObject);
        }

        #endregion

        #region biz

        public static async Task<bool> AfterKtAsync(string mid)
        {
            var redisObject = await GetObjectAsync(mid);
            if (redisObject == null)
                return false;
            //团+1
            await AddGroupOrderCountAsync(mid);

            //await AddOrderTotalCountAsync(mid);

            return true;
        }

        public static async Task<bool> AfterCtAsync(string mid)
        {
            var redisObject = await GetObjectAsync(mid);
            if (redisObject == null)
                return false;

            //await AddOrderTotalCountAsync(mid);

            return true;
        }

        public static async Task<bool> AfterPaySuccessAsync(string mid,int price,int wtg)
        {
            var redisObject = await GetObjectAsync(mid);
            if (redisObject == null)
                return false;
            await AddOrderTotalCountAsync(mid);
            await AddOrderOkTotalCountAsync(mid);
            await AddIncomeTotalCountAsync(mid, price);
            if (wtg.Equals((int)EWayToGet.自提))
                await AddIncomeDaoDianTotalCountAsync(mid, price);
            else
                await AddIncomeWuLiuTotalCountAsync(mid, price);
            return true;
        }

        public static async Task<bool> AfterRefundAsync(string appid, string otn)
        {
            try
            {
                string mid;
                int price, wtg;
                using (var repo = new BizRepository())
                {
                    var mer = await repo.GetMerchantByAppidAsync(appid);
                    mid = mer.mid.ToString();
                    var order = await repo.OrderGetByOutTradeNoAsync(otn);
                    price = order.actual_pay.Value;
                    wtg = order.waytoget.Value;
                }

                var redisObject = await GetObjectAsync(mid);
                if (redisObject == null)
                    return false;
                var temp = int.Parse(redisObject.OrderOkTotal);

                await SetOrderOkTotalCountAsync(mid, temp - 1);
                await AddIncomeTotalCountAsync(mid, -price);
                if (wtg.Equals((int)EWayToGet.自提))
                    await AddIncomeDaoDianTotalCountAsync(mid, -price);
                else
                    await AddIncomeWuLiuTotalCountAsync(mid, -price);
                return true;
            }
            catch (Exception ex)
            {
                
                throw new MDException(typeof(RedisMerchantStatisticsOp),ex);
            }
        }

        public static async Task<bool> AfterGroupOrderOk(Guid gid)
        {
            try
            {
                var group = await EsGroupManager.GetByGidAsync(gid);

                var redisObject = await GetObjectAsync(group.mid);
                if (redisObject == null)
                    return false;
                await AddGroupOrderOkCountAsync(group.mid);
                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(RedisMerchantStatisticsOp), ex);
            }
        }
        #endregion
    }
}
