using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Mmd.Activitys.Startup))]
namespace Mmd.Activitys
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
