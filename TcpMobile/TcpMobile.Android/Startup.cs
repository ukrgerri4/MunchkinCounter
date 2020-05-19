using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Xamarin.Essentials;

namespace TcpMobile.Droid
{
    public static class Startup
    {
        public static IServiceProvider ServiceProvider { get; set; }
        public static App Init(Action<HostBuilderContext, IServiceCollection> nativeConfigureServices, Stream configStream)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(c =>
                {
                    // Tell the host configuration where to file the file (this is required for Xamarin apps)
                    c.AddCommandLine(new string[] { $"ContentRoot={FileSystem.AppDataDirectory}" });

                    //read in the configuration file!
                    c.AddJsonStream(configStream);
                })
                .ConfigureServices((c, x) =>
                {
                    //Add this line to call back into your native code
                    nativeConfigureServices(c, x);
                    ConfigureServices(c, x);
                })
                .ConfigureLogging(l => 
                {
                    l.AddConsole(o => {
                        o.DisableColors = true;
                    });
                })
                .Build();

            ServiceProvider = host.Services;

            return ServiceProvider.GetService<App>();
        }

        static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddSingleton<App>();

        }
    }
}