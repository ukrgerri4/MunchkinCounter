using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MultiPlayerGamePage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGameService _gameService;

        public Player player;

        public MultiPlayerGamePage(System.IServiceProvider serviceProvider, IGameService gameService)
        {
            _serviceProvider = serviceProvider;
            _gameService = gameService;
            player = (Player)_gameService.Player;

            InitializeComponent();

            var userLevelBinding = new Binding { Source = player, Path = "Level" };
            userLevelLabel.SetBinding(Label.TextProperty, userLevelBinding);

            var userModifyersBinding = new Binding { Source = player, Path = "Modifiers" };
            userModifyersLabel.SetBinding(Label.TextProperty, userModifyersBinding);

            var userPowerBinding = new Binding { Source = player, Path = "Power" };
            userPowerLabel.SetBinding(Label.TextProperty, userPowerBinding);


            for(int i = 0; i < 6; i++)
            {
                
            }

            var p = _serviceProvider.GetService<IPlayer>() as Player;
            p.Id = "123";
            p.Name = "LOOOL";
            _gameService.LanPlayers.Add(_serviceProvider.GetService<IPlayer>());

            lanPlayersView.ItemsSource = _gameService.LanPlayers;
            lanPlayersView.ItemTemplate = new DataTemplate(() => {
                var idLabel = new Label();
                idLabel.SetBinding(Label.TextProperty, "Id");
                var nameLabel = new Label();
                nameLabel.SetBinding(Label.TextProperty, "Name");
                return new ViewCell
                {
                    View = new StackLayout
                    {
                        Padding = new Thickness(0, 5),
                        Orientation = StackOrientation.Horizontal,
                        Children = { idLabel, nameLabel }
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