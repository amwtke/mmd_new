using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB;

namespace MD.Configuration.DB
{
    public class ConfigRepository : IDisposable
    {
        private bool disposed = false;
        private readonly ConfigDbContext context;

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

        public ConfigRepository()
        {
            this.context = new ConfigDbContext();
        }

        public ConfigRepository(ConfigDbContext context)
        {
            this.context = context;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public async Task<List<MDConfigItem>>GetAll()
        {
            return await (from config in context.MDConfigs select config).ToListAsync();
        }
    }
}
