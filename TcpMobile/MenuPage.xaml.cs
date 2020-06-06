using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using TcpMobile.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MenuPage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private object LastSelectedItem;

        private ListView ListView;
        private SideBarMenuItem[] MenuItems;
        public MenuPage(IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            InitializeComponent();

            ListView = new ListView();
            MenuItems = new SideBarMenuItem[]
            {
                new SideBarMenuItem { Type = MenuItemType.ServerPage, Name = "Server" },
                new SideBarMenuItem { Type = MenuItemType.ClientPage, Name = "Client" },
                new SideBarMenuItem { Type = MenuItemType.SingleGamePage, Name = "SingleGame" },
                new SideBarMenuItem { Type = MenuItemType.MultiPlayerGamePage, Name = "MultiPlayerGame" },
                new SideBarMenuItem { Type = MenuItemType.Settings, Name = "Settings" }
            };

            ListView.ItemsSource = MenuItems;

            var defaultPage = (MenuItemType)Convert.ToInt32(_configuration["DefaultPage"]);
            ListView.SelectedItem = MenuItems.FirstOrDefault(i => i.Type == defaultPage);
            LastSelectedItem = ListView.SelectedItem;

            ListView.ItemTemplate = new DataTemplate(() =>
            {
                Label titleLabel = new Label { FontSize = 20 };
                titleLabel.SetBinding(Label.TextProperty, "Name");

                return new ViewCell
                {
                    View = titleLabel
                };
            });

            ListView.ItemSelected += async (sender, e) =>
            {
                if (((SideBarMenuItem)(e?.SelectedItem))?.Type == null)
                    return;

                var selectedType = ((SideBarMenuItem)e.SelectedItem).Type;

                var mainPage = _serviceProvider.GetService<MainPage>();

                if (selectedType == MenuItemType.Settings)
                {
                    var settingsPage = _serviceProvider.GetService<SettingsPage>();
                    await Navigation.PushModalAsync(settingsPage, false);
                    if (sender is ListView lv1)
                    {
                        lv1.SelectedItem = LastSelectedItem;
                    }
                    mainPage.IsPresented = false;
                    return;
                }

                if (sender is ListView lv2)
                {
                    LastSelectedItem = lv2.SelectedItem;
                }

                mainPage.NavigateFromMenu(selectedType);
            };


            this.Content = new StackLayout { Children = { ListView } };
        }
    }
}