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

            MenuItems = new SideBarMenuItem[]
            {
                new SideBarMenuItem { Type = MenuItemType.SingleGame, Name = "SINGLE GAME" },
                new SideBarMenuItem { Type = MenuItemType.JoinGame, Name = "JOIN GAME" },
                new SideBarMenuItem { Type = MenuItemType.CreateGame, Name = "CREATE GAME" }
            };

            BindingContext = this;
        }

        private void ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (((SideBarMenuItem)(e?.Item))?.Type == null)
                return;

            var selectedType = ((SideBarMenuItem)e.Item).Type;
            MessagingCenter.Send(this, "GoTo", selectedType);
        }
    }
}