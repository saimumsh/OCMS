using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(OptimumCoaching.web.Areas.Identity.IdentityHostingStartup))]
namespace OptimumCoaching.web.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => { });
        }
    }
}
