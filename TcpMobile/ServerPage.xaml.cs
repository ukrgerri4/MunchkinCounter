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
        private readonly IGameServer _gameServer;
        private readonly IGameClient _gameClient;

        public ServerPage(IGameServer gameServer, IGameClient gameClient)
        {
            _gameServer = gameServer;
            _gameClient = gameClient;
            InitializeComponent();

            ScrollView scrollView = new ScrollView();
            scrollView.Content = stackLayout;
            Content = scrollView;

            var observable = _gameServer.PacketSubject
                .AsObservable()
                .Subscribe(packet => {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        stackLayout.Children.Add(new Label { Text = $"Recived - {packet.MessageType}" });
                    });
                });
        }

        private void StartBroadcast(object sender, EventArgs e)
        {
            _gameServer.StartBroadcast();
        }

        private void StopBroadcast(object sender, EventArgs e)
        {
            _gameServer.StopBroadcast();
        }
    }
}