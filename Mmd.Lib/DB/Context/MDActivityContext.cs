using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB.Activity;
using System.Data.Entity;

namespace MD.Lib.DB.Context
{
    public class MDActivityContext: DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions
                .Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
        }

        public MDActivityContext() : base("name=MDDBContext")
        {
            this.Configuration.LazyLoadingEnabled = true;
            this.Database.Initialize(false);
        }

        public DbSet<Box> Box { get; set; }
        public DbSet<BoxTreasure> BoxTreasure { get; set; }
        public DbSet<UserTreasure> UserTreasure { get; set; }
        public DbSet<Sign> Sign { get; set;}
        public DbSet<UserSign> UserSign { get; set; }
        public DbSet<LadderGroup> LadderGroup { get; set; }
        public DbSet<LadderPrice> LadderPrice { get; set; }
        public DbSet<LadderGroupOrder> LadderGroupOrder { get; set; }
        public DbSet<LadderOrder> LadderOrder { get; set; }
    }
}
