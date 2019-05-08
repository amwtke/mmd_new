using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB;
using MD.Model.DB.Code;

namespace MD.Lib.DB.Context
{
    public class MdAttContext: DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions
                .Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
        }

        public MdAttContext() : base("name=MDDBContext")
        {
            this.Configuration.LazyLoadingEnabled = true;
            this.Database.Initialize(false);
        }

        public DbSet<AttName> AttNames { get; set; }
        public DbSet<AttValue> AttValues { get; set; }
    }
}
