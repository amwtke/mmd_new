using MD.Lib.DB.Context;
using MD.Lib.DB.Mysql;
using MD.Lib.Util.MDException;
using MD.Model.DB.Code;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.DB.Repositorys
{
    public class StaRepository : IDisposable
    {
        private readonly MDStaContext context;

        public MDStaContext Context => context;

        #region Constructor
        public StaRepository()
        {
            context = new MDStaContext();
        }
        public StaRepository(MDStaContext context)
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

        // ~ProRepository() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        void IDisposable.Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Operation
        #region v_merorder
        public async Task<object> getActivityData(string q, double f, double t, int index, int size)
        {
            try
            {
                //int qCount =await (from j in context.Vmerorders
                //              where j.merchant_name.Contains(q) && j.last_update_time >= f && j.last_update_time <= t
                //              group j by j.gid).CountAsync();
                if (index <= 0 || size <= 0)
                    return null;
                int from = (index - 1) * size;

                var ret = await (from j in context.Vmerorders
                                 where j.merchant_name.Contains(q) && j.last_update_time >= f && j.last_update_time <= t
                                 group j by new { j.gid,j.product_name,j.merchant_name } into g
                                 select new
                                 {
                                     g.Key.gid,
                                  aaa=  g.GroupBy(p => p.goid).Count(),//开团数
                                   bbb= g.GroupBy(p => p.gostatus).Count(),//成团数
                                                                          // cjdd=g.Count()//成交订单
                                 }).ToListAsync();
                return new { total = ret.Count, glist = ret };
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(StaRepository), ex);
            }
        }

        public DataTable GetMerchantData(string queryStr, double timeStart, double timeEnd, string orderBy, bool isAsc, int pageIndex, int pageSize)
        {
            #region SQL
            //           select* from (
            //           select m.mid,                                     /*商家mid*/
            //           m.`name`,                                         /*商家名称*/
            //           ifnull(pointCount, 0) as pointCount,               /*门店数*/
            //           FROM_UNIXTIME(m.register_date, '%Y-%m-%d %H:%i') as register_date,/*开通时间*/
            //           ifnull(productCount, 0) as productCount,           /*添加商品数*/
            //           ifnull(groupCountAll, 0) as groupCountAll,         /*发布活动数*/
            //           ifnull(groupCountK, 0) as groupCountK,             /*开团数*/
            //           ifnull(groupCountS, 0) as groupCountS,             /*成团数*/
            //           ifnull(orderCount, 0) as orderCount,               /*成交订单*/
            //           ifnull(orderAmount, 0) as orderAmount,             /*成交金额*/
            //           ifnull(orderCountH, 0) as orderCountH              /*已核销数量*/
            //           from mmd.merchant m
            //           left
            //           join (select mid, count(1) pointCount from mmd.writeoffpoint group by mid) po on m.mid = po.mid
            //left join (select mid, count(1) productCount from mmd.product where TimeStamp > 1463052262 and TimeStamp < 1472572800 group by mid) p on m.mid = p.mid
            //left join (select mid, count(1) groupCountAll from mmd.`group` where last_update_time > 1463052262 and last_update_time < 1472572800 group by mid) g on m.mid = g.mid
            //left join (select m.mid, count(1) groupCountK from mmd.merchant m join mmd.`group` g on m.mid = g.mid
            //join mmd.grouporder go on go.gid = g.gid where(go.status = 0 or go.status = 1 or go.status = 2) and go.create_date > 1463052262 and go.create_date < 1472572800 group by mid ) gck on m.mid = gck.mid
            //left join (select m.mid, count(1) groupCountS from mmd.merchant m join mmd.`group` g on m.mid = g.mid
            //join mmd.grouporder go on go.gid = g.gid where go.status = 1 and go.create_date > 1463052262 and go.create_date < 1472572800 group by mid ) gcs on m.mid = gcs.mid
            //left join (select mid, count(1) orderCount, sum(order_price) orderAmount from mmd.`order` where(status = 6 or status = 5) and paytime > 1463052262 and paytime < 1472572800 group by mid) o on m.mid = o.mid
            //left join (select mid, count(1) orderCountH from mmd.`order` where status = 6 and paytime > 1463052262 and paytime < 1472572800 group by mid) oh on m.mid = oh.mid
            //where m.status = 4 and m.`name` like '%悦%' order by groupCountK desc) t limit 0,20
            #endregion
            MysqlHelper db = MysqlHelper.Ins;
            db.ConnStr = "Data Source=rm-bp1p078adlt9f48y9.mysql.rds.aliyuncs.com;port=3306;Initial Catalog=mmd;user id=mmd;password=rds@cfd3341d00526512;Character Set=utf8mb4;";
            string sql = string.Format(@"select m.mid,                                     /*商家mid*/
 m.`name`,                                         /*商家名称*/
 ifnull(pointCount, 0) as pointCount,               /*门店数*/
 FROM_UNIXTIME(m.register_date, '%Y-%m-%d %H:%i') as regDate,/*开通时间*/
 ifnull(productCount, 0) as productCount,           /*添加商品数*/
 ifnull(groupCountAll, 0) as groupCountAll,         /*发布活动数*/
 ifnull(groupCountK, 0) as groupCountK,             /*开团数*/
 ifnull(groupCountS, 0) as groupCountS,             /*成团数*/
 ifnull(orderCount, 0) as orderCount,               /*成交订单*/
 ifnull(orderAmount, 0) as orderAmount,             /*成交金额*/
 ifnull(orderCountH, 0) as orderCountH              /*已核销数量*/
 from mmd.merchant m
 left
 join (select mid,count(1) pointCount from mmd.writeoffpoint group by mid) po on m.mid = po.mid
 left join (select mid, count(1) productCount from mmd.product where TimeStamp > {0} and TimeStamp < {1} group by mid) p on m.mid = p.mid
 left join (select mid, count(1) groupCountAll from mmd.`group` where last_update_time > {0} and last_update_time < {1} group by mid) g on m.mid = g.mid
 left join (select m.mid, count(1) groupCountK from mmd.merchant m join mmd.`group` g on m.mid = g.mid
 join mmd.grouporder go on go.gid = g.gid where(go.status = 0 or go.status = 1 or go.status = 2) and go.create_date > {0} and go.create_date < {1} group by mid ) gck on m.mid = gck.mid
 left join (select m.mid, count(1) groupCountS from mmd.merchant m join mmd.`group` g on m.mid = g.mid
 join mmd.grouporder go on go.gid = g.gid where go.status = 1 and go.create_date > {0} and go.create_date < {1} group by mid ) gcs on m.mid = gcs.mid
 left join (select mid, count(1) orderCount, sum(order_price) orderAmount from mmd.`order` where(status = 6 or status = 5) and paytime > {0} and paytime < {1} group by mid) o on m.mid = o.mid
 left join (select mid, count(1) orderCountH from mmd.`order` where status = 6 and writeoffday > {0} and writeoffday < {1} group by mid) oh on m.mid = oh.mid
 where m.status = 4 ", timeStart, timeEnd);
            if (!string.IsNullOrEmpty(queryStr))
            {
                sql += " and m.`name` like '%" + queryStr + "%' ";
            }
            if (isAsc) sql += " order by " + orderBy + " asc ";
            else sql += " order by " + orderBy + " desc "; ;
            
            sql = string.Format("select * from (" + sql + ") t limit {0},{1}", (pageIndex - 1)*pageSize, pageSize);
            DataTable dt = db.ExcuteDataTable(sql, new MySql.Data.MySqlClient.MySqlParameter[] { });
            return dt;
        }

        public int GetMerchantCount(string queryStr)
        {
            MysqlHelper db = MysqlHelper.Ins;
            db.ConnStr = "Data Source=rm-bp1p078adlt9f48y9.mysql.rds.aliyuncs.com;port=3306;Initial Catalog=mmd;user id=mmd;password=rds@cfd3341d00526512;Character Set=utf8mb4;";
            string sql = "select count(1) as count from mmd.merchant m where  m.status = 4";
            if (!string.IsNullOrEmpty(queryStr))
                sql += " and m.`name` like '%" + queryStr + "%';";
            DataTable dt = db.ExcuteDataTable(sql, new MySql.Data.MySqlClient.MySqlParameter[] { });
            if (dt != null && dt.Rows.Count > 0)
            {
                int i = Convert.ToInt32(dt.Rows[0]["count"].ToString());
                return i;
            }
            return 0;
        }
        #endregion
        #endregion
    }
}
