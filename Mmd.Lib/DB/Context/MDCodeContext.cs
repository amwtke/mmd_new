using MD.Model.DB.Code;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.DB.Context
{
    public class MDCodeContext:DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
        }
        public MDCodeContext() : base("name=MDDBContext")
        {
            this.Configuration.LazyLoadingEnabled = true;
            this.Database.Initialize(false);
        }
        public DbSet<CodeAuditPeriod> AuditPeriods { get; set; }
        public DbSet<CodeBizTaocan> BizTaoCan { get; set; }
        public DbSet<CodeBizTaocanItem> BizTaocanItems { get; set; }
        public DbSet<CodeBizType> BizTypes { get; set; }
        public DbSet<CodeExpress> Expresses { get; set; }
        public DbSet<CodeGroupOrderStatus> GroupOrderStatuses { get; set; }
        public DbSet<CodeGroupStatus> GroupStatuses { get; set; }
        public DbSet<CodeGroupType> CodeGroupTypes { get; set; }
        public DbSet<CodeMerPayType> MerPayTypes { get; set; }
        public DbSet<CodeOrderStatus> OrderStatuses { get; set; }
        public DbSet<CodePrePayError> PrePayErrors { get; set; }
        public DbSet<CodeProductCategory> ProductCategorys { get; set; }
        public DbSet<CodeRefundStatus> RefundStatuses { get; set; }
        public DbSet<CodeWayToGet> WayToGets { get; set; }
        public DbSet<CodeWXComAuthInfo> WXComAuthInfos { get; set; }
        public DbSet<CodeWXRefundError> WXRefundErrors { get; set; }
        public DbSet<CodeMerchantStatus> MerchantStatus { get; set; }
        public DbSet<CodeProductStatus> ProductStatus { get; set; }
        public DbSet<CodeMorderStatus> MorderStatus { get; set; }
        public DbSet<CodeNoticeBoardStatus> NoticeBoardStatus { get; set; }
        public DbSet<CodeSkin> CodeSkins { get; set; }
        public DbSet<Codebrand> Codebrands { get; set; }
        public DbSet<Supplystatus> Supplystatus { get; set; }
        public DbSet<CodeNoticeCategory> CodeNoticeCategory { get; set; }
    }
}
