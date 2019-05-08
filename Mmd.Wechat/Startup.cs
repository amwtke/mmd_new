using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(mmd.wechat.Startup))]
namespace mmd.wechat
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
