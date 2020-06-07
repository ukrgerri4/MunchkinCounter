using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class JoinGamePage : ContentPage
    {
        public ObservableCollection<MunchkinHost> Hosts = new ObservableCollection<MunchkinHost>();
        private readonly IGameLogger _gameLogger;
        private readonly IGameClient _gameClient;

        public JoinGamePage(IGameLogger gameLogger, IGameClient gameClient)
        {
            _gameLogger = gameLogger;
            _gameClient = gameClient;

            InitializeComponent();

            hostsView.ItemsSource = Hosts;
            hostsView.ItemTemplate = new DataTemplate(() => {
                var idLabel = new Label();
                idLabel.SetBinding(Label.TextProperty, "Id");

                var nameLabel = new Label();
                nameLabel.SetBinding(Label.TextProperty, "Name");

                var capacityLabel = new Label();
                capacityLabel.SetBinding(Label.TextProperty, "Сapacity");

                var fullnessLabel = new Label();
                fullnessLabel.SetBinding(Label.TextProperty, "Fullness");

                return new ViewCell
                {
                    View = new StackLayout
                    {
                        Padding = new Thickness(0, 5),
                        Orientation = StackOrientation.Horizontal,
                        VerticalOptions = LayoutOptions.Center,
                        Children = { idLabel, nameLabel, capacityLabel, fullnessLabel }
                    }
                };
            });
        }

        private void RefreshHosts(object sender, EventArgs e)
        {
            _gameLogger.Debug("Start listening broadcast");
            _gameClient.StartListeningBroadcast();
        }

        private void Connect(object sender, EventArgs e)
        {

        }
    }
}