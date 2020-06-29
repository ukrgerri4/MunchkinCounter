using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using TcpMobile.Views;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific.AppCompat;

namespace TcpMobile.ExtendedComponents
{
    public class MunchkinNavigationPage : Xamarin.Forms.NavigationPage
    {
        public MunchkinNavigationPage(Page root, IServiceProvider serviceProvider) : base(root)
        {
            On<Android>().SetBarHeight(100);

            ToolbarItems.Add(new ToolbarItem("Menu", null, async () =>
            {
                await Navigation.PushModalAsync(new MunchkinModalNavigationPage(serviceProvider.GetService<GameMenuPage>()));
            }));

            ToolbarItems.Add(new ToolbarItem("", "settings64.png", async () =>
            {
                await Navigation.PushModalAsync(new MunchkinModalNavigationPage(serviceProvider.GetService<SettingsPage>()));
            }));

            ToolbarItems.Add(new ToolbarItem("", "console64.png", async () =>
            {
                await Navigation.PushModalAsync(new MunchkinModalNavigationPage(serviceProvider.GetService<DebugPage>()));
            }));

            BarBackgroundColor = Color.FromHex("795544");
        }
    }
}
