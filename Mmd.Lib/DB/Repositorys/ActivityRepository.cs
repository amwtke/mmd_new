using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Context;
using MD.Lib.DB.Redis;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Redis;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.DB.Activity;
using MD.Lib.ElasticSearch.MD;
using System.Reflection;

namespace MD.Lib.DB.Repositorys
{
    public class ActivityRepository : IDisposable
    {
        private readonly MDActivityContext context;
        public MDActivityContext Context => context;
        #region Constructor

        public ActivityRepository()
        {
            context = new MDActivityContext();
        }

        public ActivityRepository(MDActivityContext context)
        {
            this.context = context;
        }

        #endregion
        #region IDisposable Support

        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.context.Dispose();
                }
                disposedValue = true;
            }
        }

        ~ActivityRepository()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }

        // 添加此代码以正确实现可处置模式。
        void IDisposable.Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Box
        public async Task<Box> BoxAddOrUpdateAsync(Box b)
        {
            if (b == null)
                return null;
            var box = await GetBoxByIdAsync(b.bid);
            //新加入
            if (box == null)
            {
                context.Box.Add(b);
                await context.SaveChangesAsync();
                await EsAct_boxManager.AddOrUpdateAsync(EsAct_boxManager.GenObject(b));
                return box;
            }
            else //更新
            {
                foreach (var pi in typeof(Box).GetProperties())
                {
                    if (pi.Name.Contains("bid"))
                        continue;
                    var parameterValue = pi.GetValue(b);
                    if (parameterValue != null)
                    {
                        pi.SetValue(box, parameterValue);
                    }
                }
                await EsAct_boxManager.AddOrUpdateAsync(EsAct_boxManager.GenObject(b));
            }
            await context.SaveChangesAsync();
            return b;
        }

        public async Task<bool> UpdateBoxAsync(Box b)
        {
            if (b == null)
                return false;
            var box = await GetBoxByIdAsync(b.bid);
            foreach (var pi in typeof(Box).GetProperties())
            {
                if (pi.Name.Contains("bid"))
                    continue;
                var parameterValue = pi.GetValue(b);
                if (parameterValue != null)
                {
                    pi.SetValue(box, parameterValue);
                }
            }
            await EsAct_boxManager.AddOrUpdateAsync(EsAct_boxManager.GenObject(b));
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<Box> GetBoxByIdAsync(Guid bid)
        {
            var ret = await (from b in context.Box where b.bid.Equals(bid) select b).FirstOrDefaultAsync();
            return ret;
        }

        public async Task<List<Box>> GetBoxByMidAsync(Guid mid)
        {
            var ret = await (from b in context.Box where b.mid.Equals(mid) select b).ToListAsync();
            return ret;
        }

        public async Task<Tuple<int, List<Box>>> GetBoxListByMidAsync(Guid mid, int status, int pageIndex, int pageSize)
        {
            var list = await (from b in context.Box where b.mid.Equals(mid) orderby b.last_update_time descending select b).ToListAsync();
            int count = await BoxCountByStatus(mid, status);
            return Tuple.Create(count, list);
        }

        public async Task<Tuple<int, int, int>> GetStaCountByMidAsync(Guid bid)
        {
            int countTotal = await (from b in context.BoxTreasure where b.bid.Equals(bid) select b.count).SumAsync();
            int countOpen = await (from ut in context.UserTreasure where ut.bid.Equals(bid) && ut.status == (int)EUserTreasureStatus.未核销 select ut).CountAsync();
            int countCheck = await (from ut in context.UserTreasure where ut.bid.Equals(bid) && ut.status == (int)EUserTreasureStatus.已核销 select ut).CountAsync();
            return Tuple.Create(countTotal, countOpen, countCheck);
        }

        public async Task<int> BoxCountByStatus(Guid mid, int status)
        {

            //var count =
            //    await (from b in context.Box where b.status == status && b.mid.Equals(mid) select b).CountAsync();
            var count =
                await (from b in context.Box where b.mid.Equals(mid) select b).CountAsync();
            return count;

        }
        #endregion

        # region BoxTreasure
        public async Task<BoxTreasure> BoxTreasureAddOrUpdateAsync(BoxTreasure bt)
        {
            if (bt == null)
                return null;
            var treasure = await GetBoxTreasureByIdAsync(bt.btid);

            //新加入
            if (treasure == null)
            {
                context.BoxTreasure.Add(bt);
                await context.SaveChangesAsync();
                await EsAct_boxtreasureManager.AddOrUpdateAsync(EsAct_boxtreasureManager.GenObject(bt));
                return treasure;
            }
            else //更新
            {
                foreach (var pi in typeof(BoxTreasure).GetProperties())
                {
                    if (pi.Name.Contains("btid") || pi.Name.Contains("bid"))
                        continue;
                    var parameterValue = pi.GetValue(bt);
                    if (parameterValue != null)
                    {
                        pi.SetValue(treasure, parameterValue);
                    }
                }
                await EsAct_boxtreasureManager.AddOrUpdateAsync(EsAct_boxtreasureManager.GenObject(bt));
            }
            await context.SaveChangesAsync();
            return bt;
        }

        public async Task<BoxTreasure> GetBoxTreasureByIdAsync(Guid btid)
        {
            var ret = await (from b in context.BoxTreasure where b.btid.Equals(btid) select b).FirstOrDefaultAsync();
            return ret;
        }

        public async Task<List<BoxTreasure>> GetBoxTreasureByBidAsync(Guid bid)
        {
            var ret = await (from b in context.BoxTreasure where b.bid.Equals(bid) select b).ToListAsync();
            return ret;
        }
        #endregion

        #region UserTreasure
        public async Task<UserTreasure> GetUserTreasureByUtidAsync(Guid utid)
        {
            var ret = await (from u in context.UserTreasure where u.utid.Equals(utid) select u).FirstOrDefaultAsync();
            return ret;
        }

        public async Task<bool> AddOrUpdateUsertreaAsync(UserTreasure ut)
        {
            try
            {
                if (ut == null)
                    return false;
                var usert = await GetUserTreasureByUtidAsync(ut.utid);
                if (usert == null)
                {
                    if (ut.utid.Equals(Guid.Empty))
                        ut.utid = Guid.NewGuid();
                    context.UserTreasure.Add(ut);
                    return await context.SaveChangesAsync() == 1;
                }
                else //更新
                {
                    foreach (var pi in typeof(UserTreasure).GetProperties())
                    {
                        var parameterValue = pi.GetValue(ut);
                        if (parameterValue != null)
                        {
                            pi.SetValue(usert, parameterValue);
                        }
                    }
                }
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(ActivityRepository), ex);
            }
        }

        /// <summary>
        /// 核销自动减宝贝数量
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateQuota_countAsync(Guid btid)
        {
            try
            {
                var boxt = await (from j in context.BoxTreasure where j.btid.Equals(btid) select j).FirstOrDefaultAsync();
                if (boxt != null)
                {
                    boxt.quota_count = boxt.quota_count - 1;
                    return await context.SaveChangesAsync() == 1;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(ActivityRepository), ex);
            }
        }
        #endregion

        #region Sign
        public async Task<Sign> SignAddOrUpdateAsync(Sign s)
        {
            if (s == null)
                return null;
            var sign = await GetSignByIdAsync(s.sid);
            //新加入
            if (sign == null)
            {
                context.Sign.Add(s);
                await context.SaveChangesAsync();
                await EsAct_signManager.AddOrUpdateAsync(await EsAct_signManager.GenObjectAsync(s));
                return s;
            }
            else //更新
            {
                foreach (var pi in typeof(Sign).GetProperties())
                {
                    if (pi.Name.Contains("sid"))
                        continue;
                    var parameterValue = pi.GetValue(s);
                    if (parameterValue != null)
                    {
                        pi.SetValue(sign, parameterValue);
                    }
                }
                await EsAct_signManager.AddOrUpdateAsync(await EsAct_signManager.GenObjectAsync(s));
            }
            await context.SaveChangesAsync();
            return s;
        }

        public async Task<bool> UpdateSignAsync(Sign s)
        {
            if (s == null)
                return false;
            var sign = await GetSignByIdAsync(s.sid);
            foreach (var pi in typeof(Sign).GetProperties())
            {
                if (pi.Name.Contains("sid"))
                    continue;
                var parameterValue = pi.GetValue(s);
                if (parameterValue != null)
                {
                    pi.SetValue(sign, parameterValue);
                }
            }
            await EsAct_signManager.AddOrUpdateAsync(await EsAct_signManager.GenObjectAsync(s));
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<Sign> GetSignByIdAsync(Guid sid)
        {
            return await context.Sign.FirstOrDefaultAsync(s => s.sid == sid);
            //var ret = await (from s in context.Sign where s.sid.Equals(sid) select s).FirstOrDefaultAsync();
            //return ret;
        }

        public async Task<Tuple<int, List<Sign>>> GetSignListByMidAsync(Guid mid, int pageIndex, int pageSize)
        {
            int count = await context.Sign.Where(s => s.mid == mid).CountAsync();
            int index = (pageIndex - 1) * pageSize;
            List<Sign> list = await context.Sign.Where(s => s.mid == mid).OrderByDescending(p => p.last_update_time).Skip(index).Take(pageSize).ToListAsync();
            return Tuple.Create(count, list);
        }

        public async Task<Tuple<int, int>> GetSignCountByMidAsync(Guid sid)
        {
            int userSignCount = await context.UserSign.Where(us => us.sid == sid).CountAsync();
            int status = (int)EUserSignStatus.已领取;
            int userCheckCount = await context.UserSign.Where(us => us.sid == sid && us.status == status).CountAsync();
            return Tuple.Create(userSignCount, userCheckCount);
        }

        public async Task<Sign> UpdateSignQuota_countAsync(Guid sid)
        {
            try
            {
                var sign = await (from j in context.Sign where j.sid.Equals(sid) select j).FirstOrDefaultAsync();
                if (sign != null)
                {
                    sign.awardQuatoCount = sign.awardQuatoCount - 1;
                    if (await context.SaveChangesAsync() == 1)
                        return sign;
                }
                return sign;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(ActivityRepository), ex);
            }
        }
        #endregion

        #region UserSign
        public async Task<UserSign> GetUserSignByIdAsync(Guid usid)
        {
            var us = await (from j in context.UserSign where j.usid.Equals(usid) select j).FirstOrDefaultAsync();
            return us ?? new UserSign();
        }
        public async Task<bool> AddOrUpdateUserSignAsync(UserSign us)
        {
            if (us == null)
                return false;
            var usersign = await GetUserSignByIdAsync(us.usid);
            //新加入
            if (usersign == null || usersign.usid.Equals(Guid.Empty))
            {
                context.UserSign.Add(us);
                return await context.SaveChangesAsync() == 1;
            }
            else //更新
            {
                foreach (var pi in typeof(UserSign).GetProperties())
                {
                    var parameterValue = pi.GetValue(us);
                    if (parameterValue != null)
                    {
                        pi.SetValue(usersign, parameterValue);
                    }
                }
            }
            return await context.SaveChangesAsync() == 1;
        }
        #endregion


        #region LadderGroup

        public async Task<LadderGroup> GroupAddOrUpdateAsync(LadderGroup g)
        {
            if (g == null)
                return null;
            var group = await GetGroupByIdAsync(g.gid);
            //新加入
            if (group == null)
            {
                context.LadderGroup.Add(g);
                await context.SaveChangesAsync();
                await EsLadderGroupManager.AddOrUpdateAsync(EsLadderGroupManager.GenObject(g));
                return g;
            }
            else //更新
            {
                foreach (var pi in typeof(LadderGroup).GetProperties())
                {
                    if (pi.Name.Contains("gid"))
                        continue;
                    var parameterValue = pi.GetValue(g);
                    if (parameterValue != null)
                    {
                        pi.SetValue(group, parameterValue);
                    }
                }
                await EsLadderGroupManager.AddOrUpdateAsync(EsLadderGroupManager.GenObject(g));
            }
            await context.SaveChangesAsync();
            return g;
        }
        /// <summary>
        /// 修改库存并更新ES
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        public async Task<bool> GroupUpdateQuotacount(LadderGroup g)
        {
            if (g == null)
                return false;
            var group = await GetGroupByIdAsync(g.gid);
            group.product_quotacount = group.product_quotacount - 1;
            await context.SaveChangesAsync();
            await EsLadderGroupManager.AddOrUpdateAsync(EsLadderGroupManager.GenObject(group));
            return true;
        }

        public async Task<LadderGroup> GetGroupByIdAsync(Guid gid)
        {
            var ret = await (from g in context.LadderGroup where g.gid.Equals(gid) select g).FirstOrDefaultAsync();
            if (ret != null)
            {
                ret.PriceList = await (from g in context.LadderPrice
                                       where g.gid.Equals(gid)
                                       orderby g.person_count
                                       select g).ToListAsync();
            }
            return ret;
        }
        public LadderGroup GetGroupById(Guid gid)
        {
            var ret = (from g in context.LadderGroup where g.gid.Equals(gid) select g).FirstOrDefault();
            if (ret != null)
            {
                ret.PriceList = (from g in context.LadderPrice
                                 where g.gid.Equals(gid)
                                 orderby g.person_count
                                 select g).ToList();
            }
            return ret;
        }

        public async Task<List<LadderGroup>> GetGroupByMidAsync(Guid mid)
        {
            var ret = await (from b in context.LadderGroup where b.mid.Equals(mid) select b).ToListAsync();
            return ret;
        }

        public async Task<Tuple<int, List<LadderGroup>>> GetGroupListByMidAsync(Guid mid, int status, int pageIndex, int pageSize)
        {
            var list = await (from b in context.LadderGroup where b.mid.Equals(mid) && b.status == status orderby b.last_update_time descending select b).ToListAsync();
            int count = await GroupCountByStatus(mid, status);
            return Tuple.Create(count, list);
        }

        public async Task<int> GroupCountByStatus(Guid mid, int status)
        {
            return await context.LadderGroup.CountAsync(g => g.mid == mid && g.status == status);
        }

        public async Task<Tuple<int,int,int ,double>> GetGroupStaCount(Guid gid)
        {
            int status = (int)ELadderOrderStatus.拼团成功;//已核销
            var listGroupOrder = await (from go in context.LadderGroupOrder where go.gid.Equals(gid) select go).ToListAsync();
            int groupCountOpen = listGroupOrder.Count;
            if (groupCountOpen > 0)
            {
                var listOrder = await GetOrderByGidAsync(gid);
                int userCountTotal = listOrder.Count;
                int orderCountH = listOrder.Where(p => p.status.Equals(status)).Count();
                var query = from o in listOrder join go in listGroupOrder on o.goid equals go.goid where o.status == status select go;
                double orderAmount = orderCountH == 0 ? 0 : query.Sum(go => go.go_price);
                return Tuple.Create(groupCountOpen, userCountTotal, orderCountH, orderAmount);
            }
            else
            {
                return Tuple.Create(0,0,0,0.00);
            }
        }

        public async Task<List<LadderPrice>> LadderPriceGetByGidAsync(Guid gid)
        {
            var list = await context.LadderPrice.Where(l => l.gid == gid).OrderBy(p => p.person_count).ToListAsync();
            return list;
        }

        public async Task<bool> LadderPriceAddAsync(List<LadderPrice> listLp)
        {
            if (listLp != null && listLp.Count > 0)
            {
                Guid gid = listLp[0].gid;
                context.LadderPrice.RemoveRange(context.LadderPrice.Where(l => l.gid == gid));
                await context.SaveChangesAsync();
                context.LadderPrice.AddRange(listLp);
                int res = await context.SaveChangesAsync();
                return res > 0;
            }
            else
            {
                return false;
            }
        }
        #endregion
        #region ladder_order
        public async Task<LadderOrder> GetOrderByOidAsync(Guid oid)
        {
            var order = await (from o in context.LadderOrder where o.oid.Equals(oid) select o).FirstOrDefaultAsync();
            return order;
        }
        public async Task<List<LadderOrder>> GetOrderByGidAsync(Guid Gid)
        {
            var orderlist = await (from o in context.LadderOrder where o.gid.Equals(Gid) select o).ToListAsync();
            return orderlist;
        }
        public async Task<LadderOrder> OrderCreate(LadderGroupOrder go, LadderGroup group, Guid buyer, int fee)
        {
            if (go == null || buyer.Equals(Guid.Empty))
                return null;
            try
            {
                //是否重复,null的时候才能生成新订单。
                var tempOrder = await (from o in context.LadderOrder where o.gid.Equals(go.gid)&& o.buyer.Equals(buyer) select o).FirstOrDefaultAsync();
                if (tempOrder != null)
                {
                    return tempOrder;
                }

                //新加入
                LadderOrder order = new LadderOrder()
                {
                    oid = Guid.NewGuid(),
                    o_no = CommonHelper.GetId32(EIdPrefix.OD),
                    buyer = buyer,
                    goid = go.goid,
                    mid = group.mid,
                    order_price = fee,
                    waytoget = group.waytoget.Value,
                    status = (int)ELadderOrderStatus.已支付,
                    gid = group.gid,
                    paytime = CommonHelper.GetUnixTimeNow(),
                };
                context.LadderOrder.Add(order);
                await context.SaveChangesAsync();
                return order;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 修改order并更新es
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<bool> OrderUpDateAsync(LadderOrder obj)
        {
            try
            {
                if (obj.oid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.o_no))
                    return false;
                var dbObject = await GetOrderByOidAsync(obj.oid);
                if (dbObject == null)
                    return false;
                foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                {
                    var v = pi.GetValue(obj);
                    if (v != null)
                        pi.SetValue(dbObject, v);
                }
                var ret = await context.SaveChangesAsync();

                await EsLadderOrderManager.AddOrUpdateAsync(EsLadderOrderManager.GenObject(dbObject));

                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 只单纯修改go状态，并更新ES
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<bool> OrderUpDateStatusAsync(LadderOrder obj)
        {
            try
            {
                if (obj.oid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.o_no))
                    return false;
                var dbObject = await GetOrderByOidAsync(obj.oid);
                if (dbObject == null)
                    return false;
                dbObject.status = obj.status;
                var ret = await context.SaveChangesAsync();

                await EsLadderOrderManager.AddOrUpdateAsync(EsLadderOrderManager.GenObject(dbObject));

                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<LadderOrder>> GetOrderByGoidAsync(Guid goid, int status)
        {
            var list = await (from o in context.LadderOrder where o.goid.Equals(goid) && o.status.Equals(status) select o).ToListAsync();
            return list;
        }

        public int GetOrderCountByGoid(Guid goid)
        {
            return (from o in context.LadderOrder where o.goid.Equals(goid) select o).Count();
        }
        #endregion
        #region ladder_grouporder
        public async Task<LadderGroupOrder> GroupOrderGetAsync(Guid goid)
        {
            try
            {
                var ret = await (from go in context.LadderGroupOrder where go.goid.Equals(goid) select go).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        private async Task<LadderGroupOrder> IsDupulicateOpenGoAsync(Guid gId, Guid leader)
        {
            try
            {
                var ret =
                    await
                        (from go in context.LadderGroupOrder
                         where go.gid.Equals(gId) && go.leader.Equals(leader)
                         select go).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 更新go和ES
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<bool> GroupOrderUpdateAsync(LadderGroupOrder obj)
        {
            try
            {
                if (obj.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.go_no))
                    return false;
                var dbObject = await GroupOrderGetAsync(obj.goid);
                if (dbObject == null)
                    return false;
                foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                {
                    var v = pi.GetValue(obj);
                    if (v != null)
                        pi.SetValue(dbObject, v);
                }
                var ret = await context.SaveChangesAsync();
                var indexgo = EsLadderGroupOrderManager.GenObject(dbObject);
                await EsLadderGroupOrderManager.AddOrUpdateAsync(indexgo);
                return true;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 只单纯修改go状态，并更新ES
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<bool> GroupOrderUpdateStatusAsync(LadderGroupOrder obj)
        {
            try
            {
                if (obj.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.go_no))
                    return false;
                var dbObject = await GroupOrderGetAsync(obj.goid);
                if (dbObject == null)
                    return false;
                dbObject.status = obj.status;
                dbObject.go_price = obj.go_price;
                var ret = await context.SaveChangesAsync();
                var indexgo = EsLadderGroupOrderManager.GenObject(dbObject);
                await EsLadderGroupOrderManager.AddOrUpdateAsync(indexgo);
                return true;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public async Task<LadderGroupOrder> GroupOderOpenAsync(LadderGroup group, Guid LeaderUuid, int fee)
        {
            if (group == null || LeaderUuid.Equals(Guid.Empty))
                return null;
            try
            {
                //重复开团
                var goDupli = await IsDupulicateOpenGoAsync(group.gid, LeaderUuid);
                if (goDupli != null)
                {
                    //重开未支付的团订单，需要更新团订单的过期时间，订单的支付时间。
                    if (goDupli.status == (int)ELadderGroupOrderStatus.拼团进行中)
                    {
                        //更新团订单过期时间
                        goDupli.create_date = CommonHelper.GetUnixTimeNow();
                        DateTime expiDateDupli = DateTime.Now.AddSeconds(86400);
                        goDupli.expire_date = CommonHelper.ToUnixTime(expiDateDupli);
                        await GroupOrderUpdateAsync(goDupli);
                    }
                    return goDupli;
                }


                //新开团
                LadderGroupOrder newGo = new LadderGroupOrder();
                newGo.goid = Guid.NewGuid();
                newGo.go_no = CommonHelper.GetId32(EIdPrefix.GO);
                newGo.gid = group.gid;
                newGo.leader = LeaderUuid;
                newGo.create_date = CommonHelper.GetUnixTimeNow();
                newGo.pid = group.pid;
                newGo.price = group.origin_price;
                newGo.go_price = fee;
                newGo.mid = group.mid;
                //截止日期
                DateTime expiDate = DateTime.Now.AddSeconds(86400);
                newGo.expire_date = CommonHelper.ToUnixTime(expiDate);
                newGo.status = (int)ELadderGroupOrderStatus.拼团进行中;
                context.LadderGroupOrder.Add(newGo);
                await context.SaveChangesAsync();
                return newGo;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 已过期的团订单
        /// </summary>
        /// <returns></returns>
        public List<LadderGroupOrder> GroupOrderGetFailsByTimeLimits()
        {
            try
            {
                var dnow = CommonHelper.GetUnixTimeNow();
                var ret = (from go in context.LadderGroupOrder
                           where go.expire_date < dnow && go.status == (int)ELadderGroupOrderStatus.拼团进行中
                           select go).ToList();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        #endregion

    }
}
