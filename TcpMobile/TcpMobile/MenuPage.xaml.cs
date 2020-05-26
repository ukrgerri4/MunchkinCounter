using TcpMobile.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MenuPage : ContentPage
    {
        private readonly System.IServiceProvider _serviceProvider;
        public MenuPage(System.IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();

            ListView listView = new ListView();
            listView.ItemsSource = new SideBarMenuItem[]
            {
                new SideBarMenuItem { Type = MenuItemType.ServerPage, Name = "Server" },
                new SideBarMenuItem { Type = MenuItemType.ClientPage, Name = "Client" },
                new SideBarMenuItem { Type = MenuItemType.SingleGamePage, Name = "SingleGame" }
            };

            listView.ItemTemplate = new DataTemplate(() =>
            {
                Label titleLabel = new Label { FontSize = 20 };
                titleLabel.SetBinding(Label.TextProperty, "Name");

                return new ViewCell
                {
                    View = titleLabel
                };
            });

            listView.ItemSelected += (sender, e) =>
            {
                if (e.SelectedItem == null)
                    return;

                var mainPage = _serviceProvider.GetService<MainPage>();
                mainPage.NavigateFromMenu(((SideBarMenuItem)e.SelectedItem).Type);

            };

            this.Content = new StackLayout { Children = { listView } };
        }


    }
}