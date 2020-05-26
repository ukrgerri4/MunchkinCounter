using Infrastracture.Interfaces;
using System;
using System.Reactive.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ServerPage : ContentPage
    {
        private readonly IGameService _gameService;

        public ServerPage(IGameService gameService)
        {
            _gameService = gameService;
            InitializeComponent();

            ScrollView scrollView = new ScrollView();
            scrollView.Content = stackLayout;
            Content = scrollView;

            var observable = _gameService.GameServer.PacketSubject
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
            _gameService.GameServer.StartBroadcast();
        }

        private void StopBroadcast(object sender, EventArgs e)
        {
            _gameService.GameServer.StopBroadcast();
        }
    }
}