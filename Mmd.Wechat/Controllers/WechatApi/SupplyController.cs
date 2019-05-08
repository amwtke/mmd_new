using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB.Code;
using MD.Model.Index.MD;
using MD.Wechat.Controllers.WechatApi.Parameters.biz.Supply;
using MD.WeChat.Filters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MD.Wechat.Controllers.WechatApi
{
    [RoutePrefix("api/supply")]
    [AccessFilter]
    public class SupplyController : ApiController
    {
        private List<Codebrand> ProductBrandList =new List<Codebrand>();
        private List<CodeProductCategory> ProductCategoryList = new List<CodeProductCategory>();
        public SupplyController()
        {
            //初始化品牌、分类字典
            using (var coderepo = new CodeRepository())
            {
                ProductBrandList =  coderepo.GetAllProductBrand2();
                ProductCategoryList = coderepo.GetAllProductCategory();
            }
        }
        static List<object> retobjNew = new List<object>();
        // GET api/<controller>
        [HttpPost]
        [Route("getsupplys")]
        public async Task<HttpResponseMessage> GetSupplys(SupplyParameter parameter)
        {
            if (parameter == null || parameter.pageIndex <= 0 || parameter.QueryStr == null)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!pageIndex:{parameter.pageIndex},QueryStr:{parameter.QueryStr}", HttpStatusCode.OK, ECustomStatus.Fail);
            int pageSize = MdWxSettingUpHelper.GetPageSize();
            var Tuple = await EsSupplyManager.SearchAsnyc2(parameter.QueryStr, parameter.pageIndex, pageSize, new List<int> { (int)ESupplyStatus.已上线 }, parameter.category);
            int totalPage = MdWxSettingUpHelper.GetTotalPages(Tuple.Item1);
            var retobj = new List<object>();
            List<IndexSupply> supplyList = Tuple.Item2;
            int i = 0;
            if (parameter.pageIndex == 1)
                retobjNew.Clear();
            foreach (var supply in supplyList)
            {
                if (i < 2 && parameter.pageIndex == 1)
                {
                    retobjNew.Add(new
                    {
                        sid = supply.Id,
                        supply.advertise_pic_1,
                        supply.advertise_pic_2,
                        supply.advertise_pic_3,
                        brand = ProductBrandList.Where(p=>p.code.Equals(supply.brand)).FirstOrDefault()?.value,
                        category = ProductCategoryList.Where(p=>p.code.Equals(supply.category)).FirstOrDefault()?.value,
                        group_price = supply.group_price / 100.00,
                        market_price = supply.market_price / 100.00,
                        supply_price = supply.supply_price / 100.00,
                        supply.name,
                        supply.pack,
                        supply.quota_max,
                        supply.quota_min,
                        supply.standard
                    });
                    i++;
                }
                else
                {
                    retobj.Add(new
                    {
                        sid = supply.Id,
                        supply.advertise_pic_1,
                        supply.advertise_pic_2,
                        supply.advertise_pic_3,
                        brand = ProductBrandList.Where(p => p.code.Equals(supply.brand)).FirstOrDefault()?.value,
                        category = ProductCategoryList.Where(p => p.code.Equals(supply.category)).FirstOrDefault()?.value,
                        group_price = supply.group_price / 100.00,
                        market_price = supply.market_price / 100.00,
                        supply_price = supply.supply_price / 100.00,
                        supply.name,
                        supply.pack,
                        supply.quota_max,
                        supply.quota_min,
                        supply.standard
                    });
                }
            }
            return JsonResponseHelper.HttpRMtoJson(new { totalPage = totalPage, gNewlist = retobjNew, glist = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
        }
        [HttpPost]
        [Route("getdetail")]
        public async Task<HttpResponseMessage> GetDetail(SupplyParameter parameter)
        {
            if (parameter == null || parameter.sid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!sid:{parameter.sid}", HttpStatusCode.OK, ECustomStatus.Fail);
            var supply = await EsSupplyManager.GetByGidAsync(parameter.sid);
            if (supply == null)
                return JsonResponseHelper.HttpRMtoJson($"supply is null!sid:{parameter.sid}", HttpStatusCode.OK, ECustomStatus.Fail);
            supply.description = HttpUtility.HtmlDecode(supply.description).Replace("\"", "\'");
            string fxUrl = MdWxSettingUpHelper.GenSupplyDetailUrl(parameter.sid);
            var retobj = new
            {
                supply.advertise_pic_1,
                supply.advertise_pic_2,
                supply.advertise_pic_3,
                brand = ProductBrandList.Where(p => p.code.Equals(supply.brand)).FirstOrDefault()?.value,
                category = ProductCategoryList.Where(p => p.code.Equals(supply.category)).FirstOrDefault()?.value,
                supply.description,
                group_price = supply.group_price / 100.00,
                market_price = supply.market_price / 100.00,
                supply_price = supply.supply_price / 100.00,
                supply.name,
                supply.pack,
                supply.quota_max,
                supply.quota_min,
                supply.standard,
                fxUrl
            };
            return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
        }
    }
}