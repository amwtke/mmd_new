using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB.Address;
using MD.Model.DB.Code;

namespace mmd.lib.DB.Context
{
    public class MDAddressContext :DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
        }

        public MDAddressContext() : base("name=MDDBContext")
        {
            this.Configuration.LazyLoadingEnabled = true;
            this.Database.Initialize(false);
        }

        public DbSet<Province> Provinces { get; set; }
        public DbSet<City> Citys { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Logistics_Region> Logistics_Region { get; set; }
        public DbSet<Logistics_Template> Logistics_Template { get; set; }
        public DbSet<Logistics_TemplateItem> Logistics_TemplateItem { get; set; }
        public DbSet<Logistics_Company> Logistics_Company { get; set; }
        public DbSet<Logistics_MerCompany> Logistics_MerCompany { get; set; }
    }
}
