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

        private ListView ListView;
        private SideBarMenuItem[] MenuItems;
        public MenuPage(IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            InitializeComponent();

            MenuItems = new SideBarMenuItem[]
            {
                new SideBarMenuItem { Type = MenuItemType.CreateGamePage, Name = "CREATE" },
                new SideBarMenuItem { Type = MenuItemType.JoinGamePage, Name = "JOIN" },
                new SideBarMenuItem { Type = MenuItemType.SingleGamePage, Name = "SINGLE" }
            };

            ListView = new ListView();
            ListView.ItemsSource = MenuItems;
            var defaultPage = (MenuItemType)Convert.ToInt32(_configuration["DefaultPage"]);
            ListView.SelectedItem = MenuItems.FirstOrDefault(i => i.Type == defaultPage);
            ListView.ItemTemplate = new DataTemplate(() =>
            {
                Label titleLabel = new Label {
                    FontSize = 20,
                    VerticalTextAlignment = TextAlignment.Center,
                    Padding = new Thickness(10, 0, 0, 0),
                    FontAttributes = FontAttributes.Bold
                };
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
                switch (selectedType)
                {
                    case MenuItemType.CreateGamePage:
                        await Navigation.PushAsync(_serviceProvider.GetService<CreateGamePage>());
                        break;
                    case MenuItemType.JoinGamePage:
                        await Navigation.PushAsync(_serviceProvider.GetService<JoinGamePage>());
                        break;
                    case MenuItemType.SingleGamePage:
                        await Navigation.PushAsync(_serviceProvider.GetService<SingleGamePage>());
                        break;
                }
            };


            Content = new StackLayout { Children = { ListView } };
        }
    }
}