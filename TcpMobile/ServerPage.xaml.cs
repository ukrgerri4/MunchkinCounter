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
        private readonly IGameServer _gameServer;

        public ServerPage(IGameLogger gameLogger,
            IGameServer gameServer)
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
            _gameServer.StartBroadcast();
            _gameLogger.Debug("Started broadcast");
        }

        private void StopBroadcast(object sender, EventArgs e)
        {
            _gameServer.StopBroadcast();
            _gameLogger.Debug("Stoped broadcast");
        }
    }
}