using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Wechat.Controllers.WechatApi.Parameters;
using MD.WeChat.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MD.Wechat.Controllers.WechatApi
{

    [RoutePrefix("api/logisticsregion")]
    [AccessFilter]
    public class LogisticsRegionController : ApiController
    {
        private static List<Province> retobj = new List<Province>();

        [HttpPost]
        [Route("getall")]
        public async Task<HttpResponseMessage> GetAllRegionList(BaseParameter parameter)
        {
            if (retobj.Count > 0)
                return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
            var list = await EsLogisticsregionManager.GetAllAsync();
            if (list != null && list.Count > 0)
            {
                var provinceList = list.Where(p => p.fatherId == 0);
                foreach (var province in provinceList)//省
                {
                    Province pro = new Province();
                    pro.name = province.name;
                    pro.code = province.code;
                    pro.cityList = new List<City>();
                    retobj.Add(pro);
                    var cityList = list.Where(p => p.fatherId == province.Id);
                    foreach (var city in cityList)//市
                    {
                        City ci = new City();
                        ci.name = city.name;
                        ci.code = city.code;
                        ci.districtList = new List<District>();
                        pro.cityList.Add(ci);
                        var districtList = list.Where(p => p.fatherId == city.Id);
                        foreach (var district in districtList)//区
                        {
                            District di = new District();
                            di.name = district.name;
                            di.code = district.code;
                            ci.districtList.Add(di);
                        }
                    }
                }
                return JsonResponseHelper.HttpRMtoJson(retobj, HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson(null, HttpStatusCode.OK, ECustomStatus.Success);
        }
        private class Province
        {
            public string name { get; set; }
            public string code { get; set; }
            public List<City> cityList { get; set; }
        }
        private class City
        {
            public string name { get; set; }
            public string code { get; set; }
            public List<District> districtList { get; set; }
        }
        private class District
        {
            public string name { get; set; }
            public string code { get; set; }
        }
    }
}