using MD.Lib.DB.Context;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.DynamicData;
using System.Web.Mvc.Html;
using MD.Lib.DB.Redis.MD;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.MQ.MD;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Message;
using MD.Lib.Weixin.Robot;
using MD.Lib.Weixin.Vector.Vectors;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using MD.Model.Index.MD;

namespace MD.Lib.DB.Repositorys
{
    public class BizRepository : IDisposable
    {
        private readonly MDProContext context;

        public MDProContext Context => context;
        static object _syncObject = new object();
        #region Constructor
        public BizRepository()
        {
            context = new MDProContext();
        }
        public BizRepository(MDProContext context)
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

        #region Community
        public async Task<Community> GetCommunityByCidAsync(Guid cid)
        {
            try
            {
                var ret = await (from c in context.Communitys where c.cid.Equals(cid) select c).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public async Task<Community> SaveOrUpdateCommunityAsync(Community comm)
        {
            try
            {
                Community community = await GetCommunityByCidAsync(comm.cid);
                //新加入
                if (community == null || community.cid.Equals(Guid.Empty))
                {
                    if (comm.cid.Equals(Guid.Empty))
                        comm.cid = Guid.NewGuid();
                    comm.createtime = (int)CommonHelper.GetUnixTimeNow();
                    comm.lastupdatetime = (int)CommonHelper.GetUnixTimeNow();
                    context.Communitys.Add(comm);
                    if (await context.SaveChangesAsync() == 1)
                        return comm;
                }
                return community;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return null;
        }
        public async Task<Community> UpdateCommunityPraisesAsync(Guid cid)
        {
            var community = await (from c in context.Communitys where c.cid.Equals(cid) select c).FirstOrDefaultAsync();
            if (community != null)
            {
                community.praises += 1;
                if (await context.SaveChangesAsync() == 1)
                    return community;
            }
            return null;
        }
        public async Task<Community> DelCommunityAsync(Guid cid, int status)
        {
            var community = await (from c in context.Communitys where c.cid.Equals(cid) select c).FirstOrDefaultAsync();
            if (community != null)
            {
                community.status = status;
                if (await context.SaveChangesAsync() == 1)
                    return community;
            }
            return null;
        }
        #endregion

        #region merchant

        /// <summary>
        /// 获取一个商家某个业务类型的余额。
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="biz_type"></param>
        /// <returns></returns>
        public async Task<int> MerchantGetBizQuota(Guid mid, string biz_type)
        {
            try
            {
                MBiz mb = await GetMbizAsync(mid, biz_type);
                if (mb == null)
                    return 0;
                if (mb.quota_remain != null) return mb.quota_remain.Value;
                return 0;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> IsMerchantExitsAsnyc(string appid)
        {
            var count = await (from m in context.Merchants where m.wx_appid == appid select m).CountAsync();
            return count == 1;
        }
        public bool IsMerchantExits(string appid)
        {
            var count = (from m in context.Merchants where m.wx_appid == appid select m).Count();
            return count == 1;
        }

        public async Task<Merchant> GetMerchantByMidAsync(Guid mid)
        {
            try
            {
                var ret = await (from m in context.Merchants where m.mid.Equals(mid) select m).FirstOrDefaultAsync();
                return RepositoryHelper.UpdateContextItem(context, ret);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public Merchant GetMerchantByMid(Guid mid)
        {
            try
            {
                var ret = (from m in context.Merchants where m.mid.Equals(mid) select m).FirstOrDefault();
                return RepositoryHelper.UpdateContextItem(context, ret);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<Merchant> GetMerchantByAppidAsync(string appid)
        {
            var mer = await (from m in context.Merchants where m.wx_appid == appid select m).FirstOrDefaultAsync();
            return mer ?? new Merchant();
        }

        public Merchant GetMerchantByAppid(string appid)
        {
            var mer = (from m in context.Merchants where m.wx_appid == appid select m).FirstOrDefault();
            return mer ?? new Merchant();
        }

        public Merchant GetMerchantByMpId(string mpid)
        {
            var mer = (from m in context.Merchants where m.wx_mp_id == mpid select m).FirstOrDefault();
            return mer;
        }

        public async Task<bool> SaveOrUpdateRegisterMerchantAsync(Merchant m)
        {
            if (string.IsNullOrEmpty(m?.wx_appid))
                return false;
            bool isNew = false;
            Merchant mer = await GetMerchantByAppidAsync(m.wx_appid);

            try
            {
                foreach (var p in typeof(Merchant).GetProperties())
                {
                    //判断是否为新加 mid为空
                    if (p.Name.Equals("mid"))
                    {
                        if (p.GetValue(mer).Equals(Guid.Empty))
                        {
                            p.SetValue(mer, Guid.NewGuid());
                            mer.register_date = CommonHelper.GetUnixTimeNow();
                            mer.status = (int)ECodeMerchantStatus.待审核;
                            mer.order_quota = 0;
                            isNew = true;
                        }
                        continue;
                    }

                    //更新属性
                    var v = m.GetType().GetProperty(p.Name).GetValue(m);
                    if (v != null)
                    {
                        //long型字段的判断
                        long? v_value = v as long?;
                        if (v_value == long.MinValue)
                            continue;

                        //其他字段更新
                        p.SetValue(mer, v);
                    }
                }
                if (isNew)
                {
                    //为了躲过验证做的mock
                    mer.wx_mch_id = "x";
                    mer.wx_apikey = "x";
                    mer.wx_p12_dir = "x";
                    mer.contact_person = "x";
                    mer.address = "x";
                    mer.biz_licence_url = "x";
                    mer.cell_phone = 13800000000;
                    mer.service_region = "x";
                    context.Merchants.Add(mer);
                }
                else//如果是通过审核，则wx_mch_id也是空，则需要mock
                {
                    if (!string.IsNullOrEmpty(mer.wx_mch_id))
                    {
                        await context.SaveChangesAsync();
                        //修改redis
                        await RedisMerchantOp.UpdateFromDbAsync(mer);
                        return true;
                    }

                    mer.wx_mch_id = "x";
                    mer.wx_apikey = "x";
                    mer.wx_p12_dir = "x";
                }

                await context.SaveChangesAsync();
                //修改redis
                await RedisMerchantOp.UpdateFromDbAsync(mer);
                return true;
            }
            catch (Exception ex)
            {

                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return false;
        }
        /// <summary>
        /// 例如：当用户配置完成时，他在后台的状态由通过审核未配置变为已配置
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="status"></param>
        /// <returns></returns>

        public async Task<bool> ChangeMerchantStatus(string appid, ECodeMerchantStatus status)
        {
            if (string.IsNullOrEmpty(appid))
                return false;
            Merchant mer = await GetMerchantByAppidAsync(appid);
            try
            {
                mer.status = (int)status;

                //修改redis
                await RedisMerchantOp.UpdateFromDbAsync(mer);

                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return false;
        }

        public async Task<Tuple<int, List<Merchant>>> MerchantSearchByNameAsync(string q, int index, int size)
        {
            try
            {
                var qCount = await context.Merchants.Where(m => m.name.Contains(q)).CountAsync();
                //搜索
                if (index <= 0 || size <= 0)
                    return null;

                int from = (index - 1) * size;

                if (!string.IsNullOrEmpty(q)) //全部
                {
                    var list =
                        await
                            (from mer in context.Merchants
                             where mer.name.Contains(q)
                             orderby mer.register_date descending
                             select mer).Skip(from)
                                .Take(size)
                                .ToListAsync();
                    foreach (var mer in list)
                    {
                        var mbiz = await GetMbizAsync(mer.mid, "DD");
                        mer.order_quota = mbiz == null ? 0 : mbiz.quota_remain;
                    }
                    return Tuple.Create(qCount, list);
                }

                //总数
                var count = await context.Merchants.CountAsync();

                var ret =
                    await
                        (from mer in context.Merchants orderby mer.register_date descending select mer).Skip(from)
                            .Take(size)
                            .ToListAsync();
                foreach (var mer in ret)
                {
                    var mbiz = await GetMbizAsync(mer.mid, "DD");
                    mer.order_quota = mbiz == null ? 0 : mbiz.quota_remain;
                }
                return Tuple.Create(count, ret);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public Tuple<int, List<Merchant>> MerchantSearchByName(string q, int index, int size)
        {
            try
            {
                //搜索
                if (index <= 0 || size <= 0)
                    return null;

                int from = (index - 1) * size;

                if (!string.IsNullOrEmpty(q)) //全部
                {
                    var qCount = context.Merchants.Count(m => m.name.Contains(q));
                    var list =

                            (from mer in context.Merchants
                             where mer.name.Contains(q)
                             orderby mer.register_date descending
                             select mer).Skip(from)
                                .Take(size)
                                .ToList();
                    //foreach (var mer in list)
                    //{
                    //    var mbiz = GetMbiz(mer.mid, "DD");
                    //    mer.order_quota = mbiz == null ? 0 : mbiz.quota_remain;
                    //}
                    return Tuple.Create(qCount, list);
                }

                //总数
                var count = context.Merchants.Count();

                var ret =

                        (from mer in context.Merchants orderby mer.register_date descending select mer).Skip(from)
                            .Take(size)
                            .ToList();
                //foreach (var mer in ret)
                //{
                //    var mbiz = GetMbiz(mer.mid, "DD");
                //    mer.order_quota = mbiz == null ? 0 : mbiz.quota_remain;
                //}
                return Tuple.Create(count, ret);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<Tuple<int, List<Merchant>>> MerchantSearchByNameAsync(string q, int index, int size, int status, string strOrderBy)
        {
            try
            {

                //搜索
                if (index <= 0 || size <= 0)
                    return null;
                int from = (index - 1) * size;
                if (!string.IsNullOrEmpty(q)) //全部
                {
                    var list =
                        await
                            (from mer in context.Merchants
                             where mer.name.Contains(q) && mer.status == status
                             orderby mer.register_date descending
                             select mer).Skip(from)
                                .Take(size)
                                .ToListAsync();
                    var qCount = await context.Merchants.Where(m => m.name.Contains(q) && m.status == status).CountAsync();
                    return Tuple.Create(qCount, list);
                }

                //总数
                var count = await context.Merchants.Where(m => m.status == status).CountAsync();
                var ret =
                    await
                        (from mer in context.Merchants
                         where mer.status == status
                         orderby mer.register_date descending
                         select mer).Skip(from)
                            .Take(size)
                            .ToListAsync();
                return Tuple.Create(count, ret);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Guid>> MerchantSearchByNameAsync(string merName)
        {
            try
            {
                List<Guid> MidList = new List<Guid>();
                var list = await (from mer in context.Merchants
                                  where mer.name.Contains(merName)
                                  orderby mer.register_date descending
                                  select mer).ToListAsync();
                if (list != null || list.Count > 0)
                {
                    foreach (var mer in list)
                    {
                        MidList.Add(mer.mid);
                    }
                }
                return MidList;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        #endregion

        #region product

        /// <summary>
        /// pageCount表示取第N页，size表示一次取多少条。
        /// 第N也为 (N-1)*size+1到N*size条。如：第5页为 记录编号为 41到50的记录。
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="pageCount"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public async Task<Tuple<int, List<Product>>> GetProductsByAppidAsync(string appid, int pageCount, int size)
        {
            Merchant mer = await GetMerchantByAppidAsync(appid);
            if (mer != null && !mer.mid.Equals(Guid.Empty))
            {
                Guid mid = mer.mid;
                var count = await GetProductCountAsync(appid);

                if (count - (pageCount - 1) * size <= 0)//无数据可取。
                    return null;

                if (count - pageCount * size >= 0)//可以返回全部的条数，如果总记录53条，pageCount=5 size=10，则可以返回第41 - 50条
                {
                    var list =
                        await
                            (from product in context.Products
                             where product.mid == mid && product.status == (int)EProductStatus.已添加
                             orderby product.timestamp descending
                             select product).Skip((pageCount - 1) * size).Take(size).ToListAsync();
                    return Tuple.Create(count, list);
                }
                //零数
                if ((count - (pageCount - 1) * size > 0) && (count - pageCount * size) < 0)
                {
                    var list =
                        await
                            (from product in context.Products
                             where product.mid == mid && product.status == (int)EProductStatus.已添加
                             orderby product.timestamp descending
                             select product).Skip((pageCount - 1) * size).ToListAsync();
                    return Tuple.Create(count, list);
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有商家商品
        /// </summary>
        /// <param name="pageCount"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public async Task<Tuple<int, List<Product>>> GetProductsAllAsync(string query, int pageCount, int size)
        {
            int from = (pageCount - 1) * size;
            int status = (int)EProductStatus.已添加;
            if (string.IsNullOrEmpty(query))
            {
                int count = await context.Products.CountAsync(p => p.status == status);
                var list =
                    await
                        (from product in context.Products
                         where product.status == (int)EProductStatus.已添加
                         orderby product.timestamp descending
                         select product).Skip(from).Take(size).ToListAsync();
                return Tuple.Create(count, list);
            }
            else
            {
                int count = await context.Products.CountAsync(p => p.status == status && p.name.Contains(query));
                List<Product> list = await context.Products.Where(p => p.name.Contains(query)).OrderByDescending(s => s.timestamp).Skip(from).Take(size).ToListAsync();
                return Tuple.Create(count, list);
            }
        }


        public async Task<int> GetProductCountAsync(string appid)
        {
            Merchant mer = await GetMerchantByAppidAsync(appid);
            if (mer != null && !mer.mid.Equals(Guid.Empty))
            {
                Guid mid = mer.mid;
                var count =
                    await
                        (from p in context.Products
                         where p.mid == mer.mid && p.status == (int)EProductStatus.已添加
                         select p)
                            .CountAsync();
                return count;
            }
            return 0;
        }


        public async Task<Product> GetProductByPidAsync(Guid pid)
        {
            var product = await context.Products.Where(p => p.pid == pid).FirstOrDefaultAsync();
            return product ?? new Product();
        }

        public Product GetProductByPid(Guid pid)
        {
            var product = context.Products.FirstOrDefault(p => p.pid == pid);
            return product ?? new Product();
        }

        public Product GetProductByPno(string pno)
        {
            long p_no = Convert.ToInt64(pno);
            var product = context.Products.FirstOrDefault(p => p.p_no == p_no);
            return product;
        }

        public async Task<bool> SaveOrUpdateProductAsync(Product pro)
        {
            Product product = await GetProductByPidAsync(pro.pid);
            bool isNew = false;
            try
            {
                //新加入
                if (product.pid.Equals(Guid.Empty))
                {
                    //pro也是新的
                    if (pro.pid.Equals(Guid.Empty))
                        product.pid = Guid.NewGuid();
                    product.pid = pro.pid;
                    product.timestamp = CommonHelper.GetUnixTimeNow();
                    product.status = (int)EProductStatus.已添加;
                    product.avgScore = 0;
                    product.scorePeopleCount = 0;
                    product.grassCount = 0;
                    isNew = true;
                }

                foreach (var p in product.GetType().GetProperties())
                {
                    //更新属性
                    var v = pro.GetType().GetProperty(p.Name).GetValue(pro);
                    if (v != null)
                    {
                        //其他字段更新
                        p.SetValue(product, v);
                    }
                }
                if (isNew)
                    context.Products.Add(product);

                int ret = await context.SaveChangesAsync();
                return ret == 1;
            }
            catch (Exception ex)
            {
                var proStr = $"pid:{product.pid},p_no:{product.p_no},name:{product.name},description:{product.description},price:{product.price},mid:{product.mid}";
                var proStr2 = $"category:{product.category},aaid:{product.aaid},standard,last_update_user,timestamp,advertise_pic_1,advertise_pic_2,advertise_pic_3,status,status,avgScore,scorePeopleCount,grassCount";
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"fun:SaveOrUpdateProductAsync,pid:{pro.pid},ex:{ex.Message},isNew:{isNew}"));
            }
            return false;
        }
        /// <summary>
        /// 删除一个商品，将商品的状态改成已删除!
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public async Task<bool> DeleteProductAsync(Guid pid)
        {
            var p = GetProductByPidAsync(pid);
            var product = await p;
            if (!product.pid.Equals(Guid.Empty))
            {
                product.status = (int)EProductStatus.已删除;
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Product>> GetProductsFromIndex(List<Guid> gList)
        {
            var list = await (from p in context.Products where gList.Contains(p.pid) orderby p.timestamp descending select p).ToListAsync();
            return list;
        }

        /// <summary>
        /// 更新平均分
        /// </summary>
        /// <param name="pid">pid</param>
        /// <param name="avgScore">平均分</param>
        /// <param name="scorePeopleCount">评论人数</param>
        /// <returns></returns>
        public async Task<Product> UpdateProductScoreAsync(Guid pid, double avgScore, int scorePeopleCount)
        {
            try
            {
                Product product = await GetProductByPidAsync(pid);
                if (product != null && !product.pid.Equals(Guid.Empty))
                {
                    product.avgScore = Math.Round(avgScore, 2);
                    product.scorePeopleCount = scorePeopleCount;
                    await context.SaveChangesAsync();
                    return product;
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception("fun:UpdateProductScoreAsync,ex:" + ex.Message));
            }
            return null;
        }
        /// <summary>
        /// 更新种草数加1
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public async Task<Product> UpdateProductGrassAsync(Guid pid)
        {
            try
            {
                Product product = await GetProductByPidAsync(pid);
                if (product != null && !product.pid.Equals(Guid.Empty))
                {
                    product.grassCount = product.grassCount + 1;
                    await context.SaveChangesAsync();
                    return product;
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception("fun:UpdateProductGrassAsync,ex:" + ex.Message));
            }
            return null;
        }

        #endregion

        #region productComment
        public async Task<ProductComment> GetProductCommentByPCidAsync(Guid pcid)
        {
            try
            {
                var obj = await (from s in context.ProductComments where s.pcid.Equals(pcid) select s).FirstOrDefaultAsync();
                return obj ?? new ProductComment();
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return new ProductComment();
        }
        public async Task<ProductComment> SaveOrUpdateProductCommentAsync(ProductComment pcomment)
        {
            ProductComment productcomment = await GetProductCommentByPCidAsync(pcomment.pcid);
            bool isNew = false;
            try
            {
                //新加入
                if (productcomment == null || productcomment.pcid.Equals(Guid.Empty))
                {
                    if (pcomment.pcid.Equals(Guid.Empty))
                        productcomment.pcid = Guid.NewGuid();
                    productcomment.timestamp = CommonHelper.GetUnixTimeNow();
                    productcomment.isessence = 0;
                    productcomment.praise_count = 0;
                    isNew = true;
                }

                foreach (var p in productcomment.GetType().GetProperties())
                {
                    //更新属性
                    var v = productcomment.GetType().GetProperty(p.Name).GetValue(pcomment);
                    if (v != null)
                    {
                        //其他字段更新
                        p.SetValue(productcomment, v);
                    }
                }
                if (isNew)
                    context.ProductComments.Add(productcomment);
                await context.SaveChangesAsync();
                return productcomment;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return null;
        }

        public async Task<Tuple<int, List<ProductComment>>> GetCommentsAsync(Guid pid, string query, int pageIndex, int pageSize)
        {
            int from = (pageIndex - 1) * pageSize;
            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    int count = await context.ProductComments.CountAsync(c => c.pid == pid);
                    var list = await context.ProductComments.Where(c => c.pid == pid).Skip(from).Take(pageSize).ToListAsync();
                    return Tuple.Create(count, list);
                }
                else
                {
                    int count = await context.ProductComments.CountAsync(c => c.pid == pid && c.comment.Contains(query));
                    var list = await context.ProductComments.Where(c => c.pid == pid && c.comment.Contains(query)).Skip(from).Take(pageSize).ToListAsync();
                    return Tuple.Create(count, list);
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
                return Tuple.Create(0, new List<ProductComment>());
            }
        }

        public async Task<ProductComment> UpdateProductComment_PraiseAsync(Guid pcid)
        {
            try
            {
                var pc = await GetProductCommentByPCidAsync(pcid);
                if (pc != null && !pc.pcid.Equals(Guid.Empty))
                {
                    pc.praise_count = pc.praise_count + 1;
                    await context.SaveChangesAsync();
                    return pc;
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception("fun:UpdateProductCommentPraise,ex:" + ex.Message));
            }
            return null;
        }

        public async Task<bool> DelCommentAsync(Guid pcid)
        {
            try
            {
                context.ProductComments.Remove(context.ProductComments.Where(c => c.pcid == pcid).FirstOrDefault());
                int res = await context.SaveChangesAsync();
                await EsProductCommentManager.DelComment(pcid);
                return res > 0;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"DelCommentAsync Error,pcid:{pcid},ex:" + ex.Message));
            }
            return false;
        }

        public async Task<Tuple<int, double>> GetByTotalAndAvgScoreAsync(Guid pid)
        {
            try
            {
                var total = await (from c in context.ProductComments where c.pid.Equals(pid) select c).CountAsync();
                var avgScore = await (from c in context.ProductComments where c.pid.Equals(pid) select c).AverageAsync(p => p.score);
                return Tuple.Create(total, avgScore.Value);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception("fun:GetByTotalAndAvgScoreAsync,ex:" + ex.Message));
            }
            return null;
        }
        #endregion

        #region NoticeBoard
        /// <summary>
        /// 获取总行数
        /// </summary>
        /// <param name="enoticestatus">状态</param>
        /// <returns></returns>
        public async Task<List<NoticeBoard>> GetNoticeBoardAsync(List<Guid> guidList)
        {
            var list = await (from n in context.NoticeBoards where guidList.Contains(n.nid) orderby n.timestamp descending select n).ToListAsync();
            return list;
        }
        /// <summary>
        /// 获取资讯详情
        /// </summary>
        /// <param name="nid">nid</param>
        /// <returns></returns>
        public async Task<NoticeBoard> GetNoticeBoardAsync(Guid nid)
        {
            var notice = await context.NoticeBoards.Where(p => p.nid == nid).FirstOrDefaultAsync();
            return notice ?? new NoticeBoard();
        }
        /// <summary>
        /// 获取分页数据
        /// </summary>
        /// <param name="pageCount">页数</param>
        /// <param name="size">返回的行数</param>
        /// <param name="enoticestatus">状态</param>
        /// <returns></returns>
        public async Task<Tuple<int, List<NoticeBoard>>> GetNoticeBoardAsync(string q, int pageCount, int size, int status)
        {
            try
            {
                if (pageCount <= 0 || size <= 0)
                    return null;


                //搜索
                int from = (pageCount - 1) * size;

                if (!string.IsNullOrEmpty(q)) //搜索
                {
                    var qCount = await context.NoticeBoards.Where(m => m.title.Contains(q) && m.status.Equals(status)).CountAsync();
                    var list =
                        await
                            (from n in context.NoticeBoards
                             where n.title.Contains(q) && n.status.Equals(status)
                             orderby n.timestamp descending
                             select n).Skip(from)
                                .Take(size)
                                .ToListAsync();
                    return Tuple.Create(qCount, list);
                }

                //无搜索时查全部
                var count = await context.NoticeBoards.Where(p => p.status.Equals(status)).CountAsync();
                var ret =
                    await
                        (from n in context.NoticeBoards where n.status.Equals(status) orderby n.timestamp descending select n).Skip(from)
                            .Take(size)
                            .ToListAsync();
                return Tuple.Create(count, ret);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 存在则更新，否则新增
        /// </summary>
        /// <param name="noticeboard">NoticeBoardModel</param>
        /// <returns></returns>
        public async Task<NoticeBoard> SaveOrUpdateNoticeBoardAsync(NoticeBoard noticeboard)
        {
            NoticeBoard notice = await GetNoticeBoardAsync(noticeboard.nid);
            try
            {
                //新加入
                if (notice.nid.Equals(Guid.Empty))
                {
                    if (noticeboard.nid.Equals(Guid.Empty))
                        noticeboard.nid = Guid.NewGuid();
                    noticeboard.status = (int)ENoticeBoardStatus.待发布;
                    noticeboard.timestamp = CommonHelper.GetUnixTimeNow();
                    context.NoticeBoards.Add(noticeboard);
                    await context.SaveChangesAsync();
                    return noticeboard;
                }
                foreach (var n in notice.GetType().GetProperties())
                {
                    //更新属性
                    var v = noticeboard.GetType().GetProperty(n.Name).GetValue(noticeboard);
                    if (v != null)
                    {
                        //其他字段更新
                        n.SetValue(notice, v);
                    }
                }
                notice.timestamp = CommonHelper.GetUnixTimeNow();//获取最新修改时间
                await context.SaveChangesAsync();
                return notice;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return notice;
        }
        /// <summary>
        /// 修改资讯状态
        /// </summary>
        /// <param name="nid">nid</param>
        /// <param name="enoticestatus">状态</param>
        /// <returns></returns>
        public async Task<bool> UpdateNoticeBoardStatusAsync(Guid nid, int status)
        {
            var notice = await GetNoticeBoardAsync(nid);
            if (!notice.nid.Equals(Guid.Empty))
            {
                notice.status = status;
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        /// <summary>
        /// 更新点击量自动加1，并加入NoticeReader表记录
        /// </summary>
        /// <param name="nid">主键</param>
        /// <returns></returns>
        public async Task<bool> UpdateNoticeBoardhitsAsync(Guid nid, NoticeReader reader)
        {
            var notice = await GetNoticeBoardAsync(nid);
            if (!notice.nid.Equals(Guid.Empty))
            {
                notice.hits_count = notice.hits_count + 1;
                await AddNoticeReaderAsync(reader);//加入NoticeReader表记录
                return true;
            }
            return false;
        }
        /// <summary>
        /// 点赞数累加1
        /// </summary>
        /// <param name="nid"></param>
        /// <returns></returns>
        public async Task<NoticeBoard> UpdateNoticeBoardPraise_countAsync(Guid nid)
        {
            var notice = await GetNoticeBoardAsync(nid);
            if (notice != null && !notice.nid.Equals(Guid.Empty))
            {
                notice.praise_count = notice.praise_count + 1;
                await context.SaveChangesAsync();
                return notice;
            }
            return null;
        }
        /// <summary>
        /// 更新转发量（种草）自动加1
        /// </summary>
        /// <param name="nid">主键</param>
        /// <returns></returns>
        public async Task<bool> UpdateNoticeBoardtransmitAsync(Guid nid)
        {
            var notice = await GetNoticeBoardAsync(nid);
            if (!notice.nid.Equals(Guid.Empty))
            {
                notice.transmit_count = notice.transmit_count + 1;
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateNoticeBoardTopAsync(Guid nid, string time)
        {
            var notice = await GetNoticeBoardAsync(nid);
            if (!notice.nid.Equals(Guid.Empty))
            {
                notice.extend_1 = time;
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        #endregion

        #region NoticeReader
        /// <summary>
        /// 根据nid查询前N个看过此文档的人的openid
        /// </summary>
        /// <param name="nid">文档nid</param>
        /// <param name="topCount">前几条</param>
        /// <returns></returns>
        public async Task<List<string>> GetNoticeReaderAsync(Guid nid, int topCount)
        {
            try
            {
                var objList = await (from j in context.NoticeReaders
                                     where j.nid.Equals(nid)
                                     orderby j.timestamp descending
                                     select new { openid = j.openid }).Take(topCount).ToListAsync();
                List<string> strList = new List<string>();
                if (objList.Count > 0)
                {
                    foreach (var o in objList)
                    {
                        strList.Add(o.openid);
                    }
                }
                return strList;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
                return new List<string>();
            }
        }
        private async Task<NoticeReader> GetNoticeReaderAsync(Guid nid, Guid uid)
        {
            try
            {
                return await context.NoticeReaders.Where(p => p.nid.Equals(nid) && p.uid.Equals(uid)).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
                return null;
            }
        }
        /// <summary>
        /// 增加当前阅读人记录
        /// </summary>
        /// <param name="nid">阅读文档nid</param>
        /// <param name="uid">用户id</param>
        /// <param name="openid">用户openid</param>
        /// <returns></returns>
        private async Task<bool> AddNoticeReaderAsync(NoticeReader re)
        {
            NoticeReader reader = await GetNoticeReaderAsync(re.nid, re.uid);
            if (reader == null)//为空时就做增加
            {
                context.NoticeReaders.Add(re);
                await context.SaveChangesAsync();
                return true;
            }
            reader.timestamp = CommonHelper.GetUnixTimeNow();
            await context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region merchant order

        /// <summary>
        /// 记录商家业务消耗日志。
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="bizType"></param>
        /// <param name="count"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task<bool> MerchantOrderConsumeAsnyc(Guid mid, string bizType, int count, Guid source)
        {
            try
            {
                if (mid.Equals(Guid.Empty) || string.IsNullOrEmpty(bizType) || count < 0 || source.Equals(Guid.Empty))
                    return false;

                var mbiz = await GetMbizAsync(mid, bizType);
                if (mbiz != null)
                {
                    mbiz.quota_remain = mbiz.quota_remain == 0 ? 0 : mbiz.quota_remain - count;
                    if (mbiz.quota_remain < 0)
                        mbiz.quota_remain = 0;

                    //await context.SaveChangesAsync();

                    //记录消耗日志
                    MBizConsumeLog log = new MBizConsumeLog()
                    {
                        biz_type = bizType,
                        count = count,
                        mid = mid,
                        source = source,
                        timestamp = CommonHelper.GetUnixTimeNow()
                    };
                    context.MBizConsumes.Add(log);
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
                return false;
            }
        }

        /// <summary>
        /// 是否可以生成商家订单的判断函数
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="mid"></param>
        /// <returns></returns>
        private async Task<bool> CanCreateMorder(CodeBizTaocan tc, Guid mid)
        {
            try
            {
                if (tc.limit == (int)ECodeTaocanLimit.无限制)
                    return true;

                int boughtShares = await GetModerBoughtSharesAsync(mid, tc.tc_type);
                if (boughtShares < tc.limit.Value)
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 生成订单，单未支付
        /// </summary>
        /// <param name="mer"></param>
        /// <param name="tcType">套餐编码</param>
        /// <param name="cash">应付金额</param>
        /// <param name="buyTcShares">购买套餐的份数</param>
        /// <returns></returns>
        public async Task<Tuple<EMorderResponse, MOrder>> MorderCreateAsync(Merchant mer, string tcType, int buyTcShares, int cash)
        {
            try
            {
                //购买的份数不能小于等于0
                if (buyTcShares <= 0 || cash < 0)
                {
                    return Tuple.Create(EMorderResponse.Error, new MOrder());
                }

                using (var codeRepo = new CodeRepository())
                {
                    CodeBizTaocan tc = await codeRepo.GetTaoCanByType(tcType);
                    if (tc == null)
                        throw new MDException(typeof(BizRepository), $"tc为空！tcType:{tcType}");
                    List<CodeBizTaocanItem> taocanItem = await codeRepo.GetTaoCanItemByType(tc.tc_type);
                    if (taocanItem == null)
                        throw new MDException(typeof(BizRepository), $"tcItem为空！tcType:{tcType}");
                    //判断是否可以生成订单

                    if (!await CanCreateMorder(tc, mer.mid))
                        return Tuple.Create(EMorderResponse.Overflow, new MOrder());//超出限制了


                    if (taocanItem != null && !mer.mid.Equals(Guid.Empty))
                    {
                        MOrder mo = new MOrder();
                        mo.moid = Guid.NewGuid();
                        mo.tc_type = tc.tc_type;
                        mo.cash = cash;
                        mo.status = (int)EMorderStatus.未支付;
                        mo.tc_price = tc.price;
                        mo.mid = mer.mid;
                        mo.buy_tc_shares = buyTcShares;
                        mo.timestamp = CommonHelper.GetUnixTimeNow();

                        context.Morder.Add(mo);
                        var ret = await context.SaveChangesAsync();
                        if (ret == 1)
                        {
                            return Tuple.Create(EMorderResponse.OK, mo);
                        }
                    }
                }
                return Tuple.Create(EMorderResponse.Error, new MOrder());
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<MOrder> GetModerAsync(Guid moid)
        {
            try
            {
                var mo = await (from m in context.Morder where m.moid == moid select m).FirstOrDefaultAsync();
                return RepositoryHelper.UpdateContextItem<MOrder>(context, mo);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<MOrder>> GetModerAsync(Guid mid, string tc_type)
        {
            try
            {
                var moList =
                    await
                        (from m in context.Morder
                         where m.mid.Equals(mid) && m.tc_type.Equals(tc_type)
                         orderby m.timestamp descending
                         select m)
                            .ToListAsync();
                return RepositoryHelper.UpdateContextItems<MOrder>(context, moList);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 获取一个mid下发起某个套餐的个数,用于校验
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="tc_type"></param>
        /// <returns></returns>
        public async Task<int> GetModerBoughtSharesAsync(Guid mid, string tc_type)
        {
            try
            {
                var count =
                    await
                        (from m in context.Morder
                         where m.mid.Equals(mid) && m.tc_type.Equals(tc_type) && m.status == (int)EMorderStatus.已生效
                         select m).SumAsync(m => m.buy_tc_shares);
                if (count == null)
                    return 0;
                return count.Value;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 完成支付，状态到已生效.通常是支付后。
        /// </summary>
        /// <param name="moid">uuid</param>
        /// <returns>null可能是订单超出了限制。</returns>
        public async Task<EMorderResponse> MorderFinish(Guid moid, CodeMerPayType payType, int cash, double payTime, string transactionId)
        {
            try
            {
                var mo = await GetModerAsync(moid);
                if (mo == null || mo.moid.Equals(Guid.Empty))
                    return EMorderResponse.Error;
                List<CodeBizTaocanItem> tcItem = null;
                using (var codeRepo = new CodeRepository())
                {
                    CodeBizTaocan tc = await codeRepo.GetTaoCanByType(mo.tc_type);
                    //判断是否可以生成订单
                    if (!await CanCreateMorder(tc, mo.mid)) //0表示无限制
                    {
                        return EMorderResponse.Overflow; //超出限制了
                    }
                    tcItem = await codeRepo.GetTaoCanItemByType(tc.tc_type);
                    if (tcItem == null || tcItem.Count == 0)
                        return EMorderResponse.Error;
                }

                mo.pay_time = payTime;
                mo.cash = cash;
                mo.pay_transactionid = transactionId;
                mo.pay_type = payType.code;
                mo.status = (int)EMorderStatus.已生效;


                //根据TaoCanType.TaocanItem生成mbiz记录
                foreach (CodeBizTaocanItem taocanItem in tcItem)
                {
                    var mBiz = await GetMbizAsync(mo.mid, taocanItem.biz_type);
                    CodeBizType biztype = null;
                    using (var codeRepo = new CodeRepository())
                    {
                        biztype = await codeRepo.GetCodeBizType(taocanItem.biz_type);
                        if (biztype == null)
                            return EMorderResponse.Error;
                    }

                    //不是新加的biztype
                    if (mBiz != null && !mBiz.mid.Equals(Guid.Empty))
                    {
                        mBiz.last_add_time = CommonHelper.GetUnixTimeNow();
                        mBiz.quota_remain += mo.buy_tc_shares * taocanItem.biz_unit_count;
                    }
                    else//新加入的biztype
                    {
                        mBiz = new MBiz()
                        {
                            mid = mo.mid,
                            biz_type = taocanItem.biz_type,
                            quota_remain = mo.buy_tc_shares * taocanItem.biz_unit_count,
                            audit_period = biztype.audit_period,
                            last_add_time = CommonHelper.GetUnixTimeNow(),
                            isvalid = true
                        };
                        context.MerBizes.Add(mBiz);
                    }
                }
                await context.SaveChangesAsync();
                return EMorderResponse.OK;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 撤销订单.
        /// </summary>
        /// <param name="moid"></param>
        /// <returns></returns>
        public async Task<MOrder> MorderRemoveAsync(Guid moid)
        {
            var mo = await GetModerAsync(moid);
            if (mo == null || mo.moid.Equals(Guid.Empty))
                return null;

            mo.status = (int)EMorderStatus.已撤销;

            await context.SaveChangesAsync();
            return RepositoryHelper.UpdateContextItem(context, mo);
        }
        #endregion

        #region MBiz

        /// <summary>
        /// 获取业务总信息.mid+bizType为主键
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="bizType"></param>
        /// <returns></returns>
        public async Task<MBiz> GetMbizAsync(Guid mid, string bizType)
        {
            try
            {
                if (mid.Equals(Guid.Empty) || string.IsNullOrEmpty(bizType))
                    return null;
                var mBiz =
                    await
                        (from b in context.MerBizes
                         where b.mid.Equals(mid) && b.biz_type.Equals(bizType) && b.isvalid.Value
                         select b)
                            .FirstOrDefaultAsync();
                return RepositoryHelper.UpdateContextItem(context, mBiz);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public MBiz GetMbiz(Guid mid, string bizType)
        {
            try
            {
                if (mid.Equals(Guid.Empty) || string.IsNullOrEmpty(bizType))
                    return null;
                var mBiz =

                        (from b in context.MerBizes
                         where b.mid.Equals(mid) && b.biz_type.Equals(bizType) && b.isvalid.Value
                         select b)
                            .FirstOrDefault();
                return mBiz;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public async Task<bool> EditMBizQuota(Guid mid, string bizType)
        {
            try
            {
                if (mid.Equals(Guid.Empty) || string.IsNullOrEmpty(bizType))
                    return false;
                var biz = await GetMbizAsync(mid, bizType);
                if (biz == null || biz.quota_remain <= 0)
                    return false;
                biz.quota_remain = biz.quota_remain - 1;
                return await context.SaveChangesAsync() == 1;

            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        #endregion

        #region Group

        public bool GroupUpdate(Group obj)
        {
            try
            {
                if (obj.gid.Equals(Guid.Empty))
                    return false;
                var dbObject = GroupGetGroupById_TB(obj.gid);

                foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                {
                    var v = pi.GetValue(obj);
                    if (v != null)
                        pi.SetValue(dbObject, v);
                }

                var ret = context.SaveChanges();
                EsGroupManager.AddOrUpdateAsync(EsGroupManager.GenObject_TB(obj.gid));
                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<Group> GroupAddOrUpdateAsync(Group g)
        {
            if (g == null)
                return null;
            var group = await GroupGetGroupById(g.gid);

            //新加入
            if (group == null)
            {

                if (g.gid == Guid.Empty)
                    g.gid = Guid.NewGuid();

                g.status = (int)EGroupStatus.待发布;
                g.lucky_status = (int)EGroupLuckyStatus.待开奖;
                g.last_update_user = Guid.Empty;
                g.last_update_time = CommonHelper.GetUnixTimeNow();
                context.Groups.Add(g);
                await context.SaveChangesAsync();
                RepositoryHelper.UpdateContextItem(context, g);
                using (var attrepo = new AttRepository())
                {
                    await attrepo.activity_pointAddOrUpdateAsync(g.gid, g.activity_point);
                    if (g.leader_price != null)
                        await attrepo.leader_priceAddOrUpdateAsync(g.gid, g.leader_price.Value);
                    if (g.userobot != null)
                        await attrepo.userobotAddOrUpdateAsync(g.gid, g.userobot.Value);
                    if (g.order_limit != null)
                        await attrepo.order_limitAddOrUpdateAsync(g.gid, g.order_limit.Value);
                    if (g.group_type != null)
                        await attrepo.group_typeAddOrUpdateAsync(g.gid, g.group_type.Value);
                    if (g.lucky_status != null)
                        await attrepo.lucky_statusAddOrUpdateAsync(g.gid, g.lucky_status.Value);
                }
                return g;
            }
            else //更新
            {
                foreach (var pi in typeof(Group).GetProperties())
                {
                    if (pi.Name.Contains("gid") || pi.Name.Contains("mid") || pi.Name.Contains("pid"))
                        continue;
                    var parameterValue = pi.GetValue(g);
                    if (parameterValue != null)
                    {
                        pi.SetValue(group, parameterValue);
                    }
                }
            }

            await context.SaveChangesAsync();
            RepositoryHelper.UpdateContextItem(context, group);
            using (var attrepo = new AttRepository())
            {
                await attrepo.activity_pointAddOrUpdateAsync(g.gid, g.activity_point);
                if (g.leader_price != null)
                    await attrepo.leader_priceAddOrUpdateAsync(group.gid, g.leader_price.Value);
                if (g.userobot != null)
                    await attrepo.userobotAddOrUpdateAsync(g.gid, g.userobot.Value);
                if (g.order_limit != null)
                    await attrepo.order_limitAddOrUpdateAsync(g.gid, g.order_limit.Value);
                if (g.group_type != null)
                    await attrepo.group_typeAddOrUpdateAsync(g.gid, g.group_type.Value);
                if (g.lucky_status != null)
                    await attrepo.lucky_statusAddOrUpdateAsync(g.gid, g.lucky_status.Value);
            }
            return group;
        }
        public async Task<Group> Group_luckyUpdateAsync(Group g)
        {
            if (g == null)
                return null;
            using (var attrepo = new AttRepository())
            {
                await attrepo.lucky_countAddOrUpdateAsync(g.gid, g.lucky_count.Value);
                await attrepo.lucky_endTimeAddOrUpdateAsync(g.gid, g.lucky_endTime);
            }
            var group = await GroupGetGroupById(g.gid);
            return group;
        }
        public async Task<Group> GroupGetGroupById(Guid gid)
        {
            var ret = await (from g in context.Groups where g.gid.Equals(gid) select g).FirstOrDefaultAsync();
            if (ret != null)
            {
                using (var attrepo = new AttRepository())
                {
                    ret = await attrepo.PatchGroup(ret);
                }
            }
            return ret;
        }

        public Group GroupGetGroupById_TB(Guid gid)
        {
            var ret = (from g in context.Groups where g.gid.Equals(gid) select g).FirstOrDefault();
            if (ret != null)
            {
                using (var attrepo = new AttRepository())
                {
                    ret = attrepo.PatchGroup_TB(ret);
                }
            }
            return ret;
        }

        public async Task<int> GroupCountByStatus(Guid mid, int status)
        {
            try
            {
                var count =
                    await (from g in context.Groups where g.status == status && g.mid.Equals(mid) select g).CountAsync();
                return count;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Group>> GroupGetByStatus(Guid mid, int status)
        {
            try
            {
                var list = await (from g in context.Groups where g.status == status && g.mid.Equals(mid) select g).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 如果是已发布，则已过期与已结束的都出来(给页面使用)
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="status"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<Tuple<int, List<Group>>> GroupGetListByStatus(Guid mid, int status, int pageIndex, int pageSize)
        {
            try
            {
                if (status == (int)EGroupStatus.待发布)
                {
                    var count = await GroupCountByStatus(mid, status);
                    if (count > 0)
                    {
                        int from = (pageIndex - 1) * pageSize;
                        var list =
                            await
                                (from g in context.Groups where g.status == status && g.mid.Equals(mid) orderby g.last_update_time descending select g).Skip(from)
                                    .Take(pageSize)
                                    .ToListAsync();
                        if (list != null && list.Count > 0)
                        {
                            using (var attRepr = new AttRepository())
                            {
                                list = await attRepr.PatchGroup(list);
                            }
                            return Tuple.Create(count, list);
                        }

                    }
                    return Tuple.Create(0, new List<Group>());
                }
                else if (status == (int)EGroupStatus.已发布)  //进行中
                {
                    var count = await GroupCountByStatus(mid, status);
                    if (count > 0)
                    {
                        int from = (pageIndex - 1) * pageSize;
                        var list =
                            await
                                (from g in context.Groups where g.status == status && g.mid.Equals(mid) orderby g.last_update_time descending select g).Skip(from)
                                    .Take(pageSize)
                                    .ToListAsync();
                        if (list != null && list.Count > 0)
                        {
                            using (var attRepr = new AttRepository())
                            {
                                list = await attRepr.PatchGroup(list);
                            }
                            return Tuple.Create(count, list);
                        }
                    }
                    return Tuple.Create(0, new List<Group>());
                }
                else if (status == (int)EGroupStatus.已过期)
                {
                    //var count0 = await GroupCountByStatus(mid, (int)EGroupStatus.已发布);
                    var count3 = await GroupCountByStatus(mid, (int)EGroupStatus.已过期);
                    var count4 = await GroupCountByStatus(mid, (int)EGroupStatus.已结束);
                    var count = count3 + count4;
                    if (count > 0)
                    {
                        int from = (pageIndex - 1) * pageSize;
                        var list =
                            await
                                (from g in context.Groups where (g.status == (int)EGroupStatus.已过期 || g.status == (int)EGroupStatus.已结束) && g.mid.Equals(mid) orderby g.last_update_time descending select g).Skip(from)
                                    .Take(pageSize)
                                    .ToListAsync();
                        if (list != null && list.Count > 0)
                        {
                            using (var attRepr = new AttRepository())
                            {
                                list = await attRepr.PatchGroup(list);
                            }
                            return Tuple.Create(count, list);
                        }

                    }
                    return Tuple.Create(0, new List<Group>());
                }
                else if (status == (int)EGroupStatus.已删除)
                {
                    var count = await GroupCountByStatus(mid, (int)EGroupStatus.已删除);
                    if (count > 0)
                    {
                        int from = (pageIndex - 1) * pageSize;
                        var list =
                            await
                                (from g in context.Groups where g.status == (int)EGroupStatus.已删除 && g.mid.Equals(mid) orderby g.last_update_time descending select g).Skip(from)
                                    .Take(pageSize)
                                    .ToListAsync();
                        if (list != null && list.Count > 0)
                        {
                            using (var attRepr = new AttRepository())
                            {
                                list = await attRepr.PatchGroup(list);
                            }
                            return Tuple.Create(count, list);
                        }
                    }
                    return Tuple.Create(0, new List<Group>());
                }
                return Tuple.Create(0, new List<Group>());
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Group>> GroupGetByList(List<Guid> gids)
        {
            try
            {
                var list = await (from g in context.Groups where gids.Contains(g.gid) orderby g.last_update_time descending select g).ToListAsync();
                return RepositoryHelper.UpdateContextItems(context, list);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> GroupDel(Guid gid)
        {
            try
            {
                var group = await GroupGetGroupById(gid);
                if (group == null)
                    return false;
                group.status = (int)EGroupStatus.已删除;
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> GroupOnline(Guid gid)
        {
            try
            {
                var group = await GroupGetGroupById(gid);
                if (group == null)
                    return false;
                group.status = (int)EGroupStatus.已发布;
                group.group_start_time = CommonHelper.GetUnixTimeNow();
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> GroupResume(Guid gid)
        {
            try
            {
                var group = await GroupGetGroupById(gid);
                if (group == null)
                    return false;
                group.status = (int)EGroupStatus.待发布;
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 库存升级
        /// </summary>
        /// <param name="gid"></param>
        /// <returns></returns>
        public async Task<bool> GroupInventoryClear(Guid gid)
        {
            try
            {
                var group = await GroupGetGroupById(gid);
                if (group == null)
                    return false;
                group.status = (int)EGroupStatus.已结束;
                group.group_end_time = CommonHelper.GetUnixTimeNow();
                group.product_quota = 0;
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 增加库存
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="product_quota"></param>
        /// <returns></returns>
        public async Task<bool> GroupInventoryAdd(Guid gid, int product_quota)
        {
            try
            {
                var group = await GroupGetGroupById(gid);
                if (group == null)
                    return false;
                group.product_setting_count += product_quota;
                group.product_quota += product_quota;
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public async Task<bool> UpdateGroupSetting_Count(Guid gid, int product_setting_count)
        {
            try
            {
                var group = await GroupGetGroupById(gid);
                if (group == null)
                    return false;
                group.product_setting_count = product_setting_count;
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public async Task<bool> UpdateGroupGroup_type(Guid gid, int group_type)
        {
            try
            {
                var group = await GroupGetGroupById(gid);
                if (group == null)
                    return false;
                group.group_type = group_type;
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 获取一个团的库存数。
        /// </summary>
        /// <param name="gid"></param>
        /// <returns></returns>
        public int? GroupGetInventoryCount(Guid gid)
        {
            try
            {
                var count =
                    (from g in context.Groups where g.gid.Equals(gid) select g.product_quota).FirstOrDefault();
                return count;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Group>> luckyGroupGetAsync()
        {
            try
            {
                var attid_type = AttHelper.GetAttid(EAttTables.Group.ToString(), EGroupAtt.group_type.ToString());//抽奖团的attid
                var attid_luckystatus = AttHelper.GetAttid(EAttTables.Group.ToString(), EGroupAtt.lucky_status.ToString());//抽奖团状态的attid
                var attid_endTime = AttHelper.GetAttid(EAttTables.Group.ToString(), EGroupAtt.lucky_endTime.ToString());//抽奖团结束时间的attid
                                                                                                                        //根据attid获取attvalueList
                using (var attr = new AttRepository())
                {
                    var attvalue_luckyList = await attr.AttValueGetByAttid(attid_type, ((int)EGroupTypes.抽奖团).ToString());//所有抽奖团的ownerList，后期数据量大可以加时间限制
                    if (attvalue_luckyList == null || attvalue_luckyList.Count == 0)
                        return null;
                    var attvalue_status = await attr.AttValueGetByOwners(attvalue_luckyList, attid_luckystatus, ((int)EGroupLuckyStatus.待开奖).ToString());//所有是抽奖团，并且是待开奖的ownerList
                    if (attvalue_status == null || attvalue_status.Count == 0)
                        return null;
                    string nowtime = CommonHelper.GetUnixTimeNow().ToString();
                    var attvalue_endTime = await attr.AttValueGetByOwners2(attvalue_status, attid_endTime, nowtime);//所有是抽奖团，并且是待开奖，并且抽奖结束时间<当前时间
                    if (attvalue_endTime == null || attvalue_endTime.Count == 0)
                        return null;
                    return await GroupGetByList(attvalue_endTime);
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), new Exception("luckyGroupGetAsync" + ex.Message));
            }
        }

        public List<Group> luckyGroupGet()
        {
            try
            {
                var attid_luckystatus = AttHelper.GetAttid(EAttTables.Group.ToString(), EGroupAtt.lucky_status.ToString());//抽奖团状态的attid
                var attid_endTime = AttHelper.GetAttid(EAttTables.Group.ToString(), EGroupAtt.lucky_endTime.ToString());//抽奖团结束时间的attid
                string nowtime = CommonHelper.GetUnixTimeNow().ToString();
                int type = (int)EGroupTypes.抽奖团;
                using (AttRepository attRepo = new AttRepository())
                {
                    var attContext = attRepo.Context;
                    string groupLuckyStatus = ((int)EGroupLuckyStatus.待开奖).ToString();
                    int groupStatus = (int)EGroupStatus.已发布;
                    var listAttr =
                    (from v_status in attContext.AttValues.Where(v => v.attid == attid_luckystatus && v.value.Equals(groupLuckyStatus)).ToList()
                     join v_time in attContext.AttValues.Where(v => v.attid == attid_endTime && v.value.CompareTo(nowtime) < 0).ToList() on v_status.owner equals v_time.owner
                     select new { id = v_status.owner, lucky_endTime = v_time.value }
                     ).ToList();
                    var list =
                    (from v in listAttr join g in context.Groups on v.id equals g.gid where g.group_type == type && g.status.Value == groupStatus select new Group() { gid = g.gid, pid = g.pid, title = g.title, lucky_endTime = v.lucky_endTime, mid = g.mid }).ToList();
                    return list;
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception("luckyGroupGet" + ex.Message));
                return new List<Group>();
            }
        }

        public List<Group> GetAllGroupList()
        {
            var list = (from g in context.Groups select g).ToList();
            return list;
        }

        public async Task<bool> GroupMediaAddAsync(Group_Media media)
        {
            context.Group_Media.Add(media);
            return await context.SaveChangesAsync() == 1;
        }

        public bool GroupMediaAdd(Group_Media media)
        {
            media.createtime = CommonHelper.GetUnixTimeNow();
            context.Group_Media.Add(media);
            int res = context.SaveChanges();
            return res == 1;
        }

        public async Task<Group_Media> GroupMediaGetByGid(Guid gid)
        {
            return await context.Group_Media.FirstOrDefaultAsync(m => m.gid == gid);
        }
        #endregion

        #region GroupOrder

        /// <summary>
        /// 查出N分钟内过期的团订单。
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public async Task<List<GroupOrder>> GroupOrderGetbyExpiretimeAsync(int minutes)
        {
            try
            {
                if (minutes <= 0)
                    return null;

                double now = CommonHelper.GetUnixTimeNow();
                double delta = now + minutes * 60;
                int gostatus = (int)EGroupOrderStatus.拼团进行中;


                var list = await (from go in context.GroupOrders
                                  where go.status == gostatus && go.expire_date < delta
                                  select go).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        private async Task<bool> GroupOrderDoesOpened(Guid gId, Guid leader)
        {
            try
            {
                var count =
                    await
                        (from go in context.GroupOrders where go.gid.Equals(gId) && go.leader.Equals(leader) select go)
                            .CountAsync();
                return count == 0;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 开团失败状态的记录排除
        /// </summary>
        /// <param name="gId"></param>
        /// <param name="leader"></param>
        /// <returns></returns>
        private async Task<GroupOrder> IsDupulicateOpenGoAsync(Guid gId, Guid leader)
        {
            try
            {
                var ret =
                    await
                        (from go in context.GroupOrders where go.gid.Equals(gId) && go.leader.Equals(leader) && (go.status == (int)EGroupOrderStatus.开团中 || go.status == (int)EGroupOrderStatus.拼团进行中) select go)
                            .FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 团长开团
        /// </summary>
        /// <param name="group"></param>
        /// <param name="LeaderUuid"></param>
        /// <returns></returns>
        public async Task<GroupOrder> GroupOderOpenAsync(Group group, Guid LeaderUuid)
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
                    if (goDupli.status == (int)EGroupOrderStatus.开团中)
                    {
                        //更新团订单过期时间
                        goDupli.create_date = CommonHelper.GetUnixTimeNow();
                        DateTime expiDateDupli = DateTime.Now.AddSeconds(group.time_limit.Value);
                        goDupli.expire_date = CommonHelper.ToUnixTime(expiDateDupli);
                        await GroupOrderUpdateAsync(goDupli);
                    }
                    return goDupli;
                }


                //新开团
                GroupOrder newGo = new GroupOrder();
                newGo.goid = Guid.NewGuid();
                newGo.go_no = CommonHelper.GetId32(EIdPrefix.GO);
                newGo.gid = group.gid;
                newGo.leader = LeaderUuid;
                newGo.create_date = CommonHelper.GetUnixTimeNow();
                newGo.pid = group.pid;
                newGo.price = group.origin_price;
                newGo.user_left = group.person_quota;
                newGo.go_price = group.group_price;

                //截止日期
                DateTime expiDate = DateTime.Now.AddSeconds(group.time_limit.Value);
                newGo.expire_date = CommonHelper.ToUnixTime(expiDate);

                newGo.status = (int)EGroupOrderStatus.开团中;
                context.GroupOrders.Add(newGo);
                await context.SaveChangesAsync();
                return newGo;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public async Task<List<GroupOrder>> GroupOrderGetByListAsnyc(List<Guid> goids)
        {
            try
            {
                var list =
                    await
                        (from go in context.GroupOrders
                         where goids.Contains(go.goid)
                         orderby go.create_date descending
                         select go).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<GroupOrder> GroupOrderGet(Guid goid)
        {
            try
            {
                var ret = await (from go in context.GroupOrders where go.goid.Equals(goid) select go).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public GroupOrder GroupOrderGet_TB(Guid goid)
        {
            try
            {
                var ret = (from go in context.GroupOrders where go.goid.Equals(goid) select go).FirstOrDefault();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> GroupOrderUpdateAsync(GroupOrder obj)
        {
            try
            {
                if (obj.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.go_no))
                    return false;
                var dbObject = await GroupOrderGet(obj.goid);
                //if (dbObject == null)
                //    return false;

                foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                {
                    var v = pi.GetValue(obj);
                    if (v != null)
                        pi.SetValue(dbObject, v);
                }

                var ret = await context.SaveChangesAsync();

                await EsGroupOrderManager.AddOrUpdateAsync(await EsGroupOrderManager.GenObjectAsync(dbObject));

                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public bool GroupOrderUpdate(GroupOrder obj)
        {
            try
            {
                if (obj.goid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.go_no))
                    return false;
                var dbObject = GroupOrderGet_TB(obj.goid);
                //if (dbObject == null)
                //    return false;

                foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                {
                    var v = pi.GetValue(obj);
                    if (v != null)
                        pi.SetValue(dbObject, v);
                }
                var ret = context.SaveChanges();
                //MDLogger.LogInfoAsync(typeof(BizRepository),$"在GroupOrderUpdate-》ret:{ret},goid:{obj.goid}");
                EsGroupOrderManager.AddOrUpdate(EsGroupOrderManager.GenObject(dbObject));
                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 获取因为过期而组团失败的团订单列表。
        /// user_left > 0 && expire_date < dnow && status == 0
        /// </summary>
        /// <returns></returns>
        public async Task<List<GroupOrder>> GroupOrderGetFailsByTimeLimitsAsync()
        {
            try
            {
                var dnow = CommonHelper.GetUnixTimeNow();
                var ret = await (from go in context.GroupOrders
                                 where go.user_left > 0 && go.expire_date < dnow && go.status == (int)EGroupOrderStatus.拼团进行中
                                 select go).ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public List<GroupOrder> GroupOrderGetFailsByTimeLimits()
        {
            try
            {
                var dnow = CommonHelper.GetUnixTimeNow();
                var ret = (from go in context.GroupOrders
                           where go.expire_date < dnow && go.status == (int)EGroupOrderStatus.拼团进行中
                           select go).ToList();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 扫描将要结束的团
        /// </summary>
        /// <param name="timeEndHours">团将要结束的时间，单位：小时</param>
        /// <param name="timeInterval">扫描时间间隔，单位：分钟</param>
        public List<GroupOrder> GroupOrdersGetByTime(int timeEndHours, int timeInterval)
        {
            // go.expire_date - 3h + 20min < timeNow < go.expire_date - 3h
            try
            {
                DateTime timeNow = DateTime.Now;
                double timeStart = CommonHelper.ToUnixTime(timeNow.AddHours(timeEndHours));
                double timeEnd = CommonHelper.ToUnixTime(timeNow.AddHours(timeEndHours).AddMinutes(timeInterval));
                /*作测试用*/
                //Guid gid = Guid.Parse("0a890564-ffc5-4ba7-b267-70f02d337d34");
                //var ret = (from go in context.GroupOrders
                //           where go.gid == gid && go.status == (int)EGroupOrderStatus.拼团进行中 && go.expire_date > timeStart && go.expire_date < timeEnd
                //           select go).ToList();
                var ret = (from go in context.GroupOrders
                           where go.status == (int)EGroupOrderStatus.拼团进行中 && go.expire_date > timeStart && go.expire_date < timeEnd
                           select go).ToList();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<GroupOrder>> GroupOrderGetByGidAsync(Guid gid, EGroupOrderStatus goStatus)
        {

            try
            {
                var ret =
                    await
                        (from go in context.GroupOrders
                         where go.gid.Equals(gid) && go.status == (int)goStatus
                         select go)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Guid>> GroupOrderGetByGidRetunGuidAsync(Guid gid, EGroupOrderStatus goStatus)
        {

            try
            {
                var ret =
                    await
                        (from go in context.GroupOrders
                         where go.gid.Equals(gid) && go.status == (int)goStatus
                         select go.goid)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<GroupOrder>> GroupOrderGetByGidAsync(Guid gid)
        {

            try
            {
                var ret =
                    await
                        (from go in context.GroupOrders
                         where go.gid.Equals(gid)
                         select go)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<int> GetCountByMidAsync(Guid Mid, List<int> status, double f, double t)
        {
            try
            {
                var count = await (from g in context.GroupOrders
                                   join gp in context.Groups on g.gid equals gp.gid
                                   where gp.mid.Equals(Mid) && g.create_date >= f && g.create_date <= t && status.Contains(g.status.Value)
                                   select g).CountAsync();
                return count;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 校正go的人数状态。
        /// </summary>
        /// <param name="goid"></param>
        public async void GroupOrderVerifyUserLeftAsync(Guid goid)
        {
            try
            {
                var go = await GroupOrderGet(goid);
                if (go != null && go.status == (int)EGroupOrderStatus.拼团进行中)
                {
                    var group = await GroupGetGroupById(go.gid);
                    if (group == null) return;
                    int userLeft = 0;

                    var oList = await OrderGetByGoidAsync(goid, EOrderStatus.已支付);
                    //一个处于拼团进行中的go，却一个已支付的订单都没有，所以需要关闭go。
                    //应该至少有一个——团长
                    if (oList == null || oList.Count == 0)
                    {
                        userLeft = group.person_quota.Value;
                        go.status = (int)EGroupOrderStatus.拼团失败;
                    }
                    else
                    {
                        userLeft = group.person_quota.Value - oList.Count;
                    }


                    go.user_left = userLeft;
                    await GroupOrderUpdateAsync(go);

                }

                //如果正好拼团成功，则需要回滚状态
                if (go != null && go.status == (int)EGroupOrderStatus.拼团成功)
                {
                    var oList = await OrderGetByGoidAsync2(goid);
                    int userCount = 0;
                    foreach (var o in oList)
                    {
                        if (o.status != (int)EOrderStatus.已退款 && o.status != (int)EOrderStatus.退款中 &&
                            o.status != (int)EOrderStatus.退款失败 && o.status != (int)EOrderStatus.未支付)
                        {
                            userCount = userCount + 1;
                        }
                    }
                    var group = await GroupGetGroupById(go.gid);
                    //zzh修改，如果是抽奖团，则不修改GroupOrder的状态，因为未中奖会退款，造成GroupOrder状态被修改。
                    if (group == null || group.group_type == (int)EGroupTypes.抽奖团) return;

                    //满了，还有多。在进入这个过程之前，退款订单的状态已经改变。
                    if (group.person_quota <= userCount)
                        return;

                    //如果现在支付成功以上的人数小于人数配额，应该是拼团进行中。
                    //进行修正
                    go.status = (int)EGroupOrderStatus.拼团进行中;
                    go.user_left = group.person_quota - userCount;
                    await GroupOrderUpdateAsync(go);
                    MDLogger.LogInfoAsync(typeof(BizRepository), $"退款失败，回滚团订单状态!goid:{go.goid},已经支付的人数：{oList.Count},回滚到 拼团进行中。后的人数:{go.user_left - 1}");
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 矫正go的人数状态
        /// </summary>
        /// <param name="goid"></param>
        public void GroupOrderVerifyUserLeft(Guid goid)
        {
            try
            {
                var go = GroupOrderGet_TB(goid);
                if (go != null && go.status == (int)EGroupOrderStatus.拼团进行中 && go.user_left > 0)
                {
                    var group = GroupGetGroupById_TB(go.gid);
                    if (group == null) return;
                    int userLeft = 0;

                    var oList = OrderGetByGoid(goid, EOrderStatus.已支付);
                    //处于拼团中的go，如果一笔支付成功的订单都没有则为异常go，必须关闭。
                    if (oList == null || oList.Count == 0)
                    {
                        userLeft = group.person_quota.Value;
                        go.status = (int)EGroupOrderStatus.拼团失败;
                    }
                    else
                    {
                        userLeft = group.person_quota.Value - oList.Count;
                    }


                    go.user_left = userLeft > 0 ? userLeft : 0;
                    GroupOrderUpdate(go);
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 判断go的状态与人数差的情况，判断是否拼团成功，确保在拼团成功后，order的状态都对。
        /// 拼团成功后更改所有订单的状态
        /// </summary>
        /// <param name="goid"></param>
        public void GroupOrderVerifyOrderStatusAfterGoSuccess(GroupOrder go, Group group)
        {
            try
            {
                if (go == null || group == null) return;
                if (go.status == (int)EGroupOrderStatus.拼团成功 && go.user_left == 0)
                {
                    var oList = OrderGetByGoid2(go.goid, EOrderStatus.已支付);
                    //go状态错误，需要校正
                    if (oList == null || oList.Count != group.person_quota)
                    {
                        go.status = (int)EGroupOrderStatus.拼团进行中;
                        GroupOrderUpdate(go);
                    }
                    else if (oList.Count == group.person_quota)
                    {
                        int statusToChange = 0;
                        foreach (var o in oList)
                        {
                            if (o.waytoget == (int)EWayToGet.自提)
                                statusToChange = (int)EOrderStatus.已成团未提货;
                            if (o.waytoget == (int)EWayToGet.物流)
                                statusToChange = (int)EOrderStatus.已成团未发货;
                            o.status = statusToChange;
                            OrderUpDate(o);
                            Guid buyer = o.buyer;
                            if (!RobotHelper.IsRobot(buyer))
                            {
                                try
                                {
                                    //判断订单中是否有分销未拼团成功的记录
                                    var distribution = GetDistributionByOid(o.oid);//判断该订单是否有分销记录
                                    if (distribution != null && distribution.isptsucceed == 0)
                                    {
                                        //判断分享人是否为核销员，如果是，直接累加他的佣金
                                        var woffer = WoerCanWriteOff_TB(distribution.mid, distribution.sharer);
                                        if (woffer != null)
                                        {
                                            int last_commission = woffer.commission;//先记录上一次的总佣金
                                            //直接累加distribution.sharer的佣金
                                            var addwoffer = addWriteOfferCommission(distribution.sharer, distribution.mid, distribution.commission);
                                            if (addwoffer != null)
                                                UpdateDisIsPTSucceed(o.oid, last_commission, addwoffer.commission);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"拼团成功后修改分销数据报错,oid:{o.oid},ex:{ex.Message}"));
                                }
                                MqWxTempMsgManager.SendMessageAsync(o, TemplateType.PTSuccess);
                            }
                        }
                    }
                    MqVectorManager.Send<PtSuccessVectorProcessor>(go.goid);
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogInfo(typeof(BizRepository), "更改团订单与订单状态时发生错误！");
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        #endregion

        #region Order

        public async Task<bool> UpdateOrderStatusByOid(Guid oid, int status)
        {
            var order = await (from j in context.Orders where j.oid.Equals(oid) select j).FirstOrDefaultAsync();
            if (order == null)
                return false;
            order.status = status;
            int i = await context.SaveChangesAsync();
            IndexOrder indexorder = await EsOrderManager.GetByIdAsync(oid);
            indexorder.status = status;
            var flag = await EsOrderManager.AddOrUpdateAsync(indexorder);
            return flag;
        }

        /// <summary>
        /// 根据状态查询order，已排除机器人
        /// </summary>
        /// <param name="statuses"></param>
        /// <returns></returns>
        public async Task<List<Order>> OrderGetByStatus(List<int> statuses)
        {
            try
            {
                if (statuses == null || statuses.Count == 0)
                    return null;
                var list =
                    await (from o in context.Orders where statuses.Contains(o.status.Value) && o.mid != RobotHelper.RobotMid select o).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 根据状态查询order，已排除机器人
        /// </summary>
        /// <param name="statuses"></param>
        /// <returns></returns>
        public List<Order> OrderGetByStatus_TB(List<int> statuses)
        {
            try
            {
                if (statuses == null || statuses.Count == 0)
                    return null;
                var list = (from o in context.Orders where statuses.Contains(o.status.Value) && o.mid != RobotHelper.RobotMid select o).ToList();
                return list;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 不等于已退款的其他状态的其他都是重复的订单。
        /// </summary>
        /// <param name="goid"></param>
        /// <param name="buyer"></param>
        /// <returns></returns>
        private async Task<Order> OrderIsDuplicate(Guid goid, Guid buyer)
        {
            var ret =
                await
                    (from o in context.Orders where o.goid.Equals(goid) && o.buyer.Equals(buyer) && o.status != (int)EOrderStatus.已退款 select o)
                        .FirstOrDefaultAsync();
            return ret;
        }
        /// <summary>
        /// 根据gid和buyer查询订单
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="buyer"></param>
        /// <returns></returns>
        public List<Order> OrderGetGidAndBuyer(Guid gid, Guid buyer, List<int> statuses)
        {
            try
            {
                if (statuses == null || statuses.Count == 0)
                    return null;

                var order = (from o in context.Orders
                             where o.gid.Equals(gid) && o.buyer.Equals(buyer)
    && statuses.Contains(o.status.Value)
                             select o).ToList();

                return order;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }

        }
        private async Task<bool> ifNeedNewOtnAsync(Order order)
        {
            if (order?.status == (int)EOrderStatus.未支付)
            {
                var prePay =
                    await
                        (from p in context.WXPrePays
                         where p.out_trade_no.Equals(order.o_no) && p.result_code.Equals("SUCCESS")
                         select p).CountAsync();
                return prePay >= 1;
            }
            return false;
        }
        private async Task<bool> DeleteOrderAsync(Guid oid, int ostatus)
        {
            var order = await (from o in context.Orders where o.oid.Equals(oid) && o.status.Value.Equals(ostatus) select o).FirstOrDefaultAsync();
            if (order != null)
            {
                context.Orders.Remove(order);
                if (await context.SaveChangesAsync() == 1)
                {
                    //删除ES中的ORDER
                    await EsOrderManager.DeleteAsync(oid);
                    return true;
                }
            }
            return false;
        }

        public async Task<Order> OrderCreate(GroupOrder go, Group group, Guid buyer, int waytoget, User_WriteOff uw, UserPost up, int fee, int post_price)
        {
            if (go == null || buyer.Equals(Guid.Empty) || waytoget >= 2 || fee <= 0 || post_price < 0)
                return null;
            try
            {
                //是否重复,只有状态在已退款或者null的时候才能生成新订单。
                var tempOrder = await OrderIsDuplicate(go.goid, buyer);
                if (tempOrder != null && tempOrder.status == (int)EOrderStatus.未支付)
                {
                    //如果是未支付状态，有可能是因为换地址或换配送方式使金额发生改变，无法重新支付,需删除订单重新创建
                    if (fee + post_price != tempOrder.actual_pay)//如果新的支付金额!=原支付金额，则删除该订单
                    {
                        var flag = await DeleteOrderAsync(tempOrder.oid, (int)EOrderStatus.未支付);//删除数据库并更新es
                        if (flag)
                            tempOrder = await OrderIsDuplicate(go.goid, buyer);
                    }
                }

                if (waytoget == (int)EWayToGet.自提)
                {
                    if (tempOrder != null)
                    {
                        tempOrder.default_writeoff_point = uw.woid;
                        tempOrder.name = uw.user_name;
                        tempOrder.cellphone = uw.cellphone;
                        tempOrder.actual_pay = fee;
                        tempOrder.post_price = post_price;
                        tempOrder.waytoget = waytoget;
                        await context.SaveChangesAsync();
                        return tempOrder;
                    }
                    //新加入
                    Order order = new Order()
                    {
                        oid = Guid.NewGuid(),
                        o_no = CommonHelper.GetId32(EIdPrefix.OD),
                        actual_pay = fee,//go.go_price,
                        buyer = buyer,
                        default_writeoff_point = uw.woid,
                        goid = go.goid,
                        mid = group.mid,
                        order_price = fee,
                        waytoget = (int)EWayToGet.自提,
                        status = (int)EOrderStatus.未支付,
                        gid = group.gid,
                        paytime = CommonHelper.GetUnixTimeNow(),
                        name = uw.user_name,
                        cellphone = uw.cellphone,
                        upid = Guid.Empty,
                        post_price = 0,
                    };
                    context.Orders.Add(order);
                    await context.SaveChangesAsync();
                    return order;
                }
                else if (waytoget == (int)EWayToGet.物流)
                {
                    if (tempOrder != null)
                    {
                        tempOrder.default_writeoff_point = Guid.Empty;
                        tempOrder.name = up.name;
                        tempOrder.cellphone = up.cellphone;
                        tempOrder.postaddress = up.province + up.city + up.district + up.address;
                        tempOrder.actual_pay = fee + post_price;
                        tempOrder.post_price = post_price;
                        tempOrder.upid = up.upid;
                        tempOrder.waytoget = waytoget;
                        await context.SaveChangesAsync();
                        return tempOrder;
                    }
                    //新加入
                    Order order = new Order()
                    {
                        oid = Guid.NewGuid(),
                        o_no = CommonHelper.GetId32(EIdPrefix.OD),
                        actual_pay = fee + post_price,//go.go_price,
                        buyer = buyer,
                        default_writeoff_point = Guid.Empty,
                        goid = go.goid,
                        mid = group.mid,
                        order_price = fee,
                        waytoget = (int)EWayToGet.物流,
                        status = (int)EOrderStatus.未支付,
                        gid = group.gid,
                        paytime = CommonHelper.GetUnixTimeNow(),
                        name = up.name,
                        cellphone = up.cellphone,
                        postaddress = up.province + up.city + up.district + up.address,
                        upid = up.upid,
                        post_price = post_price
                    };
                    context.Orders.Add(order);
                    await context.SaveChangesAsync();
                    return order;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<Order> OrderCreate_robot(GroupOrder go, Group group, User robot, Guid defaultWoPoint, int fee)
        {
            if (go == null || robot.Equals(Guid.Empty))
                return null;
            try
            {
                //是否重复,只有状态在已退款或者null的时候才能生成新订单。
                var tempOrder = await OrderIsDuplicate(go.goid, robot.uid);
                if (tempOrder != null)
                {
                    return tempOrder;
                }

                //新加入

                Order order = new Order()
                {
                    oid = Guid.NewGuid(),
                    o_no = CommonHelper.GetId32(EIdPrefix.OD),
                    actual_pay = fee,
                    buyer = robot.uid,
                    default_writeoff_point = defaultWoPoint,
                    goid = go.goid,
                    mid = robot.mid,
                    order_price = fee,
                    waytoget = 0,//机器人参团统一自提
                    status = (int)EOrderStatus.未支付,
                    gid = group.gid,
                    paytime = CommonHelper.GetUnixTimeNow(),
                    post_price = 0,
                    upid = Guid.Empty
                };
                context.Orders.Add(order);
                await context.SaveChangesAsync();
                return order;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Order>> OrderGetByListAsnyc(List<Guid> oids)
        {
            var list =
                await
                    (from o in context.Orders where oids.Contains(o.oid) orderby o.paytime descending select o)
                        .ToListAsync();
            return list;
        }

        public async Task<Order> OrderGetByOid(Guid oid)
        {
            try
            {
                var order = await (from o in context.Orders where o.oid.Equals(oid) select o).FirstOrDefaultAsync();
                return order;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public Order OrderGetByOid_TB(Guid oid)
        {
            try
            {
                var order = (from o in context.Orders where o.oid.Equals(oid) select o).FirstOrDefault();
                return order;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<Order> OrderGetByOutTradeNoAsync(string out_trade_no)
        {
            try
            {
                var order = await (from o in context.Orders where o.o_no.Equals(out_trade_no) select o).FirstOrDefaultAsync();
                return order;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public Order OrderGetByOutTradeNo(string out_trade_no)
        {
            try
            {
                var order = (from o in context.Orders where o.o_no.Equals(out_trade_no) select o).FirstOrDefault();
                return order;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> OrderUpDateAsync(Order obj)
        {
            try
            {
                if (obj.oid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.o_no))
                    return false;
                var dbObject = await OrderGetByOid(obj.oid);
                //if (dbObject == null)
                //    return false;

                foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                {
                    var v = pi.GetValue(obj);
                    if (v != null)
                        pi.SetValue(dbObject, v);
                }
                var ret = await context.SaveChangesAsync();

                await EsOrderManager.AddOrUpdateAsync(await EsOrderManager.GenObjectAsync(dbObject));

                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public bool OrderUpDate(Order obj)
        {
            try
            {
                if (obj.oid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.o_no))
                    return false;
                var dbObject = OrderGetByOid_TB(obj.oid);
                //if (dbObject == null)
                //    return false;

                foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                {
                    var v = pi.GetValue(obj);
                    if (v != null)
                        pi.SetValue(dbObject, v);
                }
                var ret = context.SaveChanges();

                EsOrderManager.AddOrUpdate(EsOrderManager.GenObject(dbObject));

                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public List<Order> OrderGetNeedRefund()
        {
            try
            {
                //var data = (from order in context.Orders where order.status==(int)EOrderStatus.退款中 && order.o_no
                var data = context.Orders.Where(
                    o => o.status == (int)EOrderStatus.退款中 && o.mid != RobotHelper.RobotMid && !context.WXRefunds.Any(r => r.out_trade_no == o.o_no))
                    .ToList();
                return data;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Order>> OrderGetNeedRefundAsync()
        {
            try
            {
                var data = await context.Orders.Where(
                    o => o.status == (int)EOrderStatus.退款中 && o.mid != RobotHelper.RobotMid && !context.WXRefunds.Any(r => r.out_trade_no == o.o_no))
                    .ToListAsync();
                return data;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Order>> OrderGetByMidAsync(Guid mid, List<int> statuses)
        {
            var list =
                await
                    (from o in context.Orders where o.mid.Equals(mid) && statuses.Contains(o.status.Value) select o)
                        .ToListAsync();
            return list;
        }

        public async Task<List<Order>> GetListOrder(Guid mid, string woid, int hxminTime, int hxmaxTime)
        {
            var orders = await (from j in context.Orders
                                where j.mid.Equals(mid) && j.extral_info.Equals(woid) && j.writeoffday >= hxminTime && j.writeoffday < hxmaxTime
                                select j).ToListAsync();
            // 
            return orders;
        }

        #region 根据goid获取order
        public async Task<List<Guid>> OrderGetByGoidAsync(Guid goid, EOrderStatus oStatus)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where or.goid.Equals(goid) && or.status == (int)oStatus
                         select or.oid)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public List<Guid> OrderGetByGoid(Guid goid, EOrderStatus oStatus)
        {
            try
            {
                var ret =

                        (from or in context.Orders
                         where or.goid.Equals(goid) && or.status == (int)oStatus
                         select or.oid)
                            .ToList();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public List<Order> OrderGetByGoid2(Guid goid, EOrderStatus oStatus)
        {
            try
            {
                var ret =

                        (from or in context.Orders
                         where or.goid.Equals(goid) && or.status == (int)oStatus
                         select or)
                            .ToList();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Order>> OrderGetByGoidAsync2(Guid goid, EOrderStatus oStatus)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where or.goid.Equals(goid) && or.status == (int)oStatus
                         select or)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Order>> OrderGetByGoidAsync2(Guid goid, List<int> oStatuslist)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where or.goid.Equals(goid) && oStatuslist.Contains(or.status.Value)
                         select or)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Guid>> OrderGetByGoidAsync(Guid goid)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where or.goid.Equals(goid)
                         select or.oid)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }

        }
        public async Task<List<Order>> OrderGetByGoidAsync2(Guid goid)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where or.goid.Equals(goid)
                         select or)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 根据Goid获取排除机器人后的orderList
        /// </summary>
        /// <param name="goid"></param>
        /// <returns></returns>
        public List<Order> OrderGetByGoid(Guid goid)
        {
            try
            {
                var ret = (from or in context.Orders
                           where or.goid.Equals(goid) && or.mid != RobotHelper.RobotMid
                           select or).ToList();
                return ret;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public async Task<List<Guid>> OrderGetByGoidAsync(List<Guid> goids, EOrderStatus oStatus)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where goids.Contains(or.goid) && or.status == (int)oStatus
                         select or.oid)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Guid>> OrderGetByGoidAsync(List<Guid> goids)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where goids.Contains(or.goid)
                         select or.oid)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Order>> OrderGetByGoidAsync2(List<Guid> goids, EOrderStatus oStatus)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where goids.Contains(or.goid) && or.status == (int)oStatus
                         select or)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Order>> OrderGetByGoidAsync2(List<Guid> goids, List<int> oStatuslist)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where goids.Contains(or.goid) && oStatuslist.Contains(or.status.Value)
                         select or)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<Order>> OrderGetByGoidAsync2(List<Guid> goids)
        {
            try
            {
                var ret =
                    await
                        (from or in context.Orders
                         where goids.Contains(or.goid)
                         select or)
                            .ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }


        #endregion


        #endregion

        #region Distribution
        public Distribution GetDistributionByOid(Guid oid)
        {
            var dis = (from d in context.Distributions where d.oid.Equals(oid) select d).FirstOrDefault();
            return dis;
        }
        /// <summary>
        /// 获取单个活动的推广列表
        public async Task<Tuple<int, List<Distribution>, int>> GetDistributionByGidandUidAsync(Guid gid, Guid sharer, int pageIndex, int pageSize)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0)
                    return null;
                int from = (pageIndex - 1) * pageSize;
                int sumCommission = 0;
                var total = await (from d in context.Distributions
                                   where d.gid.Equals(gid) && d.sharer.Equals(sharer) && d.isptsucceed == 1
                                   select d).CountAsync();
                var disList = await (from d in context.Distributions
                                     where d.gid.Equals(gid) && d.sharer.Equals(sharer) && d.isptsucceed == 1
                                     orderby d.lastupdatetime descending
                                     select d).Skip(from).Take(pageSize).ToListAsync();
                if (total > 0)
                    sumCommission = await (from d in context.Distributions
                                           where d.gid.Equals(gid) && d.sharer.Equals(sharer) && d.isptsucceed == 1
                                           select d).SumAsync(p => p.commission);
                return Tuple.Create(total, disList, sumCommission);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"fun:GetDistributionByGidandUidAsync1,ex:{ex.Message}"));
            }
            return null;
        }
        public async Task<Tuple<int, List<Distribution>, int>> GetDistributionByGidandUidAsync(Guid gid, Guid sharer, int pageIndex, int pageSize, int sourcetype)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0)
                    return null;
                int from = (pageIndex - 1) * pageSize;
                int sumCommission = 0;
                var total = await (from d in context.Distributions
                                   where d.gid.Equals(gid) && d.sharer.Equals(sharer) && d.isptsucceed == 1 && d.sourcetype.Equals(sourcetype)
                                   select d).CountAsync();
                var disList = await (from d in context.Distributions
                                     where d.gid.Equals(gid) && d.sharer.Equals(sharer) && d.isptsucceed == 1 && d.sourcetype.Equals(sourcetype)
                                     orderby d.lastupdatetime descending
                                     select d).Skip(from).Take(pageSize).ToListAsync();
                if (total > 0)
                    sumCommission = await (from d in context.Distributions
                                           where d.gid.Equals(gid) && d.sharer.Equals(sharer) && d.isptsucceed == 1 && d.sourcetype.Equals(sourcetype)
                                           select d
                                               ).SumAsync(p => p.commission);
                return Tuple.Create(total, disList, sumCommission);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"fun:GetDistributionByGidandUidAsync,ex:{ex.Message}"));
            }
            return null;
        }

        public async Task<Dictionary<Guid, int>> GetDistributionByGidAsync(Guid gid, int sourcetype)
        {
            try
            {
                //var resList = context.Distributions.Where(b => b.gid == gid && b.sourcetype == sourcetype && b.isptsucceed == 1 && b.buyer != Guid.Empty).GroupBy(d => d.sharer).Select(r=>r.Key);
                var resList = await
                    (from d in context.Distributions
                     where d.gid.Equals(gid) && d.isptsucceed == 1 && d.sourcetype.Equals(sourcetype) && d.buyer != Guid.Empty
                     group d by d.sharer into w
                     select w)
                     .OrderByDescending(r => r.Count())
                     .ToDictionaryAsync(g => g.Key, g => g.Count());
                return resList;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return null;
        }

        public async Task<Tuple<int, List<Distribution>>> GetDistributionByUidAsync(Guid sharer, int pageIndex, int pageSize)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0)
                    return null;
                int from = (pageIndex - 1) * pageSize;
                var total = await (from d in context.Distributions
                                   where d.sharer.Equals(sharer) && d.isptsucceed == 1
                                   select d).CountAsync();
                var disList = await (from d in context.Distributions
                                     where d.sharer.Equals(sharer) && d.isptsucceed == 1
                                     orderby d.lastupdatetime descending
                                     select d).Skip(from).Take(pageSize).ToListAsync();
                return Tuple.Create(total, disList);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"fun:GetDistributionByUidAsync1,ex:{ex.Message}"));
            }
            return null;
        }
        public async Task<Tuple<int, List<Distribution>>> GetDistributionByUidAsync(Guid sharer, int pageIndex, int pageSize, int sourcetype)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0)
                    return null;
                int from = (pageIndex - 1) * pageSize;
                var total = await (from d in context.Distributions
                                   where d.sharer.Equals(sharer) && d.isptsucceed == 1 && d.sourcetype.Equals(sourcetype)
                                   select d).CountAsync();
                var disList = await (from d in context.Distributions
                                     where d.sharer.Equals(sharer) && d.isptsucceed == 1 && d.sourcetype.Equals(sourcetype)
                                     orderby d.lastupdatetime descending
                                     select d).Skip(from).Take(pageSize).ToListAsync();
                return Tuple.Create(total, disList);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"fun:GetDistributionByUidAsync2,ex:{ex.Message}"));
            }
            return null;
        }

        public async Task<bool> AddDistributionAsync(Distribution dis)
        {
            try
            {
                //新加入
                if (dis != null && !dis.oid.Equals(Guid.Empty))
                {
                    dis.createtime = CommonHelper.GetUnixTimeNow();
                    context.Distributions.Add(dis);
                    int ret = await context.SaveChangesAsync();
                    return ret == 1;
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"fun:AddDistributionAsync,ex:{ex.Message}"));
            }
            return false;
        }
        /// <summary>
        /// 拼团成功后修改为1
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="isptsucceed"></param>
        /// <returns></returns>
        public Distribution UpdateDisIsPTSucceed(Guid oid, int last_commission, int finally_commission)
        {
            try
            {
                var dis = GetDistributionByOid(oid);
                if (dis == null)
                    return null;
                dis.isptsucceed = 1;
                dis.last_commission = last_commission;
                dis.finally_commission = finally_commission;
                dis.lastupdatetime = CommonHelper.GetUnixTimeNow();
                if (context.SaveChanges() == 1)
                    return dis;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), new Exception($"fun:updateDistributionAsync,ex:{ex.Message}"));
            }
            return null;
        }

        #endregion

        #region User

        public List<User> UserGetAllRobots()
        {
            try
            {
                var list = (from u in context.Users where u.mid.Equals(RobotHelper.RobotMid) select u).ToList();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<User> UserGetByUidAsync(Guid uid)
        {
            try
            {
                var ret = await (from u in context.Users where u.uid.Equals(uid) select u).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<User> UserGetByOpenIdAsync(string openId)
        {
            try
            {
                var ret = await (from u in context.Users where u.openid.Equals(openId) select u).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }

        }

        public User UserGetByOpenId(string openId)
        {
            try
            {
                var ret = (from u in context.Users where u.openid.Equals(openId) select u).FirstOrDefault();
                return ret;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }

        }

        public async Task<bool> SaveOrUpdateUserAsnyc(User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user?.openid))
                    return false;

                //判断是否需要新加入，主要是openid是否已经存在
                var temp = await UserGetByOpenIdAsync(user.openid);

                //新加入
                if (user.uid.Equals(Guid.Empty) && temp == null) //新加入
                {
                    user.uid = Guid.NewGuid();
                    user.register_time = CommonHelper.GetUnixTimeNow();
                    Context.Users.Add(user);
                    var ret = await context.SaveChangesAsync();
                    await EsUserManager.AddOrUpdateAsync(EsUserManager.GenObject(user));

                    return true;
                }
                //update
                var uInDb = await UserGetByUidAsync(user.uid);
                if (uInDb == null)
                    return false;
                foreach (PropertyInfo pi in user.GetType().GetProperties())
                {
                    var v = pi.GetValue(user);
                    if (v != null)
                    {
                        pi.SetValue(uInDb, v);
                    }
                }
                await context.SaveChangesAsync();
                //更新es
                await EsUserManager.AddOrUpdateAsync(EsUserManager.GenObject(uInDb));
                return true;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }

        }

        public async Task<User> UpdateUserAgeAndSkinAsync(Guid uid, int age, int skincode)
        {
            try
            {
                var user = await UserGetByUidAsync(uid);
                if (user != null && !user.uid.Equals(Guid.Empty))
                {
                    user.age = age;
                    user.skin = skincode;
                    await context.SaveChangesAsync();
                    return user;
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), new Exception("fun:UpdateUserAgeAndSkin," + ex.Message));
            }
            return null;
        }
        public async Task<User> UpdateUserBackImgAsync(Guid uid, string backImg)
        {
            try
            {
                var user = await UserGetByUidAsync(uid);
                if (user != null && !user.uid.Equals(Guid.Empty))
                {
                    user.backimg = backImg;
                    await context.SaveChangesAsync();
                    return user;
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), new Exception("fun:UpdateUserBackImgAsync," + ex.Message));
            }
            return null;
        }

        public async Task<Dictionary<string, User>> UserGetByGuids(List<Guid> uids)
        {
            try
            {
                Dictionary<string, User> _temp = new Dictionary<string, User>();
                var ret = await (from u in context.Users where uids.Contains(u.uid) select u).ToListAsync();
                if (ret != null && ret.Count > 0)
                {
                    foreach (var v in ret)
                    {
                        _temp[v.uid.ToString()] = v;
                    }
                }
                return _temp;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        #endregion

        #region wxPay

        /// <summary>
        /// 只新加入。不更新。
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<bool> SaveWxCallBackAsync(WXPayResult result)
        {
            try
            {
                if (result.id.Equals(Guid.Empty))
                {
                    result.id = Guid.NewGuid();
                    context.WXPayResults.Add(result);
                    return await context.SaveChangesAsync() == 1;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }

        }

        /// <summary>
        /// 判断是否微信支付重发了某个支付回调消息。
        /// appid,otn,openid,transactionid,mch_id,result_code.6个条件相同，就是相同。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool WxPayCallBackIsExits(WXPayResult obj)
        {
            if (string.IsNullOrEmpty(obj?.appid) || string.IsNullOrEmpty(obj.out_trade_no) ||
                string.IsNullOrEmpty(obj.openid) || string.IsNullOrEmpty(obj.transaction_id) ||
                string.IsNullOrEmpty(obj.mch_id))
            {
                return false;
            }
            var ret = (from r in context.WXPayResults
                       where
                           r.out_trade_no.Equals(obj.out_trade_no) && r.result_code.Equals(obj.result_code) &&
                           r.appid.Equals(obj.appid) && r.mch_id.Equals(obj.mch_id) && r.openid.Equals(obj.openid) &&
                           r.transaction_id.Equals(obj.transaction_id)
                       select r).Count();
            return ret >= 1;
        }

        public bool SaveWxCallBack(WXPayResult result)
        {
            try
            {
                if (result.id.Equals(Guid.Empty))
                {
                    result.id = Guid.NewGuid();
                    context.WXPayResults.Add(result);
                    return context.SaveChanges() == 1;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }

        }

        public async Task<bool> SaveWxRefundCallBackAsync(WXRefund result)
        {
            try
            {
                if (result.id.Equals(Guid.Empty))
                {
                    result.id = Guid.NewGuid();
                }
                context.WXRefunds.Add(result);
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public List<WXRefund> GetWXRefundFail(List<string> otnList, List<string> err_codeList)
        {
            try
            {
                var list = (from j in context.WXRefunds
                            where j.result_code == "FAIL" && otnList.Contains(j.out_trade_no) && err_codeList.Contains(j.err_code)
                            select j).ToList();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), new Exception("方法名GetWXRefundFail错误:" + ex.Message));
            }
        }

        /// <summary>
        /// 如果已经退款成功过得订单不需要重新退款。result_code==SUCCESS的记录为0返回true。
        /// </summary>
        /// <param name="otn"></param>
        /// <returns></returns>
        public async Task<bool> RefundIsNeedAsnyc(string otn)
        {
            try
            {
                if (string.IsNullOrEmpty(otn))
                    return false;
                var count =
                    await
                        (from r in context.WXRefunds
                         where r.out_trade_no.Equals(otn) && r.result_code.Equals("SUCCESS")
                         select r).CountAsync();
                return count == 0;

            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> SaveWxPrepayAsync(WXPrePay prePay)
        {
            try
            {
                if (prePay.id.Equals(Guid.Empty))
                {
                    prePay.id = Guid.NewGuid();
                }
                context.WXPrePays.Add(prePay);
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public bool SaveWxPrepay(WXPrePay prePay)
        {
            try
            {
                if (prePay.id.Equals(Guid.Empty))
                {
                    prePay.id = Guid.NewGuid();
                }
                context.WXPrePays.Add(prePay);
                return context.SaveChanges() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<WXPayResult> WxPayResultGetByOutTradeNoAsync(string out_trade_no, string result_code)
        {
            try
            {
                var ret =
                    await
                        (from p in context.WXPayResults
                         where p.out_trade_no.Equals(out_trade_no) && p.result_code.Equals(result_code)
                         select p).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public WXRefund WXRefundByOtn(string otn)
        {
            try
            {
                var list = (from j in context.WXRefunds
                            where j.out_trade_no.Equals(otn)
                            orderby j.init_time descending
                            select j).FirstOrDefault();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        #endregion

        #region 核销与快递

        #region 核销点
        /// <summary>
        /// 获取一个mid下的所有有效的核销点
        /// </summary>
        /// <param name="mid"></param>
        /// <returns></returns>
        public async Task<List<WriteOffPoint>> GetWOPsByMidAsync(Guid mid)
        {
            try
            {
                var list = await (from w in context.VerifyPoints where w.mid.Equals(mid) && w.is_valid == true select w).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<WriteOffPoint> GetWOPByWoidAsync(Guid woid)
        {
            try
            {
                var ret =
                    await (from w in context.VerifyPoints where w.woid.Equals(woid) select w).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> AddWOPAsync(WriteOffPoint obj)
        {
            if (obj.woid.Equals(Guid.Empty))
            {
                obj.woid = Guid.NewGuid();
                obj.timestamp = CommonHelper.GetUnixTimeNow();
                obj.is_valid = true;
                Context.VerifyPoints.Add(obj);
                await Context.SaveChangesAsync();
                await EsWriteOffPointManager.AddOrUpdateAsync(EsWriteOffPointManager.GenObject(obj));
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateWOPAsync(WriteOffPoint obj)
        {
            try
            {
                if (obj.woid.Equals(Guid.Empty))
                    return false;
                var dbObject = await GetWOPByWoidAsync(obj.woid);
                if (dbObject == null)
                    return false;
                obj.timestamp = CommonHelper.GetUnixTimeNow();//重新获取当前时间，以便排序
                foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                {
                    var v = pi.GetValue(obj);
                    if (v != null)
                        pi.SetValue(dbObject, v);
                }

                //更新db
                await context.SaveChangesAsync();
                //更新es
                await EsWriteOffPointManager.AddOrUpdateAsync(EsWriteOffPointManager.GenObject(dbObject));

                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        #endregion
        #region 核销员

        public async Task<List<WriteOffer>> getAllWriteoffer()
        {
            try
            {
                var list =
                    await
                        (from woer in context.Verifiers
                         select woer).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 获取一个woid下所有有效的核销员
        /// </summary>
        /// <param name="woid"></param>
        /// <returns></returns>
        public async Task<List<WriteOffer>> GetWOerByWoidAsync(Guid woid)
        {
            try
            {
                var list =
                    await
                        (from woer in context.Verifiers
                         where woer.woid.Equals(woid) && woer.is_valid == true
                         select woer).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<Dictionary<Guid, int>> GetWOerByMidAsync(Guid mid)
        {
            try
            {
                var dic =
                    await
                        (from woer in context.Verifiers
                         where woer.mid.Equals(mid) && woer.is_valid == true
                         group woer by woer.woid into w
                         select w).ToDictionaryAsync(p => p.Key, p => p.Count());
                return dic;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<WriteOffer>> GetWOerByMid2Async(Guid mid)
        {
            try
            {
                var list =
                    await
                        (from woer in context.Verifiers
                         where woer.mid.Equals(mid)
                         select woer).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
                return new List<WriteOffer>();
            }
        }
        public async Task<List<WriteOffer>> GetWOerByMid2Async(Guid mid, bool is_valid)
        {
            try
            {
                var list =
                    await
                        (from woer in context.Verifiers
                         where woer.mid.Equals(mid) && woer.is_valid == is_valid
                         select woer).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
                return new List<WriteOffer>();
            }
        }

        public async Task<List<WriteOfferView>> GetWOerByWoid2Async(Guid woid)
        {
            try
            {
                var list =
                    await
                        (from woer in context.Verifiers
                         join u in context.Users on woer.uid equals u.uid
                         where woer.woid.Equals(woid) && woer.is_valid == true
                         select new WriteOfferView
                         {
                             id = woer.id,
                             uid = woer.uid,
                             woid = woer.woid,
                             realname = woer.realname,
                             phone = woer.phone,
                             nickName = u.name,
                             openid = woer.openid
                         }).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<WriteOfferView>> GetWOerviewByMid2Async(Guid mid)
        {
            try
            {
                var list =
                    await
                        (from woer in context.Verifiers
                         join u in context.Users on woer.uid equals u.uid
                         join w in context.VerifyPoints on woer.woid equals w.woid
                         where woer.mid.Equals(mid) && woer.is_valid == true
                         select new WriteOfferView
                         {
                             id = woer.id,
                             uid = woer.uid,
                             woid = woer.woid,
                             realname = woer.realname,
                             phone = woer.phone,
                             nickName = u.name,
                             openid = woer.openid,
                             woname = w.name
                         }).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<Tuple<int, List<WriteOfferView>>> GetWOercomByMidAsync(Guid mid, int pageIndex, int pageSize)
        {
            try
            {
                int count = await context.Verifiers.CountAsync(v => v.mid == mid && v.is_valid == true);
                int from = (pageIndex - 1) * pageSize;
                var list =
                    await
                        (from woer in context.Verifiers
                         join u in context.Users on woer.uid equals u.uid
                         join w in context.VerifyPoints on woer.woid equals w.woid
                         where woer.mid.Equals(mid) && woer.is_valid == true
                         orderby w.woid
                         select new WriteOfferView
                         {
                             id = woer.id,
                             uid = woer.uid,
                             woid = woer.woid,
                             realname = woer.realname,
                             phone = woer.phone,
                             nickName = u.name,
                             openid = woer.openid,
                             commission = woer.commission,
                             woname = w.name,
                             skin = u.skin,
                             age = u.age
                         }).Skip(from).Take(pageSize).ToListAsync();
                return Tuple.Create(count, list);
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
                return Tuple.Create(0, new List<WriteOfferView>());
            }
        }
        public async Task<int> GetWoerCountByWoidAsync(Guid woid)
        {
            return await context.Verifiers.CountAsync(w => w.woid == woid && w.is_valid == true);
        }
        public async Task<bool> AddWoerAsync(WriteOffer woer)
        {
            if (woer.id == null && !woer.uid.Equals(Guid.Empty) && !string.IsNullOrEmpty(woer.openid))
            {
                try
                {
                    woer.timestamp = CommonHelper.GetUnixTimeNow();
                    context.Verifiers.Add(woer);
                    if (await context.SaveChangesAsync() == 1)
                    {
                        var indexWriteoffer = EsWriteOfferManager.GenObject(woer);
                        await EsWriteOfferManager.AddOrUpdateAsync(indexWriteoffer);
                        return true;
                    }
                }
                catch (Exception ex)
                {

                    throw new MDException(typeof(BizRepository), ex);
                }
            }
            return false;
        }
        /// <summary>
        /// 一个核销员只能属于一个核销点
        /// </summary>
        /// <param name="woer"></param>
        /// <returns></returns>
        public async Task<bool> UpdateWoerAsync(WriteOffer woer)
        {
            try
            {
                if (woer.id != null && !woer.uid.Equals(Guid.Empty) && !string.IsNullOrEmpty(woer.openid))
                {
                    if (woer.woid.Equals(Guid.Empty))
                        return false;

                    var dbObject = await WoerGetByUidAsync(woer.uid);
                    if (dbObject == null)
                        return false;

                    foreach (PropertyInfo pi in dbObject.GetType().GetProperties())
                    {
                        var v = pi.GetValue(woer);
                        if (v != null)
                            pi.SetValue(dbObject, v);
                    }

                    //更新db
                    if (await context.SaveChangesAsync() == 1)
                    {
                        var indexWriteoffer = EsWriteOfferManager.GenObject(woer);
                        await EsWriteOfferManager.AddOrUpdateAsync(indexWriteoffer);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }

        }

        public async Task<bool> WoerDeleteById(int? id)
        {
            try
            {
                var ret = await (from woer in context.Verifiers where woer.id == id select woer).FirstOrDefaultAsync();
                if (ret != null)
                {
                    ret.is_valid = false;
                    await context.SaveChangesAsync();
                    var indexWriteoffer = EsWriteOfferManager.GenObject(ret);
                    await EsWriteOfferManager.AddOrUpdateAsync(indexWriteoffer);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<WriteOffer> WoerGetByOpenIdAsync(string openId)
        {
            try
            {
                var ret =
                    await
                        (from woer in context.Verifiers where woer.openid.Equals(openId) select woer)
                            .FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 一个uid只对应一个woer记录。一个人只能属于一个wop点。
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task<WriteOffer> WoerGetByUidAsync(Guid uid)
        {
            try
            {
                var ret =
                    await
                        (from woer in context.Verifiers where woer.uid.Equals(uid) select woer)
                            .FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 判断一个用户是否可以核销此商铺的产品
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task<bool> WoerCanWriteOff(Guid mid, Guid uid)
        {
            try
            {
                var count =
                    await
                        (from wo in context.Verifiers
                         where wo.mid.Equals(mid) && wo.uid.Equals(uid) && wo.is_valid == true
                         select wo)
                            .CountAsync();
                return count >= 1;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        /// <summary>
        /// 判断是否为核销员
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public WriteOffer WoerCanWriteOff_TB(Guid mid, Guid uid)
        {
            try
            {
                var woffer = (from wo in context.Verifiers
                              where wo.mid.Equals(mid) && wo.uid.Equals(uid) && wo.is_valid == true
                              select wo).FirstOrDefault();
                return woffer;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public WriteOffer addWriteOfferCommission(Guid uid, Guid mid, int commission)
        {
            try
            {
                var woffer = WoerCanWriteOff_TB(mid, uid);
                if (woffer != null)
                {
                    woffer.commission += commission;
                    var flag = context.SaveChanges() == 1;
                    if (flag)
                    {
                        var indexWriteoffer = EsWriteOfferManager.GenObject(woffer);
                        EsWriteOfferManager.AddOrUpdate(indexWriteoffer);
                        return woffer;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), new Exception($"fun:addWriteOfferComm,ex:{ex.Message}"));
            }
            return null;
        }

        public int GetWriteOfferCommission(Guid uid)
        {
            lock (_syncObject)
            {
                var writerOffer = context.Verifiers.FirstOrDefault(v => v.uid == uid && v.is_valid == true);
                if (writerOffer != null)
                {
                    return writerOffer.commission;
                }
                else
                {
                    return -200;
                }
            }
        }
        #endregion

        #region user_writeoff

        /// <summary>
        /// 通过uid与mid是否存在来更新记录。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<bool> UserWriteOffSaveOrUpdateAsnyc(User_WriteOff obj)
        {
            try
            {
                var dbObject = await UserWriteoffGetByMidAndUidAsync(obj.mid, obj.uid);

                //新加入
                if (dbObject == null || dbObject.uw_id.Equals(Guid.Empty))
                {
                    obj.uw_id = Guid.NewGuid();
                    obj.create_time = CommonHelper.GetUnixTimeNow();
                    obj.is_default = true;
                    if (obj.mid.Equals(Guid.Empty) || obj.uid.Equals(Guid.Empty) || obj.woid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.user_name) || string.IsNullOrEmpty(obj.cellphone))
                        return false;

                    context.UserWriteOff.Add(obj);
                    await context.SaveChangesAsync();
                    return true;
                }

                if (obj.mid.Equals(Guid.Empty) || obj.uid.Equals(Guid.Empty) || obj.woid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.user_name) || string.IsNullOrEmpty(obj.cellphone))
                    return false;

                //MDLogger.LogInfoAsync(typeof(BizRepository),$"uwid:{dbObject.uw_id},obj.uw:{obj.uw_id}");

                dbObject.mid = obj.mid;
                dbObject.cellphone = obj.cellphone;
                dbObject.woid = obj.woid;
                dbObject.is_default = obj.is_default;
                dbObject.uid = obj.uid;
                dbObject.user_name = obj.user_name;

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<User_WriteOff> UserWriteOffGetByIdAsync(Guid uwid)
        {
            try
            {
                var ret =
                    await (from uw in context.UserWriteOff where uw.uw_id.Equals(uwid) select uw).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<List<User_WriteOff>> UserWriteoffGetByMiAsync(Guid mid)
        {
            try
            {
                var ret =
                    await (from uw in context.UserWriteOff where uw.mid.Equals(mid) && uw.is_default.Value == true select uw).ToListAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        /// <summary>
        /// 一个用户在一个mid下暂时就只有一条记录。用于自动填写用户的提货信息。
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task<User_WriteOff> UserWriteoffGetByMidAndUidAsync(Guid mid, Guid uid)
        {
            try
            {
                var ret =
                    await (from uw in context.UserWriteOff where uw.mid.Equals(mid) && uw.uid.Equals(uid) && uw.is_default.Value == true select uw).FirstOrDefaultAsync();
                return ret;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }
        }

        #endregion
        #region user_post
        public async Task<UserPost> GetUserPostByUpidAsync(Guid upid)
        {
            var userpost = await (from p in context.UserPosts where p.upid.Equals(upid) select p).FirstOrDefaultAsync();
            return userpost;
        }
        public async Task<List<UserPost>> GetUserPostByUidAsync(Guid uid)
        {
            var list = await (from p in context.UserPosts
                              orderby p.is_default descending, p.createtime descending
                              where p.uid.Equals(uid)
                              select p).ToListAsync();
            return list;
        }
        public async Task<List<UserPost>> GetUserPostByUidAsync(Guid uid, bool isdelete)
        {
            var list = await (from p in context.UserPosts
                              orderby p.is_default descending, p.createtime descending
                              where p.uid.Equals(uid) && p.isdelete.Equals(isdelete)
                              select p).ToListAsync();
            return list;
        }
        public async Task<UserPost> GetUserPostDefaultByUidAsync(Guid uid)
        {
            var up = await (from p in context.UserPosts
                            orderby p.is_default descending, p.createtime descending
                            where p.uid.Equals(uid)
                            select p).FirstOrDefaultAsync();
            return up;
        }
        private async Task<bool> UserPostCleanIs_DefaultAsync(Guid uid)
        {
            var up = await (from p in context.UserPosts where p.uid.Equals(uid) && p.is_default == true select p).FirstOrDefaultAsync();
            if (up != null)
            {
                up.is_default = false;
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<bool> UpdateUserPostIs_DefaultAsync(Guid uid, Guid upid)
        {
            await UserPostCleanIs_DefaultAsync(uid);//先清空上一个默认地址
            var up = await (from p in context.UserPosts where p.upid.Equals(upid) select p).FirstOrDefaultAsync();
            if (up != null)
            {
                up.is_default = true;
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<bool> AddOrUpdateUserPostAsync(UserPost obj)
        {
            try
            {
                if (obj == null || obj.upid.Equals(Guid.Empty) || obj.uid.Equals(Guid.Empty) || string.IsNullOrEmpty(obj.code))
                    return false;
                if (obj.is_default)//如果新增或修改的是默认地址，则把原来的默认地址清空
                {
                    await UserPostCleanIs_DefaultAsync(obj.uid);
                }
                var userpost = await GetUserPostByUpidAsync(obj.upid);
                if (userpost == null || userpost.upid.Equals(Guid.Empty))
                {
                    context.UserPosts.Add(obj);
                    await context.SaveChangesAsync();
                    return true;
                }
                else
                {
                    foreach (PropertyInfo pi in userpost.GetType().GetProperties())
                    {
                        var v = pi.GetValue(obj);
                        if (v != null)
                        {
                            pi.SetValue(userpost, v);
                        }
                    }
                    await context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        public async Task<bool> DelUserPostAsync(Guid upid)
        {
            try
            {
                var up = await GetUserPostByUpidAsync(upid);
                if (up == null || up.upid.Equals(Guid.Empty))
                    return false;
                up.isdelete = true;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        #endregion
        #endregion

        #region vector

        public async Task<bool> VectorAddAsnyc(Vector v)
        {
            try
            {
                if (v.vid.Equals(Guid.Empty))
                {
                    v.vid = Guid.NewGuid();
                }
                context.Vectors.Add(v);
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        #endregion

        #region sta_user
        public async Task<sta_user> sta_userGetAsync(string loginname, string pwd)
        {
            try
            {
                var user = await context.sta_users.Where(p => p.loginname.Equals(loginname) && p.pwd.Equals(pwd)).FirstOrDefaultAsync();
                return user;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }

        }
        public async Task<bool> addOrUpdapesta_userAsync(sta_user obj)
        {
            try
            {
                var stauser = await context.sta_users.Where(p => p.loginname.Equals(obj.loginname)).FirstOrDefaultAsync();
                if (stauser == null || stauser.uid.Equals(Guid.Empty))
                {
                    //新加入
                    sta_user newuser = new sta_user
                    {
                        uid = Guid.NewGuid(),
                        loginname = obj.loginname,
                        pwd = obj.pwd,
                        nickname = obj.nickname,
                        mid = Guid.NewGuid(),
                        tel = obj.tel,
                        register_date = CommonHelper.GetUnixTimeNow(),
                        is_valid = 1
                    };
                    context.sta_users.Add(newuser);
                    return await context.SaveChangesAsync() == 1;
                }
                stauser.nickname = obj.nickname;
                stauser.pwd = obj.pwd;
                stauser.tel = obj.tel;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        #endregion

        #region subscribe_user
        public async Task<bool> AddSub_userAsync(Subscribe_User obj)
        {
            try
            {
                var stauser = await context.Subscribe_User.Where(p => p.openid.Equals(obj.openid)).FirstOrDefaultAsync();
                if (stauser == null)
                {
                    context.Subscribe_User.Add(obj);
                    return await context.SaveChangesAsync() == 1;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<bool> BatchAddSub_userAsync(List<Subscribe_User> list)
        {
            try
            {
                foreach (var obj in list)
                {
                    var stauser = await context.Subscribe_User.Where(p => p.openid.Equals(obj.openid)).FirstOrDefaultAsync();
                    if (stauser == null)
                    {
                        context.Subscribe_User.Add(obj);
                    }
                }
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }
        #endregion

        #region Supply
        public async Task<bool> SaveOrUpdateSupplyAsync(Supply sup)
        {
            Supply supply = await GetSupplyBySidAsync(sup.sid);
            bool isNew = false;
            try
            {
                //新加入
                if (supply == null || supply.sid.Equals(Guid.Empty))
                {
                    //sup也是新的
                    if (sup.sid.Equals(Guid.Empty))
                        supply.sid = Guid.NewGuid();
                    supply.timestamp = CommonHelper.GetUnixTimeNow();
                    supply.s_no = (int)supply.timestamp;
                    supply.status = (int)ESupplyStatus.已上线;
                    isNew = true;
                }

                foreach (var p in supply.GetType().GetProperties())
                {
                    //更新属性
                    var v = sup.GetType().GetProperty(p.Name).GetValue(sup);
                    if (v != null)
                    {
                        //其他字段更新
                        p.SetValue(supply, v);
                    }
                }
                if (isNew)
                    context.Supplys.Add(supply);
                supply.timestamp = CommonHelper.GetUnixTimeNow();
                int ret = await context.SaveChangesAsync();
                return ret == 1;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return false;
        }

        public async Task<bool> UpdateSupplystatusAsync(Guid sid, int status)
        {
            try
            {
                var supply = await (from s in context.Supplys where s.sid.Equals(sid) select s).FirstOrDefaultAsync();
                if (supply == null)
                    return false;
                supply.status = status;
                return await context.SaveChangesAsync() == 1;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(BizRepository), ex);
            }
        }

        public async Task<Supply> GetSupplyBySidAsync(Guid Sid)
        {
            try
            {
                var list = await (from s in context.Supplys where s.sid.Equals(Sid) select s).FirstOrDefaultAsync();
                return list ?? new Supply();
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return new Supply();
        }

        public async Task<List<Supply>> GetSupplyBySidAsync(List<Guid> sidList)
        {
            try
            {
                var list = await (from s in context.Supplys
                                  where sidList.Contains(s.sid)
                                  orderby s.timestamp descending
                                  select s).ToListAsync();
                return list;
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(BizRepository), ex);
            }
            return new List<Supply>();
        }
        #endregion

        #endregion

        public async Task<bool> Save()
        {
            try
            {
                return await context.SaveChangesAsync() >= 1;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(BizRepository), ex);
            }

        }
    }

    public enum EMorderResponse
    {
        Overflow = 0,//超出套餐的限额
        OK = 1,
        Error = 2,
    }
}
