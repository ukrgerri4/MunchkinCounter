using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MultiPlayerGamePage : ContentPage
    {
        private readonly IConfiguration _configuration;
        private readonly IMultiPlayerService<Player> _multiPlayerService;
        
        public Player player;
        public ObservableCollection<Player> lanPlayers;

        public MultiPlayerGamePage(IConfiguration configuration, IMultiPlayerService<Player> multiPlayerService)
        {
            _configuration = configuration;
            _multiPlayerService = multiPlayerService;

            player = new Player();
            player.Id = _configuration["DeviceId"];
            player.Name = "гн.Костин";

            lanPlayers = _multiPlayerService.GetPlayers();

            InitializeComponent();

            var userLevelBinding = new Binding { Source = player, Path = "Level" };
            userLevelLabel.SetBinding(Label.TextProperty, userLevelBinding);

            var userModifyersBinding = new Binding { Source = player, Path = "Modifiers" };
            userModifyersLabel.SetBinding(Label.TextProperty, userModifyersBinding);

            var userPowerBinding = new Binding { Source = player, Path = "Power" };
            userPowerLabel.SetBinding(Label.TextProperty, userPowerBinding);


            for(int i = 0; i < 6; i++)
            {
                lanPlayers.Add(new Player { Id = $"ID[{i}]", Name = $"N_a_m_e - [{i}]"});
            }

            lanPlayersView.ItemsSource = lanPlayers;
            lanPlayersView.ItemTemplate = new DataTemplate(() => {
                var idLabel = new Label();
                idLabel.SetBinding(Label.TextProperty, "Id");

                var nameLabel = new Label();
                nameLabel.SetBinding(Label.TextProperty, "Name");

                var levelLabel = new Label();
                levelLabel.SetBinding(Label.TextProperty, "Level");

                var modifyersLabel = new Label();
                modifyersLabel.SetBinding(Label.TextProperty, "Modifiers");

                var powerLabel = new Label();
                powerLabel.SetBinding(Label.TextProperty, "Power");

                return new ViewCell
                {
                    View = new StackLayout
                    {
                        Padding = new Thickness(0, 5),
                        Orientation = StackOrientation.Horizontal,
                        Children = { idLabel, nameLabel, levelLabel, modifyersLabel, powerLabel }
                    }
                };
            });
        }

        private void IncreaseLevel(object sender, EventArgs e)
        {
            if (player.Level < 10)
            {
                player.Level++;
            }
        }

        private void DecreaseLevel(object sender, EventArgs e)
        {
            if (player.Level > 1)
            {
                player.Level--;
            }
        }

        private void IncreaseModifiers(object sender, EventArgs e)
        {
            if (player.Modifiers < 255)
            {
                player.Modifiers++;
            }
        }

        private void DecreaseModifiers(object sender, EventArgs e)
        {
            if (player.Modifiers > 0)
            {
                player.Modifiers--;
            }
        }
    }
}