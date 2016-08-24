using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(breakfastbot.Startup))]
namespace breakfastbot
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
