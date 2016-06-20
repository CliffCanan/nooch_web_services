using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Owin;
using Nooch.Common;
using Owin;

[assembly: OwinStartup(typeof(Nooch.Web.Startup))]

namespace Nooch.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            bool isRunningOnSandbox = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox"));
            string connString = "";
            connString = Utility.GetValueFromConfig("isRunningOnSandbox") == "true" ? Utility.GetValueFromConfig("HangFireSandboxConnectionString") : Utility.GetValueFromConfig("HangFireProductionConnectionString");
            Hangfire.GlobalConfiguration.Configuration.UseSqlServerStorage(connString);

            //app.UseHangfireDashboard();
            app.UseHangfireServer();
        }
    }
}
