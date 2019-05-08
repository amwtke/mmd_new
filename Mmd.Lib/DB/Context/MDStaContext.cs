using MD.Model.DB.View;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.DB.Context
{
   public class MDStaContext: DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
        }
        public MDStaContext() : base("name=MDDBContext")
        {
            this.Configuration.LazyLoadingEnabled = true;
            this.Database.Initialize(false);
        }
        public DbSet<v_merorder> Vmerorders { get; set; }
    }
}
