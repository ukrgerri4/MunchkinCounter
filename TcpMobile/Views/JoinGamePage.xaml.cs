using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Tcp.Enums;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class JoinGamePage : ContentPage
    {
        private readonly IGameLogger _gameLogger;
        private readonly ILanClient _gameClient;

        public ObservableCollection<MunchkinHost> Hosts { get; set; } = new ObservableCollection<MunchkinHost>();

        private IDisposable hostsObservable;

        private Subject<Unit> _destroy = new Subject<Unit>();

        public JoinGamePage(IGameLogger gameLogger, ILanClient gameClient)
        {
            _gameLogger = gameLogger;
            _gameClient = gameClient;

            InitializeComponent();

            BindingContext = this;
        }

        private void RefreshHosts(object sender, EventArgs e)
        {
            _gameLogger.Debug("Start listening broadcast");
            _gameClient.StartListeningBroadcast();
        }

        private void Connect(object sender, EventArgs e)
        {

        }

        protected override void OnAppearing()
        {
            _gameClient.StartListeningBroadcast();

            _gameClient.PacketSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
                .Where(tcpEvent => tcpEvent.Data != null)
                .Where(tcpEvent => tcpEvent.Data.MessageType == MunchkinMessageType.HostFound)
                .Finally(() => _gameLogger.Debug("Game host observable end."))
                .Select(tcpEvent =>
                {
                    var packet = tcpEvent.Data;
                    var position = 3;
                    var host = new MunchkinHost();

                    host.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                    position += packet.Buffer[position];
                    position++;

                    host.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                    position += packet.Buffer[position];
                    position++;

                    host.Capacity = packet.Buffer[position++];
                    host.Fullness = packet.Buffer[position++];

                    _gameLogger.Debug($"Got new packet with ip [{packet.SenderIpAdress}]");
                    return host;
                    //return new MunchkinHost
                    //{
                    //    Id = "qwer-tyui",
                    //    IpAddress = packet.SenderIpAdress,
                    //    Name = "Host №1",
                    //    Fullness = 5,
                    //    Capacity = 3
                    //};
                })
                .Subscribe(host =>
                {
                    if (!Hosts.Any(h => h.Id == host.Id))
                    {
                        _gameLogger.Debug($"Added new host name[{host.Name}]");
                        Hosts.Add(host);
                    }
                    else
                    {
                        var hostToUpdate = Hosts.First(h => h.Id == host.Id);
                        hostToUpdate.Name = host.Name;
                        hostToUpdate.Capacity = host.Capacity;
                        hostToUpdate.Fullness = host.Fullness;
                    }
                });
        }

        protected override void OnDisappearing()
        {
            _destroy.OnNext(Unit.Default);
            _gameClient.StopListeningBroadcast();
        }
    }
}