using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Owin;
using Nooch.Common;
using Owin;

[assembly: OwinStartup(typeof(Nooch.API.Startup))]

namespace Nooch.API
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            bool isRunningOnSandbox = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox"));
            string connString = "";
            connString = Utility.GetValueFromConfig(isRunningOnSandbox ? "HangFireSandboxConnectionString" : "HangFireProductionConnectionString");
            Hangfire.GlobalConfiguration.Configuration.UseSqlServerStorage(connString);



            //app.UseHangfireDashboard();
            app.UseHangfireServer();
        }
    }
}
