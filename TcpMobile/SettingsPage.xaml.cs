using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            //Padding = new Thickness(20);
            //BackgroundColor = Color.Gray;
            //Opacity = 0.5;

            var dnsName = Dns.GetHostName();
            var ips = Dns.GetHostAddresses(dnsName);
            ips.ForEach(ip => dataLayout.Children.Add(new Label { Text = ip.ToString() }));
        }

        private async void Close(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(false);
        }
    }
}