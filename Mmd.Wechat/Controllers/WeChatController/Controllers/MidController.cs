using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Weixin.MmdBiz;
using MD.Model.DB.Code;

namespace MD.Wechat.Controllers.WX.Controllers
{
    public class MidController : Controller
    {
        // GET: Mid
        public async Task<ActionResult> Index(Guid mid)
        {
            using (var repo = new BizRepository())
            {
                if (mid.Equals(Guid.Empty))
                    return Content("mid is null!");
                var mer = await repo.GetMerchantByMidAsync(mid);
                if (mer == null)
                    return Content("null");

                ViewBag.mid = mer.mid.ToString();
                ViewBag.mName = mer.name;

                return View();
            }
        }

        public async Task<ActionResult> GetMidFwCount(Guid mid,string days)
        {
            if (Guid.Empty.Equals(mid) || string.IsNullOrEmpty(days))
                return Content("错误了！");
            int ds;
            if (int.TryParse(days, out ds))
            {
                if (ds > 0)
                {
                    double delta = ds*24*60*60;
                    double f = CommonHelper.GetUnixTimeNow() - delta;
                    var ret =
                        await
                            EsBizLogStatistics.SearchBizViewAsnyc(ELogBizModuleType.MidView, mid, Guid.Empty, f,
                                CommonHelper.GetUnixTimeNow());
                    return Content($"{ret.Item1}次");
                }
            }
            return Content("days输入为正整数！");
        }

        public PartialViewResult GroupGetPartial(Guid mid)
        {
            if (mid.Equals(Guid.Empty))
                return new PartialViewResult();

            var merRedis = RedisMerchantOp.GetByMid(mid);

            var ret = EsGroupManager.GetByMid(mid, new List<int>() {(int) EGroupStatus.已发布}, 1, 100);
            if (ret.Item1 > 0)
            {
                var retList = new List<PartialRowClass>();

                foreach (var r in ret.Item2)
                {
                    var temp = new PartialRowClass();
                    //团长优惠
                    var leader_price = AttHelper.GetValue(Guid.Parse(r.Id), EAttTables.Group.ToString(),
                        EGroupAtt.leader_price.ToString());
                    temp.TuanYou = leader_price;

                    //团标题
                    temp.GroupName = r.title;

                    temp.gid = r.Id;
                    temp.GroupPersonQouta = r.person_quota.ToString();
                    temp.Price = ((decimal) r.group_price/100).ToString();
                    temp.KuCun = r.product_quota.ToString();
                    
                    //总点击量
                    double f = CommonHelper.GetUnixTimeNow() - 100*24*60*60;

                    var djl = EsBizLogStatistics.SearchBizView(ELogBizModuleType.GidView, Guid.Parse(r.Id), Guid.Empty, null,null, 1, 1);
                    temp.DianJiLiang = djl.Item1.ToString();

                    temp.Url = MdWxSettingUpHelper.GenGroupDetailUrl(merRedis.wx_appid, Guid.Parse(r.Id));

                    //成功与总数赋值
                    var openingCount = (EsGroupOrderManager.GetByGid2(Guid.Parse(r.Id), new List<int>() { (int)EGroupOrderStatus.拼团成功, (int)EGroupOrderStatus.拼团失败, (int)EGroupOrderStatus.拼团进行中 }, 1, 1)).Item1;
                    var sucessCount = (EsGroupOrderManager.GetByGid2(Guid.Parse(r.Id), EGroupOrderStatus.拼团成功, 1, 1)).Item1;

                    temp.CTCount = sucessCount.ToString();
                    temp.KTCount = openingCount.ToString();
                    temp.Robot =
                        AttHelper.GetValue(Guid.Parse(r.Id), EAttTables.Group.ToString(),
                                EGroupAtt.userobot.ToString());
                    retList.Add(temp);
                }

                return PartialView("mid/_midPatial", retList);
            }
            return PartialView("mid/_midPatial", new List<PartialRowClass>());
        }

        public class PartialRowClass
        {
            public string gid { get; set; }
            public string GroupName { get; set; }
            public string GroupPersonQouta { get; set; }
            public string Price { get; set; }
            public string TuanYou { get; set; }
            public string Robot { get; set; }
            public string KuCun { get; set; }
            public string DianJiLiang { get; set; }
            public string KTCount { get; set; }
            public string CTCount { get; set; }
            public string Url { get; set; }
        }
    }
}