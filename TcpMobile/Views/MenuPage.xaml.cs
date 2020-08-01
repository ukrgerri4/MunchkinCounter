using Core.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using TcpMobile.Models;
using TcpMobile.Views;
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

            MessagingCenter.Subscribe<MainMDPage, bool>(this, "SideMenuOpend", (s, isPresented) => {
                if (isPresented)
                {
                    SetCurrentPage();
                }
            });
        }

        private void ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (((SideBarMenuItem)(e?.Item))?.Type == null)
                return;

            var selectedType = ((SideBarMenuItem)e.Item).Type;
            switch (selectedType)
            {
                case MenuItemType.Debug:
                case MenuItemType.Settings:
                case MenuItemType.ShareApp:
                case MenuItemType.About:
                    MessagingCenter.Send(this, "GoTo", selectedType);
                    break;
                default:
                    MessagingCenter.Send(this, "GoTo", selectedType);
                    break;
            }

            SetCurrentPage();
        }

        private void SetCurrentPage()
        {
            var masterDetailPage = _serviceProvider.GetService<MainMDPage>();
            menuItemsListView.SelectedItem = MenuItems.FirstOrDefault(i => i.Type == masterDetailPage.CurrentPage);
        }

        private SideBarMenuItem[] InitMenuItems()
        {
            return new SideBarMenuItem[]
            {
                new SideBarMenuItem { Type = MenuItemType.SingleGame, Name = "SINGLE GAME", Icon = FontAwesomeIcons.User },
                new SideBarMenuItem { Type = MenuItemType.CreateGame, Name = "CREATE GAME", Icon = FontAwesomeIcons.Users },
                new SideBarMenuItem { Type = MenuItemType.JoinGame, Name = "FIND GAME", Icon = FontAwesomeIcons.BroadcastTower, Divider = true },
                new SideBarMenuItem { Type = MenuItemType.Debug, Name = "Debug", Icon = FontAwesomeIcons.Code },
                new SideBarMenuItem { Type = MenuItemType.Settings, Name = "Settings", Icon = FontAwesomeIcons.Cogs },
                new SideBarMenuItem { Type = MenuItemType.ShareApp, Name = "Share", Icon = FontAwesomeIcons.ShareAlt },
                new SideBarMenuItem { Type = MenuItemType.About, Name = "About", Icon = FontAwesomeIcons.InfoCircle },

            };
        }
    }
}