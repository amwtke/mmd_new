using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Mmd.Statistics.Startup))]
namespace Mmd.Statistics
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
