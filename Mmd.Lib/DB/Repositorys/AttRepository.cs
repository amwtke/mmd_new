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
using MD.Model.Redis.RedisObjects.WeChat.Biz.AttTable;

namespace MD.Lib.DB.Repositorys
{
    public enum EGroupAtt
    {
        leader_price,
        userobot,
        order_limit,
        activity_point,
        group_type,
        lucky_count,
        lucky_endTime,
        lucky_status
    }
    public enum EOrderAtt
    {
        luckyStatus
    }

    public enum EAttTables
    {
        Group,
        Order
    }

    /// <summary>
    /// 自定义属性操作
    /// </summary>
    public class AttRepository : IDisposable
    {
        private readonly MdAttContext context;
        public MdAttContext Context => context;
        //public MdAttContext Context => context;

        #region Constructor

        public AttRepository()
        {
            context = new MdAttContext();
        }

        public AttRepository(MdAttContext context)
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

        ~AttRepository()
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

        #region Operation

        #region AttNameOperation

        public async Task<List<AttName>> AttNameGetallAsync()
        {
            try
            {
                return await context.AttNames.ToListAsync();
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public List<AttName> AttNameGetall()
        {
            try
            {
                return context.AttNames.ToList();
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }
        public async Task<bool> AddAttNameAsync(string attName, string targetTableName, string unit, string description)
        {
            try
            {
                var count =
                    await
                        (from a in context.AttNames
                         where a.att_name.Equals(attName) && a.table_name.Equals(targetTableName)
                         select a).CountAsync();
                if (count == 0)
                {
                    AttName at = new AttName()
                    {
                        attid = Guid.NewGuid(),
                        att_name = attName,
                        description = description,
                        table_name = targetTableName,
                        unit = unit
                    };
                    context.AttNames.Add(at);
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public async Task<AttName> AttNameGetAsync(Guid atid)
        {
            try
            {
                var attname =
                    await (from at in context.AttNames where at.attid.Equals(atid) select at).FirstOrDefaultAsync();
                return attname;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public async Task<AttName> AttNameGetAsync(string table_name, string att_name)
        {
            try
            {
                var attname =
                    await (from at in context.AttNames
                           where at.table_name.Equals(table_name) && at.att_name.Equals(att_name)
                           select at).FirstOrDefaultAsync();
                return attname;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public AttName AttNameGet(string table_name, string att_name)
        {
            try
            {
                var attname =
                    (from at in context.AttNames
                     where at.table_name.Equals(table_name) && at.att_name.Equals(att_name)
                     select at).FirstOrDefault();
                return attname;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public async Task<bool> DelAttNameAsync(Guid attid)
        {
            try
            {
                var obj =
                    await
                        (from a in context.AttNames where a.attid.Equals(attid) select a).FirstOrDefaultAsync();
                if (obj != null)
                {
                    context.AttNames.Remove(obj);
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        #endregion

        #region AttValueOperation

        public async Task<bool> AttValueAddOrUpdateValueAsync(Guid ownerId, Guid attid, string value)
        {
            try
            {
                var attValue =
                    await
                        (from v in context.AttValues where v.owner.Equals(ownerId) && v.attid.Equals(attid) select v)
                            .FirstOrDefaultAsync();
                //存在则更新
                if (attValue != null && attValue.timestamp > 0)
                {
                    attValue.value = value;
                    attValue.timestamp = CommonHelper.GetUnixTimeNow();
                    await context.SaveChangesAsync();
                    return true;
                }

                //新建
                AttValue newValue = new AttValue()
                {
                    attid = attid,
                    owner = ownerId,
                    value = value,
                    timestamp = CommonHelper.GetUnixTimeNow()
                };
                context.AttValues.Add(newValue);
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public bool AttValueAddOrUpdateValue(Guid ownerId, Guid attid, string value)
        {
            try
            {
                var attValue =
                        (from v in context.AttValues where v.owner.Equals(ownerId) && v.attid.Equals(attid) select v)
                            .FirstOrDefault();
                //存在则更新
                if (attValue != null && attValue.timestamp > 0)
                {
                    attValue.value = value;
                    attValue.timestamp = CommonHelper.GetUnixTimeNow();
                    context.SaveChanges();
                    return true;
                }

                //新建
                AttValue newValue = new AttValue()
                {
                    attid = attid,
                    owner = ownerId,
                    value = value,
                    timestamp = CommonHelper.GetUnixTimeNow()
                };
                context.AttValues.Add(newValue);
                return context.SaveChanges() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public async Task<AttValue> AttValueGetAsync(Guid owner, Guid attid)
        {
            try
            {
                var val =
                    await
                        (from v in context.AttValues where v.owner.Equals(owner) && v.attid.Equals(attid) select v)
                            .FirstOrDefaultAsync();
                return val;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public AttValue AttValueGet(Guid owner, Guid attid)
        {
            try
            {
                var val =

                        (from v in context.AttValues where v.owner.Equals(owner) && v.attid.Equals(attid) select v)
                            .FirstOrDefault();
                return val;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        public async Task<List<AttValue>> AttValueGetAsync(Guid owner)
        {
            try
            {
                var list = await (from v in context.AttValues where v.owner.Equals(owner) select v).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }
        public async Task<List<Guid>> AttValueGetByAttid(Guid attid,string value)
        {
            try
            {
                var list = await (from v in context.AttValues where v.attid.Equals(attid)&&v.value.Equals(value)
                                  select v.owner).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }
        public async Task<List<Guid>> AttValueGetByOwners(List<Guid> owners, Guid attid, string value)
        {
            try
            {
                var list = await (from v in context.AttValues
                                  where owners.Contains(v.owner)&&v.attid.Equals(attid) && v.value.Equals(value)
                                  select v.owner).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }
        public async Task<List<Guid>> AttValueGetByOwners2(List<Guid> owners, Guid attid, string value)
        {
            try
            {
               // string a = "100";
               //int aa= a.CompareTo(value);//-1
               // string b = "999999999999";
               // int bb = b.CompareTo(value);//1
                var list = await (from v in context.AttValues
                                  where owners.Contains(v.owner) && v.attid.Equals(attid) &&v.value.CompareTo(value)<0
                                  select v.owner).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttRepository), ex);
            }
        }

        private AttValue GetAttValue(Guid owner, EAttTables tab, EGroupAtt att_name)
        {
            var attname = AttNameGet(tab.ToString(), att_name.ToString());
            if (attname != null)
            {
                return AttValueGet(owner, attname.attid);
            }
            return null;
        }
        private async Task<AttValue> GetAttValueAsync(Guid owner, EAttTables tab, EGroupAtt att_name)
        {
            var attname = await AttNameGetAsync(tab.ToString(), att_name.ToString());
            if (attname != null)
            {
                return await AttValueGetAsync(owner, attname.attid);
            }
            return null;
        }

        #endregion

        #endregion

        #region biz

        #region Group
        /// <summary>
        /// 新增字段重新赋值
        /// </summary>
        /// <param name="groupList">Group集合</param>
        /// <returns></returns>
        public async Task<List<Group>> PatchGroup(List<Group> groupList)
        {
            if (groupList != null)
            {
                foreach (var group in groupList)
                {
                    //获取团长优惠
                    var attValue = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.leader_price);
                    if (attValue != null)
                        group.leader_price = Convert.ToInt32(attValue.value);
                    else
                        group.leader_price = 0;
                    //获取是否使用机器人
                    var userobot = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.userobot);
                    if (userobot != null)
                        group.userobot = Convert.ToInt32(userobot.value);
                    else
                        group.userobot = 0;
                    //获取限制购买次数
                    var orderlimit = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.order_limit);
                    group.order_limit = orderlimit == null ? 0 : Convert.ToInt32(orderlimit.value);
                    //获取活动门店
                    var actpoint = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.activity_point);
                    group.activity_point = actpoint == null ? "" : actpoint.value;
                    //获取团类型
                    var type = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.group_type);
                    group.group_type = type == null ? 0 : Convert.ToInt32(type.value);
                }
            }
            return groupList;
        }
        /// <summary>
        /// 新增字段重新赋值
        /// </summary>
        /// <param name="group">GroupModel</param>
        /// <returns></returns>
        public async Task<Group> PatchGroup(Group group)
        {
            if (group != null)
            {
                //获取团长优惠
                var attValue = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.leader_price);
                if (attValue != null)
                    group.leader_price = Convert.ToInt32(attValue.value);
                else
                    group.leader_price = 0;
                //获取是否使用机器人
                var userobot = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.userobot);
                if (userobot != null)
                    group.userobot = Convert.ToInt32(userobot.value);
                else
                    group.userobot = 0;
                //获取限制购买次数
                var orderlimit = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.order_limit);
                group.order_limit = orderlimit == null ? 0 : Convert.ToInt32(orderlimit.value);
                //获取活动门店
                var actpoint = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.activity_point);
                group.activity_point = actpoint == null ? "" : actpoint.value;
                //获取团类型
                var type = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.group_type);
                group.group_type = type == null ? 0 : Convert.ToInt32(type.value);
                //获取中奖人数
                var obj = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.lucky_count);
                group.lucky_count = obj == null ? 0 : Convert.ToInt32(obj.value);
                //获取抽奖团结束时间
                var obj2 = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.lucky_endTime);
                group.lucky_endTime = obj2 == null ? "0" : obj2.value;
                //抽奖团状态
                var obj3 = await GetAttValueAsync(group.gid, EAttTables.Group, EGroupAtt.lucky_status);
                group.lucky_status = obj3 == null ? 0 :Convert.ToInt32(obj3.value);
            }
            return group;
        }

        public Group PatchGroup_TB(Group group)
        {
            if (group != null)
            {
                //获取团长优惠
                var attValue = GetAttValue(group.gid, EAttTables.Group, EGroupAtt.leader_price);
                if (attValue != null)
                    group.leader_price = Convert.ToInt32(attValue.value);
                else
                    group.leader_price = 0;
                //获取是否使用机器人
                var userobot = GetAttValue(group.gid, EAttTables.Group, EGroupAtt.userobot);
                if (userobot != null)
                    group.userobot = Convert.ToInt32(userobot.value);
                else
                    group.userobot = 0;
                //获取限制购买次数
                var orderlimit = GetAttValue(group.gid, EAttTables.Group, EGroupAtt.order_limit);
                group.order_limit = orderlimit == null ? 0 : Convert.ToInt32(orderlimit.value);
                //获取活动门店
                var actpoint = GetAttValue(group.gid, EAttTables.Group, EGroupAtt.activity_point);
                group.activity_point = actpoint == null ? "" : actpoint.value;
                //获取团类型
                var type = GetAttValue(group.gid, EAttTables.Group, EGroupAtt.group_type);
                group.group_type = type == null ? 0 : Convert.ToInt32(type.value);
                //获取中奖人数
                var obj = GetAttValue(group.gid, EAttTables.Group, EGroupAtt.lucky_count);
                group.lucky_count = obj == null ? 0 : Convert.ToInt32(obj.value);
                //获取抽奖团结束时间
                var obj2 = GetAttValue(group.gid, EAttTables.Group, EGroupAtt.lucky_endTime);
                group.lucky_endTime = obj2 == null ? "0" : obj2.value;
                //抽奖团状态
                var obj3 = GetAttValue(group.gid, EAttTables.Group, EGroupAtt.lucky_status);
                group.lucky_status = obj3 == null ? 0 : Convert.ToInt32(obj3.value);
            }
            return group;
        }

        public async Task<bool> leader_priceAddOrUpdateAsync(Guid gid, int leader_price)
        {
            //根据表名和枚举获取AttName表中attid,再根据gid(owner),attid,leader_price做增加
            var attname = await AttNameGetAsync("Group", EGroupAtt.leader_price.ToString());
            if (attname != null)
            {
                return await AttValueAddOrUpdateValueAsync(gid, attname.attid, leader_price.ToString()) && await
                    AttHelper.UpdateRedisValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.leader_price.ToString(),
                        leader_price.ToString());
            }
            return false;
        }
        public async Task<bool> userobotAddOrUpdateAsync(Guid gid, int userobot)
        {
            var attname = await AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.userobot.ToString());
            if (attname != null)
            {
                return await AttValueAddOrUpdateValueAsync(gid, attname.attid, userobot.ToString()) && await
                    AttHelper.UpdateRedisValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.userobot.ToString(),
                        userobot.ToString());
            }
            return false;
        }
        public async Task<bool> order_limitAddOrUpdateAsync(Guid gid, int order_limit)
        {
            var attname = await AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.order_limit.ToString());
            if (attname != null)
            {
                return await AttValueAddOrUpdateValueAsync(gid, attname.attid, order_limit.ToString()) && await
                    AttHelper.UpdateRedisValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.order_limit.ToString(),
                        order_limit.ToString());
            }
            return false;
        }
        public async Task<bool> activity_pointAddOrUpdateAsync(Guid gid, string activity_point)
        {
            var attname = await AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.activity_point.ToString());
            if (attname != null)
            {
                return await AttValueAddOrUpdateValueAsync(gid, attname.attid, activity_point) && await
                    AttHelper.UpdateRedisValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.activity_point.ToString(), activity_point);
            }
            return false;
        }
        public async Task<bool> group_typeAddOrUpdateAsync(Guid gid, int group_type)
        {
            var attname = await AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.group_type.ToString());
            if (attname != null)
            {
                return await AttValueAddOrUpdateValueAsync(gid, attname.attid, group_type.ToString()) && await
                    AttHelper.UpdateRedisValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.group_type.ToString(), group_type.ToString());
            }
            return false;
        }
        public async Task<bool> lucky_countAddOrUpdateAsync(Guid gid, int lucky_count)
        {
            var attname = await AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.lucky_count.ToString());
            if (attname != null)
            {
                return await AttValueAddOrUpdateValueAsync(gid, attname.attid, lucky_count.ToString()) && await
                    AttHelper.UpdateRedisValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.lucky_count.ToString(), lucky_count.ToString());
            }
            return false;
        }
        public async Task<bool> lucky_endTimeAddOrUpdateAsync(Guid gid, string lucky_endTime)
        {
            var attname = await AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.lucky_endTime.ToString());
            if (attname != null)
            {
                return await AttValueAddOrUpdateValueAsync(gid, attname.attid, lucky_endTime) && await
                    AttHelper.UpdateRedisValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.lucky_endTime.ToString(), lucky_endTime);
            }
            return false;
        }
        public async Task<bool> lucky_statusAddOrUpdateAsync(Guid gid, int lucky_status)
        {
            var attname = await AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.lucky_status.ToString());
            if (attname != null)
            {
                return await AttValueAddOrUpdateValueAsync(gid, attname.attid,lucky_status.ToString()) && await
                    AttHelper.UpdateRedisValueAsync(gid, EAttTables.Group.ToString(), EGroupAtt.lucky_status.ToString(), lucky_status.ToString());
            }
            return false;
        }
        #endregion

        #region Order
        /// <summary>
        /// 更改抽奖团中奖状态
        /// </summary>
        /// <param name="oid">oid</param>
        /// <param name="luckyStatus">order_luckyStatus</param>
        /// <returns></returns>
        public bool order_luckyStatusAddOrUpdateAsync(Guid oid,  int luckyStatus)
        {
            var attname = AttNameGet(EAttTables.Order.ToString(), EOrderAtt.luckyStatus.ToString());
            if (attname != null)
            {
                return AttValueAddOrUpdateValue(oid, attname.attid, luckyStatus.ToString()) && 
                    AttHelper.UpdateRedisValue(oid, EAttTables.Order.ToString(), EOrderAtt.luckyStatus.ToString(), luckyStatus.ToString());
            }
            return false;
        }
        #endregion

        #endregion
    }

    public static class AttHelper
    {
        private static ConcurrentDictionary<string, Guid> _attnameDic = null;
        static object _lockObject = new object();

        static AttHelper()
        {
            if (_attnameDic == null)
            {
                lock (_lockObject)
                {
                    if (_attnameDic == null)
                    {
                        initDic();
                    }
                }
            }
        }

        private static string makeKey(string table_Name, string att_name)
        {
            return table_Name + att_name;
        }

        private static void initDic()
        {
            try
            {
                _attnameDic = new ConcurrentDictionary<string, Guid>();
                using (AttRepository repo = new AttRepository())
                {
                    foreach (var v in repo.AttNameGetall())
                    {
                        string key = makeKey(v.table_name, v.att_name);
                        _attnameDic[key] = v.attid;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttHelper), ex);
            }
        }

        public static Guid GetAttid(string table_Name, string att_name)
        {
            Guid attid;
            string key = makeKey(table_Name, att_name);
            if (_attnameDic.TryGetValue(key, out attid))
            {
                return attid;
            }
            return Guid.Empty;
        }

        public static void RefreshAll()
        {
            try
            {
                lock (_lockObject)
                {
                    _attnameDic.Clear();
                    initDic();
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(AttHelper), ex);
            }
        }

        private static async Task<string> GetValueAsync(Guid owner, Guid attid)
        {
            try
            {
                string key = AttTableRedis.MakeKey(owner, attid);
                var obj = await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<AttTableRedis>(key);

                //如果redis没有，则从数据库中取，并更新到redis中。
                if (string.IsNullOrEmpty(obj.Value))
                {
                    using (AttRepository repo = new AttRepository())
                    {
                        var dbobj = await repo.AttValueGetAsync(owner, attid);
                        if (dbobj != null)
                        {
                            AttTableRedis redisObj = new AttTableRedis()
                            {
                                Key = AttTableRedis.MakeKey(owner, attid),
                                Value = dbobj.value
                            };
                            await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(redisObj);
                            return dbobj.value;
                        }
                        return null;
                    }
                }
                return obj.Value;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(AttHelper), ex);
            }
        }

        private static string GetValue(Guid owner, Guid attid)
        {
            try
            {
                string key = AttTableRedis.MakeKey(owner, attid);
                var obj = new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash_TongBu<AttTableRedis>(key);

                //如果redis没有，则从数据库中取，并更新到redis中。
                if (string.IsNullOrEmpty(obj.Value))
                {
                    using (AttRepository repo = new AttRepository())
                    {
                        var dbobj = repo.AttValueGet(owner, attid);
                        if (dbobj != null)
                        {
                            AttTableRedis redisObj = new AttTableRedis()
                            {
                                Key = AttTableRedis.MakeKey(owner, attid),
                                Value = dbobj.value
                            };
                            new RedisManager2<WeChatRedisConfig>().SaveObject(redisObj);
                            return dbobj.value;
                        }
                        return null;
                    }
                }
                return obj.Value;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(AttHelper), ex);
            }
        }

        public static async Task<string> GetValueAsync(Guid owner, string table_name, string att_name)
        {
            try
            {
                Guid attid = GetAttid(table_name, att_name);
                if (attid.Equals(Guid.Empty))
                    return null;

                return await GetValueAsync(owner, attid);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(AttHelper), ex);
            }
        }

        public static async Task<string> GetValueAsync(Guid owner, EAttTables table_name, EGroupAtt att_name)
        {
            try
            {
                Guid attid = GetAttid(table_name.ToString(), att_name.ToString());
                if (attid.Equals(Guid.Empty))
                    return null;

                return await GetValueAsync(owner, attid);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(AttHelper), ex);
            }
        }
        public static async Task<string> GetValueAsync(Guid owner, EAttTables table_name, EOrderAtt att_name)
        {
            try
            {
                Guid attid = GetAttid(table_name.ToString(), att_name.ToString());
                if (attid.Equals(Guid.Empty))
                    return null;

                return await GetValueAsync(owner, attid);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(AttHelper), ex);
            }
        }

        public static string GetValue(Guid owner, string table_name, string att_name)
        {
            try
            {
                Guid attid = GetAttid(table_name, att_name);
                if (attid.Equals(Guid.Empty))
                    return null;

                return GetValue(owner, attid);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(AttHelper), ex);
            }
        }

        public static async Task<bool> UpdateRedisValueAsync(Guid owner, string table_name, string att_name, string new_value)
        {
            try
            {
                Guid attid = GetAttid(table_name, att_name);
                if (attid.Equals(Guid.Empty))
                    return false;

                string key = AttTableRedis.MakeKey(owner, attid);
                AttTableRedis value = new AttTableRedis();
                value.Key = key;
                value.Value = new_value;
                return await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(value);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(AttHelper), ex);
            }
        }

        public static bool UpdateRedisValue(Guid owner, string table_name, string att_name, string new_value)
        {
            try
            {
                Guid attid = GetAttid(table_name, att_name);
                if (attid.Equals(Guid.Empty))
                    return false;

                string key = AttTableRedis.MakeKey(owner, attid);
                AttTableRedis value = new AttTableRedis();
                value.Key = key;
                value.Value = new_value;
                return new RedisManager2<WeChatRedisConfig>().SaveObject(value);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(AttHelper), ex);
            }
        }
    }
}
