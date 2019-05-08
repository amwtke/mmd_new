using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Util.Data;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Statistics.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 活动数据导出
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="queryStr"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<ActionResult> ExportCsv(int pageIndex, int pageSize, string queryStr, DateTime from, DateTime to)
        {
            string fileName = "活动数据.csv";
            string csv = await genCsvByStatus(pageIndex, pageSize, queryStr, from, to);
            if (!string.IsNullOrEmpty(csv))
            {
                byte[] bs = Encoding.GetEncoding("gb2312").GetBytes(csv);
                Stream st = new MemoryStream(bs);
                return File(st, "text/csv", fileName);
            }
            return Content("无数据！");
        }

        /// <summary>
        /// 门店数据导出
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="queryStr"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public async Task<ActionResult> ExportCsv2(int pageIndex, int pageSize, string queryStr, DateTime from, DateTime to,string orderBy)
        {
            string fileName = "门店数据.csv";
            double timeStart = CommonHelper.ToUnixTime(from);
            double timeEnd = CommonHelper.ToUnixTime(to.AddDays(1));
            //string order = "groupCountK";
            List<WriteOffPointModel> listRes = new List<WriteOffPointModel>();
            string csvString = string.Empty;
            DataTable dtExport = new DataTable();
            dtExport.Columns.Add("商家名称");
            dtExport.Columns.Add("门店数");
            dtExport.Columns.Add("开通时间");
            dtExport.Columns.Add("添加商品数");
            dtExport.Columns.Add("发布活动数");
            dtExport.Columns.Add("开团数");
            dtExport.Columns.Add("成团数");
            dtExport.Columns.Add("成交订单");
            dtExport.Columns.Add("成交金额");
            dtExport.Columns.Add("已核销");
            dtExport.Columns.Add("浏览量"); 
            dtExport.Columns.Add("转化率"); 
            using (StaRepository rpo = new StaRepository())
            {
                DataTable dt = rpo.GetMerchantData(queryStr, timeStart, timeEnd, orderBy, false, pageIndex, pageSize);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];
                    //浏览量
                    var tupleView = EsBizLogStatistics.SearchBizView(ELogBizModuleType.MidView, Guid.Parse(dr["mid"].ToString()), Guid.Empty, timeStart, timeEnd, 1, 1);
                    int viewCount = tupleView.Item1;
                    double per = viewCount == 0 ? 0: Convert.ToInt32(dr["orderCount"])/1.00 / viewCount;
                    List<object> arr = dr.ItemArray.ToList();
                    arr.Add(viewCount);
                    arr.Add((per*100).ToString("0.00") + "%");
                    arr.RemoveAt(0);
                    dtExport.Rows.Add(arr.ToArray());
                }
                var csv = new CSVHelper(dtExport);
                csvString = csv.ExportCSV();
            }
            if (!string.IsNullOrEmpty(csvString))
            {
                byte[] bs = Encoding.GetEncoding("gb2312").GetBytes(csvString);
                Stream st = new MemoryStream(bs);
                return File(st, "text/csv", fileName);
            }
            return Content("无数据！");
        }

        private async Task<string> genCsvByStatus(int pageIndex,int pageSize,string queryStr, DateTime from,DateTime to)
        {
            DataTable dtExport = new DataTable();
            dtExport.Columns.Add("商家名称");
            dtExport.Columns.Add("商品名称");
            dtExport.Columns.Add("发布时间");
            dtExport.Columns.Add("库存");
            dtExport.Columns.Add("拼团价格");
            dtExport.Columns.Add("成团人数");
            dtExport.Columns.Add("自动成团");
            dtExport.Columns.Add("开团数");
            dtExport.Columns.Add("成团数");
            dtExport.Columns.Add("成交订单");
            dtExport.Columns.Add("成交金额");
            dtExport.Columns.Add("已核销");
            dtExport.Columns.Add("浏览量");
            dtExport.Columns.Add("转化率");
            #region 老代码
            using (var reop = new BizRepository())
            {
                double f = CommonHelper.ToUnixTime(from);
                double t = CommonHelper.ToUnixTime(to.AddDays(1));
                
                //获取查询条件下的所有商家MidList
                if (string.IsNullOrEmpty(queryStr))
                    queryStr = "";
                var midList = await reop.MerchantSearchByNameAsync(queryStr);
                if (midList == null || midList.Count == 0)
                    return "";
                //根据MidList再获取这些商家的团信息
                List<int> status = new List<int>();
                var tuple = await EsGroupManager.getGroupsAsync(midList, status, pageIndex, pageSize, f, t);
                if (tuple.Item1 > 0 && tuple.Item2 != null && tuple.Item2.Count > 0)
                {
                    
                    foreach (var group in tuple.Item2)
                    {
                        //根据团信息再获取商家
                        var mer = await reop.GetMerchantByMidAsync(Guid.Parse(group.mid));
                        //根据pid获取商品信息
                        var product = await EsProductManager.GetByPidAsync(Guid.Parse(group.pid));
                        //是否使用机器人
                        string userobot = await AttHelper.GetValueAsync(Guid.Parse(group.Id), EAttTables.Group.ToString(), EGroupAtt.userobot.ToString());
                        //某个团的浏览量
                        var lllObj = await EsBizLogStatistics.SearchBizViewAsnyc(ELogBizModuleType.GidView, Guid.Parse(group.Id), Guid.Empty);
                        //开团数
                        int kts = await EsGroupOrderManager.GetByGidCountAsync(new List<int>() { }, Guid.Parse(group.Id));
                        //成团数
                        int cts = await EsGroupOrderManager.GetByGidCountAsync(new List<int> { (int)EGroupOrderStatus.拼团成功 }, Guid.Parse(group.Id));
                        //成交订单
                        int cjdd = await EsOrderManager.GetByGidCountAsync("", new List<int> { (int)EOrderStatus.已成团未提货, (int)EOrderStatus.拼团成功 }, Guid.Parse(group.Id));
                        //已核销
                        int yhx = await EsOrderManager.GetByGidCountAsync("", new List<int> { (int)EOrderStatus.拼团成功 }, Guid.Parse(group.Id));
                        //成交金额
                        decimal cjje = await EsOrderManager.GetAmountAsync(group.Id, new List<int> { (int)EOrderStatus.已成团未提货, (int)EOrderStatus.拼团成功 });
                        var ret = new
                        {
                            Gid = group.Id,
                            grouptitle = group.title,
                            merchant_name = mer.name,
                            product_name = product.name,
                            last_update_time = CommonHelper.FromUnixTime(group.last_update_time).ToString(),
                            product_setting_count = group.product_quota,//设置库存数（会变）
                            group_price = (float)group.group_price / 100,
                            ctrs = group.person_quota,//成团人数（几人团的意思）
                            userobot = userobot == "1" ? "是" : "否",//自动成团
                            kts,//开团数
                            cts,//成团数
                            cjdd,//成交订单
                            yhx,//已核销订单
                            cjje,//成交金额
                            lll = lllObj.Item1//浏览量
                        };
                        double per = lllObj.Item1 == 0 ? 0 : cjdd / 1.00 / lllObj.Item1;
                        dtExport.Rows.Add(ret.merchant_name, ret.product_name, ret.last_update_time, ret.product_setting_count, ret.group_price, ret.ctrs, ret.userobot, ret.kts, ret.cts, ret.cjdd, ret.cjje, ret.yhx, ret.lll, (per * 100).ToString("0.00") + "%");
                    }
                    
                }
                
            }
            #endregion
            
            string csvString = null;
            var csv = new CSVHelper(dtExport);
            csvString = csv.ExportCSV();
            return csvString;
        }
    }
}