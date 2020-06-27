using System;
using System.Collections.Generic;
using System.Text;
using TcpMobile.Views;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific.AppCompat;

namespace TcpMobile.ExtendedComponents
{
    public class MunchkinModalNavigationPage: Xamarin.Forms.NavigationPage
    {
        public MunchkinModalNavigationPage(Page root) : base(root)
        {
            On<Android>().SetBarHeight(100);
            BarBackgroundColor = Color.FromHex("795544");
        }
    }
}
