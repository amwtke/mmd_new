using MD.Lib.DB.Repositorys;
using MD.Lib.Util;
using MD.Model.DB;
using MD.Model.DB.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Mmd.Backend.Controllers
{
    public class LogisticsController : Controller
    {
        // GET: Logistics
        public async Task<ActionResult> Index()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }

        public async Task<JsonResult> GetAllTemplates()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            using (var repo = new AddressRepository())
            {
                List<Logistics_Template> list = await repo.GetTemplateByMidAsync(mer.mid);
                foreach (var template in list)
                {
                    template.items = await repo.GetTemplateItemsByLtidAsync(template.ltid);
                }
                return Json(new { status = "Success", Data = list });
            }
        }

        public async Task<JsonResult> GetTemplatesIdNames()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            using (var repo = new AddressRepository())
            {
                List<Logistics_Template> list = await repo.GetTemplateByMidAsync(mer.mid);
                return Json(new { status = "Success", Data = list });
            }
        }

        public async Task<ActionResult> Add()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }

        public async Task<ActionResult> Modify(Guid id)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            ViewBag.ltid = id;
            return View();
        }

        public async Task<JsonResult> GetById(Guid id)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            using (var repo = new AddressRepository())
            {
                Logistics_Template temp = await repo.GetByIdAsync(id);
                temp.items = await repo.GetTemplateItemsByLtidAsync(temp.ltid);
                return Json(new { status = "Success", Data = temp });
            }
        }

        public async Task<JsonResult> DoModify(Logistics_Template template)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            template.mid = mer.mid;
            try
            {
                using (var repo = new AddressRepository())
                {
                    bool isAdd = false;
                    if (template.ltid.Equals(Guid.Empty))
                    {
                        isAdd = true;
                        template.ltid = Guid.NewGuid();
                    }
                    foreach (var item in template.items)
                    {
                        item.ltid = template.ltid;
                        item.id = Guid.NewGuid();
                        item.createtime = CommonHelper.GetUnixTimeNow();
                        item.lastupdatetime = item.createtime;
                    }
                    if (isAdd)
                        await repo.AddTemplateAsync(template);
                    else
                        await repo.UpdateTemplateAsync(template);
                    repo.AddTemplateItem(template.items);
                }
                return Json(new { status = "Success", message = "Success" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = ex });
            }
        }

        public async Task<ActionResult> AddCompany()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return RedirectToAction("SessionTimeOut", "Session");
            return View();
        }

        public async Task<JsonResult> DoAddCompany(string code)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            using (var repo = new AddressRepository())
            {
                var company = await repo.GetByCodeAsync(code);
                bool res = false;
                if (company != null)
                {
                    Logistics_MerCompany com = new Logistics_MerCompany();
                    com.mid = mer.mid;
                    com.companyCode = company.companyCode;
                    com.companyName = company.companyName;
                    com.createtime = CommonHelper.GetUnixTimeNow();
                    com.orderId = 99;
                    res = await repo.AddMerCompany(com);
                }
                return Json(new { status = "Success",result = res});
            }
        }

        public async Task<JsonResult> DoDeleteCompany(string code)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            using (var repo = new AddressRepository())
            {
                bool res = await repo.DeleteMerCompany(code,mer.mid);
                return Json(new { status = "Success", result = res });
            }
        }

        public async Task<JsonResult> SetDefault(string code)
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            using (var repo = new AddressRepository())
            {
                bool res = await repo.SetDefaultMerCompany(code, mer.mid);
                return Json(new { status = "Success", result = res });
            }
        }

        public async Task<JsonResult> GetMerCompany()
        {
            Merchant mer = await SessionHelper.GetMerchant(this);
            if (mer == null)
                return Json(new { status = "SessionTimeOut" });
            using (var repo = new AddressRepository())
            {
                List<Logistics_MerCompany> list = await repo.GetCompanyByMidAsync(mer.mid);
                Dictionary<string, string> dic = new Dictionary<string, string>();
                foreach (var item in list)
                {
                    dic.Add(item.companyCode, item.companyName);
                }
                return Json(new { data = list });
            }
        }

        public async Task<JsonResult> GetRegion(List<Logistics_Region> listExp)
        {
            using (var repo = new AddressRepository())
            {
                List<Logistics_Region> list = await repo.GetRegionAllAsync();
                if (listExp != null && listExp.Count > 0)
                {
                    list = list.Except(listExp, new RegionComparer()).ToList();
                }
                return Json(new { data = list});
            }
        }

        public async Task<ActionResult> GetRegionJson()
        {
            using (var repo = new AddressRepository())
            {
                List<Logistics_Region> list = await repo.GetRegionAllAsync();
                Dictionary<string, string> dic = new Dictionary<string, string>();
                foreach (var item in list)
                {
                    dic.Add(item.code,item.name);
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(dic);
                System.IO.File.AppendAllText("D:\\regions.json", json);
                return Json(new { data = "success" });
            }
        }

        public async Task<ActionResult> GetCompanyJson()
        {
            using (var repo = new AddressRepository())
            {
                List<Logistics_Company> list = await repo.GetAllCompany();
                Dictionary<string, string> dic = new Dictionary<string, string>();
                foreach (var item in list)
                {
                    dic.Add(item.companyCode, item.companyName);
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(dic);
                System.IO.File.AppendAllText("D:\\company.json", json);
                return Json(new { data = "success" });
            }
        }

        public async Task<ActionResult> BatAddCompany()
        {
            return Content("Error");
            string filePath = @"D:\logistics_company.txt";
            StringBuilder sb = new StringBuilder();
            using (StreamReader sr = System.IO.File.OpenText(filePath))
            {
                string s = "";
                while (true)
                {
                    s = sr.ReadLine();
                    if (s != null && s != Environment.NewLine)
                    {
                        sb.Append(s);
                    }
                    else break;
                }
            }
            string text = sb.ToString();
            //Regex name = new Regex(@"name=(\w+)""");
            //Regex code = new Regex(@"code=(\w+)""");
            text = text.Replace(@"name=","").Replace("code=","|").Replace("\"","");
            List<string> list = text.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<Logistics_Company> listCompany = new List<Logistics_Company>();
            double time = CommonHelper.GetUnixTimeNow();
            foreach (string item in list)
            {
                var company = new Logistics_Company();
                string[] str = item.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                company.lcid = Guid.NewGuid();
                company.companyName = str[0];
                company.companyCode = str[1];
                company.createtime = time;
                company.orderId = 99;
                listCompany.Add(company);
            }
            using (var repo = new AddressRepository())
            {
                await repo.AddCompany(listCompany);
            }
            return Content("Success");
        }

        public class RegionComparer : IEqualityComparer<Logistics_Region>
        {
            public bool Equals(Logistics_Region x, Logistics_Region y)
            {
                if (x == null)
                    return y == null;
                return x.code == y.code;
            }

            public int GetHashCode(Logistics_Region obj)
            {
                if (obj == null)
                    return 0;
                return obj.code.GetHashCode();
            }
        }

        public async Task<JsonResult> GetLogisticsInfo(string companyCode,string number)
        {
            string ApiKey = "";
            string apiurl = @"http://api.kuaidi100.com/api?id=" + ApiKey + "&com=" + companyCode + "&nu=" + number + "&show=0&muti=1&order=desc";
            WebRequest request = WebRequest.Create(apiurl);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            Encoding encode = Encoding.UTF8;
            StreamReader reader = new StreamReader(stream, encode);
            string detail = reader.ReadToEnd();
            return Json(new { data = detail});
        }
    }
}