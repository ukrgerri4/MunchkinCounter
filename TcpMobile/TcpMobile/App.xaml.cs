using Microsoft.Extensions.Configuration;
using System.Net;
using TcpMobile.Tcp;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace TcpMobile
{
    public partial class App : Application
    {
        private readonly IConfiguration _configuration;

        public App(IConfiguration configuration)
        {
            InitializeComponent();

            MainPage = new MainPage();
            _configuration = configuration;
        }

        protected override void OnStart()
        {
            var stackLayout = (StackLayout)MainPage.FindByName("stackLayout");

            var server = new MobileTcpServer();
            server.onServerStart += () =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    stackLayout.Children.Add(new Label { Text = _configuration["Environment"] });
                    var dnsName = Dns.GetHostName();
                    stackLayout.Children.Add(new Label { Text = dnsName });
                    var ips = Dns.GetHostAddresses(dnsName);
                    ips.ForEach(ip => stackLayout.Children.Add(new Label { Text = ip.ToString() }));
                });
            };

            server.onClientConnect += (string remoteIp) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    stackLayout.Children.Add(new Label { Text = remoteIp ?? "Empty remoteIp" });
                });
            };

            server.onReceiveData += (string data) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    stackLayout.Children.Add(new Label { Text = data ?? "Empty data" });
                });
            };

            server.Start();
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
