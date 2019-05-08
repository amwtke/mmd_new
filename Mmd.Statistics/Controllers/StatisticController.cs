using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using Mmd.Statistics.Controllers.Parameters;
using Mmd.Statistics.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Mmd.Statistics.Controllers
{
    [AccessFilter]
    [RoutePrefix("api/sta")]
    public class StatisticController : ApiController
    {
        [HttpPost]
        [Route("sta/writeoff")]
        public async Task<HttpResponseMessage> GetWriteOffPointSta(BaseParameter param)
        {
            if (param == null || param.pageIndex <= 0 || param.pageSize <= 0 || param.from == null || param.to == null)
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            List<WriteOffPointModel> listRes = new List<WriteOffPointModel>();
            double timeStart = CommonHelper.ToUnixTime(param.from);
            double timeEnd = CommonHelper.ToUnixTime(param.to.AddDays(1));
            //int pageSize = MdWxSettingUpHelper.GetPageSize();
            using (var repo = new BizRepository())
            {
                var tuple = await repo.MerchantSearchByNameAsync(param.queryStr, param.pageIndex, param.pageSize, (int)ECodeMerchantStatus.已配置, "");
                if (tuple.Item1 > 0 && tuple.Item2 != null && tuple.Item2.Count > 0)
                {
                    List<Merchant> list = tuple.Item2.ToList();
                    foreach (Merchant m in list)
                    {
                        //成交订单
                        var tupleOrder = await EsOrderManager.GetOrderCountAndAmountAsync(m.mid, new List<int>() { (int)EOrderStatus.已成团未提货, (int)EOrderStatus.拼团成功 }, timeStart, timeEnd);
                        //浏览量
                        var tupleView = EsBizLogStatistics.SearchBizView(ELogBizModuleType.MidView, m.mid, Guid.Empty, timeStart, timeEnd, 1, 1);
                        //团活动
                        var tupleGroup2 = await EsGroupManager.GetByMidAsync(m.mid, new List<int>() { (int)EGroupStatus.已发布, (int)EGroupStatus.已结束, (int)EGroupStatus.已过期 }, timeStart, timeEnd);
                        List<string> listGroupGid = tupleGroup2.Item2.ToList();
                        WriteOffPointModel obj = new WriteOffPointModel()
                        {
                            mid = m.mid.ToString(),
                            name = m.name,
                            pointCount = (int)await EsWriteOffPointManager.GetCountByMidAsync(m.mid),
                            regDate = CommonHelper.FromUnixTime((double)m.register_date).ToString("yyyy-MM-dd HH:mm"),
                            productCount = await EsProductManager.GetCountByMidAsync(m.mid, timeStart, timeEnd),
                            groupCountAll = (int)tupleGroup2.Item1,
                            //groupCountK = (int)await EsGroupManager.GetCountByMidAsync(m.mid, new List<int>() { (int)EGroupOrderStatus.拼团成功, (int)EGroupOrderStatus.拼团失败, (int)EGroupOrderStatus.拼团进行中 }, timeStart, timeEnd),
                            //groupCountS = (int)await EsGroupManager.GetCountByMidAsync(m.mid, new List<int>() { (int)EGroupOrderStatus.拼团成功 }, timeStart, timeEnd),
                            groupCountK = listGroupGid.Count > 0 ? (int)await EsGroupOrderManager.GetCountByGidsAsync(listGroupGid, new List<int>() { (int)EGroupOrderStatus.拼团成功, (int)EGroupOrderStatus.拼团失败, (int)EGroupOrderStatus.拼团进行中 }, timeStart, timeEnd) : 0,
                            groupCountS = listGroupGid.Count > 0 ? (int)await EsGroupOrderManager.GetCountByGidsAsync(listGroupGid, new List<int>() { (int)EGroupOrderStatus.拼团成功 }, timeStart, timeEnd) : 0,
                            orderCount = tupleOrder.Item1,
                            orderAmount = tupleOrder.Item2,
                            orderH = (int)await EsOrderManager.GetOrderCountAsync(m.mid, new List<int>() { (int)EOrderStatus.拼团成功 }, timeStart, timeEnd),
                            viewCount = tupleView.Item1
                        };
                        listRes.Add(obj);
                    }
                }
                int totalCount = tuple.Item1;
                //double totalPage = MdWxSettingUpHelper.GetTotalPages(totalCount);
                double totalPage = Math.Ceiling(totalCount / 1.00 / param.pageSize);
                return JsonResponseHelper.HttpRMtoJson(new { totalCount = totalCount, totalPage = totalPage, glist = listRes }, HttpStatusCode.OK, ECustomStatus.Success);
            }

        }

        [HttpPost]
        [Route("activitydata")]
        public async Task<HttpResponseMessage> getActivityData(BaseParameter parameter)
        {
            if(parameter==null||parameter.pageIndex<=0||parameter.pageSize<=0)
                return JsonResponseHelper.HttpRMtoJson("parameter is error!", HttpStatusCode.OK, ECustomStatus.Fail);
            //using (var sta=new StaRepository())
            //{
            //    double f = CommonHelper.ToUnixTime(parameter.from);
            //    double t = CommonHelper.ToUnixTime(parameter.to.AddDays(1));
            //    var obj = sta.getActivityData(parameter.queryStr, f, t, parameter.pageIndex, parameter.pageSize);
            //    return JsonResponseHelper.HttpRMtoJson(obj, HttpStatusCode.OK, ECustomStatus.Success);
            //}

            #region 老代码
            using (var reop = new BizRepository())
            {
                double f = CommonHelper.ToUnixTime(parameter.from);
                double t = CommonHelper.ToUnixTime(parameter.to.AddDays(1));
                List<object> listRes = new List<object>();
                //获取查询条件下的所有商家MidList
                if (string.IsNullOrEmpty(parameter.queryStr))
                    parameter.queryStr = "";
                var midList = await reop.MerchantSearchByNameAsync(parameter.queryStr);
                if (midList == null || midList.Count == 0)
                    return JsonResponseHelper.HttpRMtoJson("暂无数据", HttpStatusCode.OK, ECustomStatus.Fail);
                //根据MidList再获取这些商家的团信息
                List<int> status = new List<int>();
                var tuple = await EsGroupManager.getGroupsAsync(midList, status, parameter.pageIndex, parameter.pageSize, f, t);
                if (tuple.Item1 > 0 && tuple.Item2 != null && tuple.Item2.Count > 0)
                {
                    int totalPage = 0;
                    if (tuple.Item1 % parameter.pageSize == 0)
                        totalPage = tuple.Item1 / parameter.pageSize;
                    else
                        totalPage = (tuple.Item1 / parameter.pageSize) + 1;
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
                        int cjdd= await EsOrderManager.GetByGidCountAsync("", new List<int> { (int)EOrderStatus.已成团未提货, (int)EOrderStatus.拼团成功 }, Guid.Parse(group.Id));
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
                            cjdd ,//成交订单
                            yhx ,//已核销订单
                            cjje ,//成交金额
                            lll = lllObj.Item1//浏览量
                        };
                        listRes.Add(ret);
                    }
                    return JsonResponseHelper.HttpRMtoJson(new {total= tuple.Item1, totalPage = totalPage, glist = listRes }, HttpStatusCode.OK, ECustomStatus.Success);
                }
                return JsonResponseHelper.HttpRMtoJson("暂无数据", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            #endregion
        }

        [HttpPost]
        [Route("writeoff")]
        public HttpResponseMessage GetWriteOffPointStaNew(BaseParameter param)
        {

            if (param == null || param.pageIndex <= 0 || param.pageSize <= 0 || param.from == null || param.to == null)
            {
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            List<WriteOffPointModel> listRes = new List<WriteOffPointModel>();
            int totalCount = 0;
            double timeStart = CommonHelper.ToUnixTime(param.from);
            double timeEnd = CommonHelper.ToUnixTime(param.to.AddDays(1));
            string orderBy = "regDate";
            if (!string.IsNullOrEmpty(param.orderBy)) orderBy = param.orderBy;
            using (StaRepository rpo = new StaRepository())
            {
                DataTable dt = rpo.GetMerchantData(param.queryStr, timeStart, timeEnd, orderBy, param.isAsc, param.pageIndex, param.pageSize);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];
                    //浏览量
                    var tupleView = EsBizLogStatistics.SearchBizView(ELogBizModuleType.MidView, Guid.Parse(dr["mid"].ToString()), Guid.Empty, timeStart, timeEnd, 1, 1);
                    WriteOffPointModel obj = new WriteOffPointModel()
                    {
                        mid = dr["mid"].ToString(),
                        name = dr["name"].ToString(),
                        pointCount = Convert.ToInt32(dr["pointCount"].ToString()),
                        regDate = dr["regDate"].ToString(),
                        productCount = Convert.ToInt32(dr["productCount"].ToString()),
                        groupCountAll = Convert.ToInt32(dr["groupCountAll"].ToString()),
                        groupCountK = Convert.ToInt32(dr["groupCountK"].ToString()),
                        groupCountS = Convert.ToInt32(dr["groupCountS"].ToString()),
                        orderCount = Convert.ToInt32(dr["orderCount"].ToString()),
                        orderAmount = Convert.ToDecimal(Convert.ToInt32(dr["orderAmount"].ToString()) / 100.00),
                        orderH = Convert.ToInt32(dr["orderCountH"].ToString()),
                        viewCount = tupleView.Item1
                    };
                    listRes.Add(obj);
                }
                totalCount = rpo.GetMerchantCount(param.queryStr);
            }
            double totalPage = Math.Ceiling(totalCount / 1.00 / param.pageSize);
            return JsonResponseHelper.HttpRMtoJson(new { totalCount = totalCount, totalPage = totalPage, glist = listRes }, HttpStatusCode.OK, ECustomStatus.Success);
        }
    }
    public class WriteOffPointModel
    {
        public string mid { get; set; }
        public string name { get; set; }
        public int pointCount { get; set; }
        public string regDate { get; set; }
        public int productCount { get; set; }
        public int groupCountAll { get; set; }
        public int groupCountK { get; set; }
        public int groupCountS { get; set; }
        public int orderCount { get; set; }
        public decimal orderAmount { get; set; }
        public int orderH { get; set; }
        public int viewCount { get; set; }
    }
}
