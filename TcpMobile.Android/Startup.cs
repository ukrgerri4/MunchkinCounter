using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MunchkinCounterLan.Views.Popups;
using System;
using System.Collections.Generic;
using System.IO;
using TcpMobile.Services;
using TcpMobile.Tcp;
using TcpMobile.Views;
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

                    var deviceId = Android.Provider.Settings.Secure
                        .GetString(Android.App.Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
                    c.AddInMemoryCollection(
                        new KeyValuePair<string, string>[] { 
                            new KeyValuePair<string, string> ( "DeviceId", !string.IsNullOrWhiteSpace(deviceId) ? deviceId : Guid.NewGuid().ToString())
                        }
                    );
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
            services.AddSingleton<MenuPage>();
            services.AddSingleton<MainMDPage>();

            // default pages
            services.AddSingleton<SingleGamePage>();
            services.AddSingleton<CreateGamePage>();
            services.AddSingleton<JoinGamePage>();

            // modal pages
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<DebugPage>();
            services.AddSingleton<AboutPage>();

            // popups
            services.AddSingleton<DicePage>();

            // services
            services.AddSingleton<IGameLogger, GameLogger>();
            services.AddSingleton<ILanServer, LanServer>();
            services.AddSingleton<ILanClient, LanClient>();
            services.AddSingleton<IGameClient, GameClient>();
            services.AddSingleton<IGameServer, GameServer>();

            services.AddSingleton<IBrightnessService, AndroidBrightnessService>();
            
        }
    }
}