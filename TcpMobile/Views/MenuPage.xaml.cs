using Core.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using TcpMobile;
using TcpMobile.Models;
using TcpMobile.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MenuPage : ContentPage
    {
        private JoinGamePage _joinGamePage => DependencyService.Get<JoinGamePage>();
        private CreateGamePage _createGamePage => DependencyService.Get<CreateGamePage>();

        public ObservableCollection<SideBarMenuItem> MenuItems { get; set; }

        public string CurrentVersion => VersionTracking.CurrentVersion;

        public MenuPage()
        {

            InitializeComponent();

            MenuItems = InitMenuItems();

            BindingContext = this;

            MessagingCenter.Subscribe<MainMDPage, bool>(this, "SideMenuOpend", (s, isPresented) => {
                if (isPresented)
                {
                    ReinitMenuItems();
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

        private void ReinitMenuItems()
        {
            var endGameMenuItem = MenuItems.FirstOrDefault(m => m.Type == MenuItemType.EndGame);
            if (_joinGamePage?.ViewModel?.Process == true || _createGamePage?.ViewModel?.WaitingPlayers == true)
            {
                if (endGameMenuItem == null)
                {
                    MenuItems.Insert(3, new SideBarMenuItem { Type = MenuItemType.EndGame, Name = "END GAME", Icon = FontAwesomeIcons.SignOutAlt, Divider = true });
                }
            }
            else
            {
                if (endGameMenuItem != null)
                {
                    MenuItems.RemoveAt(MenuItems.IndexOf(endGameMenuItem));
                }
            }
        }

        private void SetCurrentPage()
        {
            var masterDetailPage = (MainMDPage)Application.Current.MainPage;
            menuItemsListView.SelectedItem = MenuItems.FirstOrDefault(i => i.Type == masterDetailPage.CurrentPage);
        }

        private ObservableCollection<SideBarMenuItem> InitMenuItems()
        {
            var items = new ObservableCollection<SideBarMenuItem>();
            
            items.Add(new SideBarMenuItem { Type = MenuItemType.SingleGame, Name = "SINGLE GAME", Icon = FontAwesomeIcons.User });
            items.Add(new SideBarMenuItem { Type = MenuItemType.CreateGame, Name = "CREATE GAME", Icon = FontAwesomeIcons.Users });
            items.Add(new SideBarMenuItem { Type = MenuItemType.JoinGame, Name = "FIND GAME", Icon = FontAwesomeIcons.BroadcastTower, Divider = true });
#if DEBUG
            items.Add(new SideBarMenuItem { Type = MenuItemType.Debug, Name = "Debug", Icon = FontAwesomeIcons.Code });
#endif
            items.Add(new SideBarMenuItem { Type = MenuItemType.Settings, Name = "Settings", Icon = FontAwesomeIcons.Cogs });
            items.Add(new SideBarMenuItem { Type = MenuItemType.ShareApp, Name = "Share", Icon = FontAwesomeIcons.ShareAlt });
            items.Add(new SideBarMenuItem { Type = MenuItemType.About, Name = "About", Icon = FontAwesomeIcons.InfoCircle });

            return items;
        }
    }
}