using Core.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using TcpMobile.Models;
using TcpMobile.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MenuPage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public SideBarMenuItem[] MenuItems { get; set; }

        public MenuPage(IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            InitializeComponent();

            MenuItems = InitMenuItems();

            BindingContext = this;
        }

        private async void ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (((SideBarMenuItem)(e?.Item))?.Type == null)
                return;

            var selectedType = ((SideBarMenuItem)e.Item).Type;
            switch (selectedType)
            {
                case MenuItemType.ShareApp:
                    await Share.RequestAsync(new ShareTextRequest
                    {
                        Uri = "https://play.google.com/store/apps/details?id=com.kivgroupua.munchkincounterlan",
                        Title = "Share Text"
                    });
                    break;
                case MenuItemType.Debug:
                case MenuItemType.Settings:
                case MenuItemType.About:
                    menuItemsListView.SelectedItem = null;
                    MessagingCenter.Send(this, "GoTo", selectedType);
                    break;
                default:
                    MessagingCenter.Send(this, "GoTo", selectedType);
                    break;
            }
        }

        private SideBarMenuItem[] InitMenuItems()
        {
            return new SideBarMenuItem[]
            {
                new SideBarMenuItem { Type = MenuItemType.SingleGame, Name = "SINGLE GAME", Icon = FontAwesomeIcons.User },
                new SideBarMenuItem { Type = MenuItemType.CreateGame, Name = "CREATE GAME", Icon = FontAwesomeIcons.Users },
                new SideBarMenuItem { Type = MenuItemType.JoinGame, Name = "JOIN GAME", Icon = FontAwesomeIcons.UserAstronaut, Divider = true },
                new SideBarMenuItem { Type = MenuItemType.Debug, Name = "Debug", Icon = FontAwesomeIcons.Code },
                new SideBarMenuItem { Type = MenuItemType.Settings, Name = "Settings", Icon = FontAwesomeIcons.Cogs },
                new SideBarMenuItem { Type = MenuItemType.ShareApp, Name = "Share", Icon = FontAwesomeIcons.ShareAlt },
                new SideBarMenuItem { Type = MenuItemType.About, Name = "About", Icon = FontAwesomeIcons.InfoCircle },

            };
        }
    }
}