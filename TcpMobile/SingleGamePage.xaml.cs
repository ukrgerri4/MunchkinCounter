using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleGamePage : ContentPage
    {
        public Player player;
        private readonly ISinglePlayerService<Player> _singlePlayerService;

        public SingleGamePage(ISinglePlayerService<Player> singlePlayerService)
        {
            _singlePlayerService = singlePlayerService;

            InitializeComponent();

            player = _singlePlayerService.GetPlayer();

            var userLevelBinding = new Binding { Source = player, Path = "Level" };
            userLevelLabel.SetBinding(Label.TextProperty, userLevelBinding);
            
            var userModifyersBinding = new Binding { Source = player, Path = "Modifiers" };
            userModifyersLabel.SetBinding(Label.TextProperty, userModifyersBinding);

            var userPowerBinding = new Binding { Source = player, Path = "Power" };
            userPowerLabel.SetBinding(Label.TextProperty, userPowerBinding);

            var publicLevelBinding = new Binding { Source = player, Path = "Level", Mode = BindingMode.OneWay };
            var levelColor = new Binding { Source = player, Path = "Color", Mode = BindingMode.OneWay };
            publicLevelLabel.SetBinding(Label.TextProperty, publicLevelBinding);
            publicLevelLabel.SetBinding(Label.TextColorProperty, levelColor);

            var publicModifiersBinding = new Binding { Source = player, Path = "Modifiers", Mode = BindingMode.OneWay };
            publicModifiersLabel.SetBinding(Label.TextProperty, publicModifiersBinding);

            var publicPowerBinding = new Binding { Source = player, Path = "Power", Mode = BindingMode.OneWay };
            publicPowerLabel.SetBinding(Label.TextProperty, publicPowerBinding);

            var publicNameBinding = new Binding { Source = player, Path = "Name", Mode = BindingMode.OneWay };
            publicNameLabel.SetBinding(Label.TextProperty, publicNameBinding);
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