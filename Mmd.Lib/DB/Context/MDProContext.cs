using MD.Model.DB;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB.Professional;

namespace MD.Lib.DB.Context
{
    public class MDProContext : DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
        }
        public MDProContext() : base("name=MDDBContext")
        {
            this.Configuration.LazyLoadingEnabled = true;
            this.Database.Initialize(false);
        }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Group_Media> Group_Media { get; set; }
        public DbSet<GroupOrder> GroupOrders { get; set; }
        public DbSet<GroupOrderMember> GroupOrderMembers { get; set; }
        public DbSet<MBiz> MerBizes { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<MOrder> Morder { get; set; }
        public DbSet<MerWXAuth> MerWXAuths { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<PriceMatrix> PriceMatrixes { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductComment> ProductComments { get; set; }
        public DbSet<PublicAdvertise> PublicAdvertises { get; set; }
        public DbSet<AdvertiseArtide> SponsoredContents { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserPost> UserPosts { get; set; }
        public DbSet<WriteOffer> Verifiers { get; set; }
        public DbSet<WriteOffPoint> VerifyPoints { get; set; }
        public DbSet<WXPayResult> WXPayResults { get; set; }
        public DbSet<WXPrePay> WXPrePays { get; set; }
        public DbSet<WXRefund> WXRefunds { get; set; }
        public DbSet<User_WriteOff> UserWriteOff { get; set; }
        public DbSet<NoticeBoard> NoticeBoards { get; set; }
        public DbSet<NoticeReader> NoticeReaders { get; set; }
        public DbSet<Vector> Vectors { get; set; }

        public DbSet<sta_user> sta_users { get; set; }

        public DbSet<Subscribe_User> Subscribe_User { get; set; }
        public DbSet<MBizConsumeLog> MBizConsumes { get; set; }
        public DbSet<Supply> Supplys { get; set; }
        public DbSet<Distribution> Distributions { get; set; }
        public DbSet<Community> Communitys { get; set; }

    }
}
