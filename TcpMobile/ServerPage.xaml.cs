using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using System;
using System.Reactive.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ServerPage : ContentPage
    {
        private readonly IGameLogger _gameLogger;
        private readonly ILanServer _gameServer;

        public ServerPage(IGameLogger gameLogger,
            ILanServer gameServer)
        {
            _gameLogger = gameLogger;
            _gameServer = gameServer;

            InitializeComponent();

            //var observable = _gameServer.PacketSubject
            //    .AsObservable()
            //    .Subscribe(packet => { });
        }

        private void StartBroadcast(object sender, EventArgs e)
        {
            _gameServer.StartUdpServer();
            _gameLogger.Debug("Started broadcast");
        }

        private void StopBroadcast(object sender, EventArgs e)
        {
            _gameServer.StopUdpServer();
            _gameLogger.Debug("Stoped broadcast");
        }
    }
}