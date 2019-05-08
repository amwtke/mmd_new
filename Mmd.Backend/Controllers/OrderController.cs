using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using MD.Configuration;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Util;
using MD.Lib.Util.Data;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.Aliyun;
using MD.Model.Configuration.UI;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using MD.Model.Index.MD;
using Microsoft.Ajax.Utilities;
using System.Net.Http;
using System.Net;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;

namespace Mmd.Backend.Controllers
{
    public class OrderController : Controller
    {
        // GET: Order
        public async Task<ActionResult> OrderManage(string qdate, int? status, int? wtg)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            if (string.IsNullOrEmpty(qdate))
            {
                string begindate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                string enddate = DateTime.Now.ToString("yyyy-MM-dd");
                qdate = begindate + " - " + enddate;
            }
            ViewBag.status = status;
            ViewBag.wtg = wtg;
            ViewBag.qdate = qdate;
            return View();
        }

        //订单状态改配货中
        public async Task<JsonResult> Distribution(Guid oid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return Json(new { status = "SessionOut", message = "登录超时！" });
            }
            using (var repo = new BizRepository())
            {
                bool res = await repo.UpdateOrderStatusByOid(oid,(int)EOrderStatus.已成团配货中);
                return Json(new { status = "Success",result = res});
            }
        }
        //发货
        public async Task<JsonResult> Shipment(Guid oid,string post_company,string post_number)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return Json(new { status = "SessionOut", message = "登录超时！" });
            }
            using (var repo = new BizRepository())
            {
                var order = await repo.OrderGetByOid(oid);
                if (order != null)
                {
                    order.status = (int)EOrderStatus.已发货待收货;
                    order.shipmenttime = CommonHelper.GetUnixTimeNow();
                    order.post_company = post_company;
                    order.post_number = post_number;
                    bool res = await repo.OrderUpDateAsync(order);
                    return Json(new { status = "Success", result = res });
                }
                return Json(new { status = "Success", result = false });
            }
        }

        public async Task<JsonResult> GetWriteOffPoint()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return new JsonResult()
                {
                    ContentType = "application/json",
                    Data = "",
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                //return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            }
            using (var reop = new BizRepository())
            {
                var WriteOffPoint = await reop.GetWOPsByMidAsync(mer.mid);
                return new JsonResult()
                {
                    ContentType = "application/json",
                    Data = WriteOffPoint,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
        }

        public async Task<JsonResult> GetWriteOffPointer(Guid woid)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return Json(new { status = "SessionOut",message="登录超时！"});
            }
            using (var repo = new BizRepository())
            {
                List<WriteOffer> writerOfferList = await repo.GetWOerByWoidAsync(woid);
                foreach (var item in writerOfferList)
                {
                    if (string.IsNullOrEmpty(item.realname))
                    {
                        var user = await EsUserManager.GetByIdAsync(item.uid);
                        if (user != null) item.realname = user.name;
                    }
                }
                return Json(new { status = "Success",data= writerOfferList });
            }
        }

        public async Task<PartialViewResult> OrderGetPartial(Guid? writeoffpoint,string writeofferid, string qdate, int? status, int? wtg, string q, int pageIndex)
        {
            //session
            object mid = SessionHelper.Get(this, ESessionStateKeys.Mid);
            if (mid == null)
                return PartialView("ProductPartial/ProductListErrorPartial", "Session null!");

            //page size
            UiBackEndConfig uiConfig = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (uiConfig == null)
                throw new MDException(typeof(GroupController), "UiBackEndConfig 没取到！");
            int pageSize = int.Parse(uiConfig.PageSize);

            if (string.IsNullOrEmpty(qdate))
            {
                string begindate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                string enddate = DateTime.Now.ToString("yyyy-MM-dd");
                qdate = begindate + " - " + enddate;
            }

            Guid guidWriteoffer = string.IsNullOrEmpty(writeofferid) ? Guid.Empty : Guid.Parse(writeofferid);
            var ret = await EsOrderManager.SearchAsnyc2(writeoffpoint, guidWriteoffer, qdate, q, Guid.Parse(mid.ToString()), wtg, status, pageIndex, pageSize);

            //全es
            if (ret.Item2.Count > 0)
            {
                var list = new List<OrderDetailPartialObject>();
                using (var repo = new BizRepository())
                {
                    var addressRepo = new AddressRepository();
                    List<WriteOffer> writerOfferList = await repo.GetWOerByMid2Async(Guid.Parse(mid.ToString()));
                    foreach (var o in ret.Item2)
                    {
                        if (o.gid == null)
                            continue;
                        var writeoffer = await repo.UserGetByUidAsync(Guid.Parse(o.writeoffer));
                        //从user_writeoff中读取数据
                        Guid midGuid = Guid.Parse(mid.ToString());
                        Guid uid = Guid.Parse(o.buyer);
                        string userName = o.name;
                        string cellPhone = o.cellphone;
                        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(cellPhone))
                        {
                            User_WriteOff user = await repo.UserWriteoffGetByMidAndUidAsync(midGuid, uid);
                            if (user != null)
                            {
                                userName = user.user_name;
                                cellPhone = user.cellphone;
                            }
                        }
                        var group = await EsGroupManager.GetByGidAsync(Guid.Parse(o.gid));
                        if (group == null) continue;
                        if (group.group_type == (int)EGroupTypes.抽奖团)
                        {
                            var attRepo = new AttRepository();
                            var attName = await attRepo.AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.lucky_status.ToString());
                            var luckStatus = await attRepo.AttValueGetAsync(Guid.Parse(group.Id), attName.attid);
                            if (luckStatus == null || Convert.ToInt16(luckStatus.value) == (int)EGroupLuckyStatus.待开奖)
                            {
                                continue;
                            }
                        }
                        var product = await EsProductManager.GetByPidAsync(Guid.Parse(group.pid));
                        if (product == null) continue;

                        var wop = await EsWriteOffPointManager.GetByIdAsync(Guid.Parse(o.default_writeoff_point));

                        var writeoffer2 = writerOfferList.Where(w => w.uid == Guid.Parse(o.writeoffer)).FirstOrDefault();
                        OrderDetailPartialObject temp = new OrderDetailPartialObject()
                        {
                            oid = Guid.Parse(o.Id),
                            o_no = o.o_no,
                            title = group.title,
                            p_no = product.p_no.ToString(),
                            wtg = ((EWayToGet)o.waytoget).ToString(),
                            gmfs = "团购",
                            price = ((float)o.order_price.Value) / 100,
                            buyer = userName,
                            cellphone = cellPhone,
                            paytime = CommonHelper.FromUnixTime(o.paytime.Value).ToString("yyyy/MM/dd HH:mm"),
                            writeoffpoint = wop == null ? "" : wop.name,
                            writeoffer = writeoffer == null ? "" : writeoffer.name,
                            writeofferName = writeoffer2 == null ? "" : writeoffer2.realname,
                            status = ((EOrderStatusShow)o.status).ToString(),
                            writeofftime = o.writeoffday == null ? "" : CommonHelper.FromUnixTime(o.writeoffday.Value).ToString("yyyy/MM/dd HH:mm"),
                            postaddress = o.postaddress,
                            post_price = o.post_price/100.00
                        };
                        if (o.shipmenttime != null)
                            temp.shipmenttime = CommonHelper.FromUnixTime(o.shipmenttime.Value).ToString("yyyy/MM/dd HH:mm");
                        if (!string.IsNullOrEmpty(o.post_company))
                        {
                            temp.post_company = await addressRepo.GetCompanyNameByCode(o.post_company);
                            temp.post_number = o.post_number;
                        }
                        list.Add(temp);
                    }
                }
                
                OrderPatialObject retObject = new OrderPatialObject()
                {
                    qdate = qdate,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Status = status,
                    Wtg = wtg,
                    TotalCount = ret.Item1,
                    List = list
                };
                return PartialView("Order/_OrderListPartial", retObject);
            }
            OrderPatialObject retObject2 = new OrderPatialObject()
            {
                qdate = qdate,
                PageIndex = pageIndex,
                PageSize = pageSize,
                Status = status,
                Wtg = wtg,
                TotalCount = ret.Item1,
                List = new List<OrderDetailPartialObject>()
            };
            return PartialView("Order/_OrderListPartial", retObject2);
        }

        public class OrderPatialObject
        {
            public string qdate { get; set; }
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int PageSize { get; set; }
            public int? Status { get; set; }
            public int? Wtg { get; set; }
            public List<OrderDetailPartialObject> List { get; set; }
        }
        public class OrderDetailPartialObject
        {
            public Guid oid { get; set; }
            public string o_no { get; set; }
            public string title { get; set; }
            public string p_no { get; set; }
            public string wtg { get; set; }
            /// <summary>
            /// 购买方式
            /// </summary>
            public string gmfs { get; set; }
            public float price { get; set; }
            public string buyer { get; set; }
            public string cellphone { get; set; }
            public string paytime { get; set; }

            /// <summary>
            /// 核销点名称
            /// </summary>
            public string writeoffpoint { get; set; }

            /// <summary>
            /// 核销人
            /// </summary>
            public string writeoffer { get; set; }
            public string writeofferName { get; set; }
            public string writeofferPhone { get; set; }
            public string writeofftime { get; set; }
            /// <summary>
            /// 订单状态
            /// </summary>
            public string status { get; set; }
            public string postaddress { get; set; }
            public string post_company { get; set; }
            public string post_number { get; set; }
            public string shipmenttime { get; set; }
            public double post_price { get; set; }
        }

        public string GetWriteOffUrl(string appid, Guid oid)
        {
            return MdWxSettingUpHelper.GenWriteOffOrderUrl(appid, oid);
        }

        #region 导出
        public async Task<ActionResult> ExportCsv(Guid? writeoffpoint, string writeofferid, string qdate, int status, string q,int waytoget= (int)EWayToGet.自提)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");

            string fileName = ((EOrderStatus)status).ToString() + ".csv";
            if (status == -1)
                fileName = "全部订单.csv";

            if (string.IsNullOrEmpty(qdate))
            {
                string begindate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                string enddate = DateTime.Now.ToString("yyyy-MM-dd");
                qdate = begindate + " - " + enddate;
            }

            Tuple<int, List<IndexOrder>> tuple = null;
            //全部导出
            if (status == -1)
            {
                //tuple = await EsOrderManager.SearchAsnyc(qdate, q, mer.mid, null,
                //            new List<int>()
                //            {
                //                (int) EOrderStatus.拼团成功,
                //                (int) EOrderStatus.已成团未提货,
                //                (int) EOrderStatus.已支付,
                //                (int) EOrderStatus.已退款,
                //                (int) EOrderStatus.未支付,
                //                (int)EOrderStatus.退款失败,
                //                (int)EOrderStatus.已成团未发货,
                //                (int)EOrderStatus.已成团配货中,
                //                (int)EOrderStatus.已发货待收货                                
                //            }, 1, 100000);
                tuple = await EsOrderManager.SearchAsnyc(qdate, q, mer.mid, null,null,1,100000);
            }
            else
            {
                Guid writeoffer = string.IsNullOrEmpty(writeofferid) ? Guid.Empty : Guid.Parse(writeofferid);
                tuple = await EsOrderManager.Search(writeoffpoint, writeoffer, qdate, q, mer.mid, waytoget, status);
            }

            if (tuple.Item2 != null && tuple.Item2.Count > 0)
            {
                string csv = await genCsvByStatus(status, mer.mid, tuple.Item2, waytoget);
                if (!string.IsNullOrEmpty(csv))
                {
                    byte[] bs = Encoding.GetEncoding("gb2312").GetBytes(csv);
                    Stream st = new MemoryStream(bs);
                    return File(st, "text/csv", fileName);
                }
            }
            return Content("无数据！");
        }

        private async Task<string> genCsvByStatus(int status, Guid mid, List<IndexOrder> list,int waytoget)
        {
            var olist = await genOdpo(mid, list);
            string csvString = null;
            if (olist != null && olist.Count > 0 && status == (int)EOrderStatus.已成团未提货)
            {
                DataTable dt = new DataTable("已成团未提货");
                dt.Columns.Add("订单号");
                dt.Columns.Add("拼团活动标题");
                dt.Columns.Add("商品编号");
                dt.Columns.Add("订单金额");
                dt.Columns.Add("购买人");
                dt.Columns.Add("联系电话");
                dt.Columns.Add("下单时间");
                dt.Columns.Add("预约提货店");
                foreach (var o in olist)
                {
                    List<object> _temp = new List<object>();
                    _temp.Add(o.o_no);
                    _temp.Add(o.title);
                    _temp.Add(o.p_no);
                    _temp.Add(o.price);
                    _temp.Add(o.buyer);
                    _temp.Add(o.cellphone);
                    _temp.Add(o.paytime);
                    _temp.Add(o.writeoffpoint);
                    dt.Rows.Add(_temp.ToArray());
                }
                var csv = new CSVHelper(dt);
                csvString = csv.ExportCSV();
                return csvString;
            }

            if (olist != null && olist.Count > 0 && status == (int)EOrderStatus.拼团成功 && waytoget == (int)EWayToGet.自提)
            {
                DataTable dt = new DataTable("拼团成功");
                dt.Columns.Add("订单号");
                dt.Columns.Add("拼团活动标题");
                dt.Columns.Add("商品编号");
                dt.Columns.Add("订单金额");
                dt.Columns.Add("购买人");
                dt.Columns.Add("联系电话");
                dt.Columns.Add("下单时间");
                dt.Columns.Add("提货门店");
                dt.Columns.Add("提货时间");
                dt.Columns.Add("核销人");
                dt.Columns.Add("昵称");
                foreach (var o in olist)
                {
                    List<object> _temp = new List<object>();
                    _temp.Add(o.o_no);
                    _temp.Add(o.title);
                    _temp.Add(o.p_no);
                    _temp.Add(o.price);
                    _temp.Add(o.buyer);
                    _temp.Add(o.cellphone);
                    _temp.Add(o.paytime);
                    _temp.Add(o.writeoffpoint);
                    _temp.Add(o.writeofftime);
                    _temp.Add(o.writeofferName);
                    _temp.Add(o.writeoffer);
                    dt.Rows.Add(_temp.ToArray());
                }
                var csv = new CSVHelper(dt);
                csvString = csv.ExportCSV();
                return csvString;
            }

            //全部订单
            if (olist != null && olist.Count > 0 && status == -1)
            {
                DataTable dt = new DataTable("拼团成功");
                dt.Columns.Add("订单号");
                dt.Columns.Add("拼团活动标题");
                dt.Columns.Add("商品编号");
                dt.Columns.Add("订单金额");
                dt.Columns.Add("购买人");
                dt.Columns.Add("联系电话");
                dt.Columns.Add("下单时间");
                dt.Columns.Add("提货门店");
                dt.Columns.Add("提货时间");
                dt.Columns.Add("核销人");
                dt.Columns.Add("订单状态");
                foreach (var o in olist)
                {
                    List<object> _temp = new List<object>();
                    _temp.Add(o.o_no);
                    _temp.Add(o.title);
                    _temp.Add(o.p_no);
                    _temp.Add(o.price);
                    _temp.Add(o.buyer);
                    _temp.Add(o.cellphone);
                    _temp.Add(o.paytime);
                    _temp.Add(o.writeoffpoint);
                    _temp.Add(o.writeofftime);
                    _temp.Add(o.writeoffer);
                    _temp.Add(o.status);
                    dt.Rows.Add(_temp.ToArray());
                }
                var csv = new CSVHelper(dt);
                csvString = csv.ExportCSV();
                return csvString;
            }
            if (olist != null && olist.Count > 0 && status == (int)EOrderStatus.已成团未发货)
            {
                DataTable dt = new DataTable("待发货");
                dt.Columns.Add("订单号");
                dt.Columns.Add("拼团活动标题");
                dt.Columns.Add("商品编号");
                dt.Columns.Add("订单金额");
                dt.Columns.Add("邮费");
                dt.Columns.Add("购买人");
                dt.Columns.Add("联系电话");
                dt.Columns.Add("邮寄地址");
                dt.Columns.Add("下单时间");
                foreach (var o in olist)
                {
                    List<object> _temp = new List<object>();
                    _temp.Add(o.o_no);
                    _temp.Add(o.title);
                    _temp.Add(o.p_no);
                    _temp.Add(o.price);
                    _temp.Add(o.post_price);
                    _temp.Add(o.buyer);
                    _temp.Add(o.cellphone);
                    _temp.Add(o.postaddress);
                    _temp.Add(o.paytime);
                    dt.Rows.Add(_temp.ToArray());
                }
                var csv = new CSVHelper(dt);
                csvString = csv.ExportCSV();
                return csvString;
            }
            if (olist != null && olist.Count > 0 && status == (int)EOrderStatus.已成团配货中)
            {
                DataTable dt = new DataTable("配货中");
                dt.Columns.Add("订单号");
                dt.Columns.Add("拼团活动标题");
                dt.Columns.Add("商品编号");
                dt.Columns.Add("订单金额");
                dt.Columns.Add("邮费");
                dt.Columns.Add("购买人");
                dt.Columns.Add("联系电话");
                dt.Columns.Add("邮寄地址");
                dt.Columns.Add("下单时间");
                foreach (var o in olist)
                {
                    List<object> _temp = new List<object>();
                    _temp.Add(o.o_no);
                    _temp.Add(o.title);
                    _temp.Add(o.p_no);
                    _temp.Add(o.price);
                    _temp.Add(o.post_price);
                    _temp.Add(o.buyer);
                    _temp.Add(o.cellphone);
                    _temp.Add(o.postaddress);
                    _temp.Add(o.paytime);
                    dt.Rows.Add(_temp.ToArray());
                }
                var csv = new CSVHelper(dt);
                csvString = csv.ExportCSV();
                return csvString;
            }
            if (olist != null && olist.Count > 0)
            {
                if (status == (int)EOrderStatus.已发货待收货 || (status == (int)EOrderStatus.拼团成功 || waytoget == (int)EWayToGet.物流))
                {
                    DataTable dt = new DataTable("已发货");
                    dt.Columns.Add("订单号");
                    dt.Columns.Add("拼团活动标题");
                    dt.Columns.Add("商品编号");
                    dt.Columns.Add("订单金额");
                    dt.Columns.Add("邮费");
                    dt.Columns.Add("购买人");
                    dt.Columns.Add("联系电话");
                    dt.Columns.Add("邮寄地址");
                    dt.Columns.Add("下单时间");
                    dt.Columns.Add("物流公司");
                    dt.Columns.Add("快递单号");
                    dt.Columns.Add("发货时间");
                    foreach (var o in olist)
                    {
                        List<object> _temp = new List<object>();
                        _temp.Add(o.o_no);
                        _temp.Add(o.title);
                        _temp.Add(o.p_no);
                        _temp.Add(o.price);
                        _temp.Add(o.post_price);
                        _temp.Add(o.buyer);
                        _temp.Add(o.cellphone);
                        _temp.Add(o.postaddress);
                        _temp.Add(o.paytime);
                        _temp.Add(o.post_company);
                        _temp.Add(o.post_number);
                        _temp.Add(o.shipmenttime);
                        dt.Rows.Add(_temp.ToArray());
                    }
                    var csv = new CSVHelper(dt);
                    csvString = csv.ExportCSV();
                    return csvString;
                }
            }
            return csvString;
        }

        private async Task<List<OrderDetailPartialObject>> genOdpo(Guid mid, List<IndexOrder> list)
        {
            List<OrderDetailPartialObject> retList = new List<OrderDetailPartialObject>();
            List<User_WriteOff> userList = null;
            List<WriteOffer> writeoffList = null;
            using (var repo = new BizRepository())
            {
                userList = await repo.UserWriteoffGetByMiAsync(mid);
                if (userList == null)
                    return null;
                writeoffList = await repo.GetWOerByMid2Async(mid);
            }
            var addressRepo = new AddressRepository();
           // string user_name = "", cellphone = "";
            foreach (var o in list)
            {
                if (o.gid == null)
                    continue;
                //从user_writeoff中读取数据
                Guid uid = Guid.Parse(o.buyer);//购买用户uid
                Guid writeofferuid = Guid.Parse(o.writeoffer);//核销员uid
                var group = await EsGroupManager.GetByGidAsync(Guid.Parse(o.gid));
                if (group == null) continue;

                var product = await EsProductManager.GetByPidAsync(Guid.Parse(group.pid));
                if (product == null) continue;

                var wop = await EsWriteOffPointManager.GetByIdAsync(Guid.Parse(o.default_writeoff_point));

                string userName = o.name;
                string cellPhone = o.cellphone;
                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(cellPhone))
                {
                    var user = userList.Where(p => p.uid.Equals(uid)).FirstOrDefault();
                    if (user != null)
                    {
                        userName = user.user_name;
                        cellPhone = user.cellphone;
                    }
                }

                //user_name = userList.Where(p => p.uid.Equals(uid)).FirstOrDefault()?.user_name;
                //cellphone = userList.Where(p => p.uid.Equals(uid)).FirstOrDefault()?.cellphone;
                var writeoffer = await EsUserManager.GetByIdAsync(writeofferuid);
                var writeoffer2 = writeoffList.Where(w => w.uid == Guid.Parse(o.writeoffer)).FirstOrDefault();
                OrderDetailPartialObject temp = new OrderDetailPartialObject()
                {
                    o_no = o.o_no,
                    title = group.title,
                    p_no = product.p_no.ToString(),
                    wtg = ((EWayToGet)o.waytoget).ToString(),
                    gmfs = "团购",
                    price = ((float)o.order_price.Value) / 100,
                    buyer = userName,
                    cellphone = cellPhone,
                    paytime = CommonHelper.FromUnixTime(o.paytime.Value).ToString("yyyy/MM/dd HH:mm"),
                    writeoffpoint = wop == null ? "" : wop.name,
                    writeoffer = writeoffer == null ? "" : writeoffer.name,
                    writeofferName = writeoffer2 == null ?"": writeoffer2.realname,
                    status = ((EOrderStatusShow)o.status).ToString(),
                    writeofftime = o.writeoffday == null ? "" : CommonHelper.FromUnixTime(o.writeoffday.Value).ToString("yyyy/MM/dd HH:mm"),
                    postaddress = o.postaddress,
                    post_price = o.post_price / 100.00
                };
                if (o.shipmenttime != null)
                    temp.shipmenttime = CommonHelper.FromUnixTime(o.shipmenttime.Value).ToString("yyyy/MM/dd HH:mm");
                if (!string.IsNullOrEmpty(o.post_company))
                {
                    temp.post_company = await addressRepo.GetCompanyNameByCode(o.post_company);
                    temp.post_number = o.post_number;
                }
                retList.Add(temp);
            }
            return retList;
        }
        #endregion

        [HttpPost]
        public async Task<JsonResult> OrderRefund(string appid, string o_no)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
            {
                return Json(new { flag = false, retMessage = "请重新登录" });
            }
            var retobj = await MdWxPayUtil.OperationRefundAsync(appid, o_no);
            return Json(new { flag = retobj.Item1, retMessage = retobj.Item2 });
        }
    }
}