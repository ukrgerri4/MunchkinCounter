using Infrastracture.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive;
using System.Reactive.Subjects;
using Xamarin.Forms;

namespace TcpMobile
{
    public partial class App : Application
    {
        private readonly System.IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IGameService _gameService;
        private Subject<Unit> _destroy = new Subject<Unit>();

        public App(System.IServiceProvider serviceProvider, IConfiguration configuration, IGameService gameService)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _gameService = gameService;

            InitializeComponent();

            MainPage = _serviceProvider.GetService<MainPage>();
        }

        protected override void OnStart()
        {
            //_gameService.GameServer.onServerStart += () =>
            //{
            //    Device.BeginInvokeOnMainThread(() =>
            //    {
            //        stackLayout.Children.Add(new Label { Text = _configuration["Environment"] });
            //        var dnsName = Dns.GetHostName();
            //        stackLayout.Children.Add(new Label { Text = dnsName });
            //        var ips = Dns.GetHostAddresses(dnsName);
            //        ips.ForEach(ip => stackLayout.Children.Add(new Label { Text = ip.ToString() }));
            //        stackLayout.Children.Add(new Label { Text = _configuration["DeviceId"] }) ;
            //    });
            //};

            //_gameService.GameServer.onClientConnect += (string remoteIp) =>
            //{
            //    Device.BeginInvokeOnMainThread(() =>
            //    {
            //        stackLayout.Children.Add(new Label { Text = remoteIp ?? "Empty remoteIp" });
            //    });
            //};

            //_gameService.GameServer.onReceiveData += (Packet packet) =>
            //{
            //    var position = 3;
            //    var playerInfo = new PlayerInfo();

            //    switch (packet.MessageType) {
            //        case MunchkinMessageType.InitInfo:
            //            playerInfo.Level = packet.Buffer[position++];
            //            playerInfo.Modifiers = packet.Buffer[position++];

            //            playerInfo.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
            //            position += packet.Buffer[position];
            //            position++;

            //            playerInfo.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
            //            break;
            //        case MunchkinMessageType.UpdatePlayerState:
            //            playerInfo.Level = packet.Buffer[position++];
            //            playerInfo.Modifiers = packet.Buffer[position++];
            //            break;
            //        case MunchkinMessageType.UpdatePlayerName:
            //            playerInfo.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
            //            break;
            //        case MunchkinMessageType.UpdatePlayers:
            //            var players = new List<PlayerInfo>();
            //            var playersCount = packet.Buffer[position++];
            //            for(byte i = 0; i < playersCount; i++)
            //            {
            //                var p = new PlayerInfo();
            //                p.Level = packet.Buffer[position++];
            //                p.Modifiers = packet.Buffer[position++];

            //                p.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
            //                position += packet.Buffer[position];
            //                position++;

            //                p.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
            //                position += packet.Buffer[position];
            //                position++;
            //                players.Add(p);
            //            }
            //            Device.BeginInvokeOnMainThread(() =>
            //            {
            //                foreach(var p in players)
            //                {
            //                    stackLayout.Children.Add(new Label { Text = p.ToString() ?? "Empty data" });
            //                }
            //            });

            //            break;
            //        default:
            //            // error
            //            break;
            //    }

            //    Device.BeginInvokeOnMainThread(() =>
            //    {
            //        stackLayout.Children.Add(new Label { Text = playerInfo.ToString() ?? "Empty data" });
            //    });
            //};

            //_gameService.GameServer.Start(9999);
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
