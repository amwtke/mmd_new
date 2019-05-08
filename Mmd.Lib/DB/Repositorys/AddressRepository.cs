using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using mmd.lib.DB.Context;
using MD.Model.DB.Address;
using Province = MD.Model.DB.Address.Province;
using MD.Model.DB.Code;
using MD.Lib.Util;
using MD.Lib.ElasticSearch.MD;
using System.Collections.Concurrent;

namespace MD.Lib.DB.Repositorys
{
    public class AddressRepository : IDisposable
    {
        private bool disposed = false;
        private readonly MDAddressContext context;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
                disposed = true;
            }
        }
        #region common
        public AddressRepository()
        {
            this.context = new MDAddressContext();
        }
        public AddressRepository(MDAddressContext context)
        {
            this.context = context;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region operation
        #region Province,city,district

        public async Task<bool> AddProvince(MD.Model.Json.Province p)
        {
            if (p == null) return false;

            var pp = await GetProvinceByIdAsync(p.id);
            if (pp!=null && pp.code > 0)//存在
            {
                pp.code = p.id;
                pp.province = p.province;
            }
            else
            {
                if(pp==null) pp = new Province();
                pp.code = p.id;
                pp.province = p.province;
                //var pro = new Province()
                //{
                //    id = p.id,
                //    province = p.province
                //};
                context.Provinces.Add(pp);
            }
            return await context.SaveChangesAsync() > 0;
        }

        public bool AddProvince2(MD.Model.Json.Province p)
        {

            var pro = new Province();

            //pro.code = 1;//p.id;
            pro.province = p.province;
            pro.code = p.id;
            context.Provinces.Add(pro);
            return context.SaveChanges() > 0;
        }

        public async Task<bool> AddCity(MD.Model.Json.City c,int pro_id)
        {
            if (c == null ) return false;

            var cc = await GetCityByIdAsync(c.id);
            if (cc!=null && cc.code > 0)
            {
                cc.code = c.id;
                cc.province_id = pro_id;
                cc.city = c.city;
            }
            else
            {
                if(cc==null)
                    cc = new City();
                cc.code = c.id;
                cc.city = c.city;
                cc.province_id = pro_id;
                //var ci = new City()
                //{
                //    id = c.id,
                //    city = c.city,
                //    province_id = pro_id
                //};
                context.Citys.Add(cc);
            }
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddDistrict(MD.Model.Json.District d, int ci_id)
        {
            if (d == null) return false;

            var dd = await GetDistrictByIdAsync(d.id);
            if (dd!=null && dd.code> 0)
            {
                dd.code = d.id;
                dd.city_id = ci_id;
                dd.district = d.district;
            }

            else
            {
                if(dd==null) dd = new District();
                dd.code = d.id;
                dd.city_id = ci_id;
                dd.district = d.district;
                //var di = new District()
                //{
                //    id = d.id,
                //    district = d.district,
                //    city_id = ci_id
                //};
                context.Districts.Add(dd);
            }
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<Province> GetProvinceByIdAsync(int pro_id)
        {
            return await (from pro in context.Provinces where pro.code == pro_id select pro).FirstOrDefaultAsync();
        }

        public async Task<City> GetCityByIdAsync(int ci_id)
        {
            return await (from ci in context.Citys where ci.code == ci_id select ci).FirstOrDefaultAsync();
        }

        public async Task<District> GetDistrictByIdAsync(int di_id)
        {
            return await (from di in context.Districts where di.code == di_id select di).FirstOrDefaultAsync();
        }

        public async Task<List<Province>> GetProvincesAsync()
        {
            return await (from pro in context.Provinces select pro).ToListAsync();
        }

        public async Task<List<City>> GetCitysAsync(int pro_id)
        {
            return await (from ci in context.Citys where ci.province_id == pro_id select ci).ToListAsync();
        }

        public async Task<List<District>> GetDistrictAsnyc(int ci_id)
        {
            return await (from dis in context.Districts where dis.city_id == ci_id select dis).ToListAsync();
        }
        #endregion

        #region Logistics_Company
        static ConcurrentDictionary<string, string> _dicCompany = new ConcurrentDictionary<string, string>();
        public async Task<string> GetCompanyNameByCode(string code)
        {
            string retstring = "";
            if (_dicCompany.TryGetValue(code, out retstring))
            {  
                return retstring;
            }
            else
            {
                _dicCompany = await GetCompanyCodeNameAsync();
                if (_dicCompany.ContainsKey(code))
                {
                    return _dicCompany[code];
                }
                return "";
            }
        }
        public async Task<ConcurrentDictionary<string, string>> GetCompanyCodeNameAsync()
        {
            var list = await (from j in context.Logistics_Company select j).ToListAsync();
            foreach (var item in list)
            {
                _dicCompany[item.companyCode] = item.companyName;
            }
            return _dicCompany;
        }
        public async Task<bool> AddCompany(List<Logistics_Company> list)
        {
            context.Logistics_Company.AddRange(list);
            int res = await context.SaveChangesAsync();
            return res > 0;
        }

        public async Task<bool> AddMerCompany(Logistics_MerCompany merCompany)
        {
            context.Logistics_MerCompany.Add(merCompany);
            int res = await context.SaveChangesAsync();
            return res > 0;
        }

        public async Task<bool> DeleteMerCompany(string companyCode,Guid mid)
        {
            var com = await context.Logistics_MerCompany.FirstOrDefaultAsync(c=> c.mid == mid && c.companyCode == companyCode);
            if (com != null)
            {
                context.Logistics_MerCompany.Remove(com);
                int res = await context.SaveChangesAsync();
                return res > 0;
            }
            return false;
        }

        public async Task<bool> SetDefaultMerCompany(string companyCode, Guid mid)
        {
            var list = await context.Logistics_MerCompany.Where(c => c.mid == mid).ToListAsync();
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item.companyCode != companyCode)
                        item.isDefault = 0;
                    else item.isDefault = 1;
                }
                int res = await context.SaveChangesAsync();
                return res > 0;
            }
            return false;
        }

        public async Task<List<Logistics_Company>> GetAllCompany()
        {
            return await (from r in context.Logistics_Company orderby r.orderId,r.companyCode select r).ToListAsync();
        }

        public async Task<List<Logistics_MerCompany>> GetCompanyByMidAsync(Guid mid)
        {
            return await (from r in context.Logistics_MerCompany where r.mid == mid orderby r.isDefault descending, r.orderId select r).ToListAsync();
        }

        public async Task<Logistics_Company> GetByCodeAsync(string companyCode)
        {
            return await (from r in context.Logistics_Company where r.companyCode == companyCode select r).FirstOrDefaultAsync();
        }
        #endregion

        #region Logistics_Region
        public async Task<List<Logistics_Region>> GetRegionByFatherIdAsync(int fatherId)
        {
            return await (from r in context.Logistics_Region where r.fatherId == fatherId select r).ToListAsync();
        }
        public async Task<List<Logistics_Region>> GetRegionAllAsync()
        {
            return await (from r in context.Logistics_Region orderby r.orderId select r).ToListAsync();
        }

        public async Task<List<Logistics_Region>> GetRegionByLevelAsync(int level)
        {
            return await (from r in context.Logistics_Region where r.categoryLevel < level orderby r.orderId select r).ToListAsync();
        }
        #endregion

        #region Logistics_Template

        public async Task<List<Logistics_Template>> GetTemplateByMidAsync(Guid mid)
        {
            return await context.Logistics_Template.Where(l=>l.mid == mid).ToListAsync();
        }

        public async Task<Logistics_Template> GetByIdAsync(Guid ltid)
        {
            return await context.Logistics_Template.FirstOrDefaultAsync(l => l.ltid == ltid);
        }

        public async Task<List<Logistics_TemplateItem>> GetTemplateItemsByLtidAsync(Guid ltid)
        {
            return await context.Logistics_TemplateItem.Where(l => l.ltid == ltid).ToListAsync();
        }

        public async Task<bool> AddTemplateAsync(Logistics_Template tpl)
        {
            tpl.createtime = CommonHelper.GetUnixTimeNow();
            tpl.lastupdatetime = tpl.createtime;
            context.Logistics_Template.Add(tpl);
            var obj = EsLogisticsTemplateManager.GenObject(tpl);
            await EsLogisticsTemplateManager.AddOrUpdateAsync(obj);
            return context.SaveChanges() > 0;
        }

        public async Task<bool> UpdateTemplateAsync(Logistics_Template tpl)
        {
            var temp = context.Logistics_Template.FirstOrDefault(l => l.ltid == tpl.ltid);
            if (temp != null)
            {
                temp.lastupdatetime = CommonHelper.GetUnixTimeNow();
                temp.name = tpl.name;
                var obj = EsLogisticsTemplateManager.GenObject(tpl);
                await EsLogisticsTemplateManager.AddOrUpdateAsync(obj);
                return context.SaveChanges() > 0;
            }
            return false;
        }

        public bool AddTemplateItem(Logistics_TemplateItem item)
        {
            item.createtime = CommonHelper.GetUnixTimeNow();
            item.lastupdatetime = item.createtime;
            context.Logistics_TemplateItem.Add(item);
            return context.SaveChanges() > 0;
        }
        public bool AddTemplateItem(List<Logistics_TemplateItem> list)
        {
            Guid ltid = list[0].ltid;
            var items = context.Logistics_TemplateItem.Where(i => i.ltid == ltid);
            context.Logistics_TemplateItem.RemoveRange(items);
            context.SaveChanges();
            context.Logistics_TemplateItem.AddRange(list);
            return context.SaveChanges() > 0;
        }
        #endregion

        #endregion
    }
}
