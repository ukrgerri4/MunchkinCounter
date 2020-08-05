using Core.Utils;
using Infrastracture.Interfaces;
using MunchkinCounterLan.Models;
using System.Collections.ObjectModel;
using System.Linq;
using TcpMobile;
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
        private IDeviceInfoService _deviceInfoService => DependencyService.Get<IDeviceInfoService>();


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
            GoTo(selectedType);
        }

        public void GoTo(MenuItemType type)
        {
            MessagingCenter.Send(this, "GoTo", type);
            SetCurrentPage();
        }

        private void ReinitMenuItems()
        {
            var endGameMenuItem = MenuItems.FirstOrDefault(m => m.Type == MenuItemType.EndGame);
            if (_joinGamePage?.ViewModel?.Process == true || _createGamePage?.ViewModel?.WaitingPlayers == true)
            {
                if (endGameMenuItem == null)
                {
                    MenuItems.Insert(4, new SideBarMenuItem { Type = MenuItemType.EndGame, Name = "END GAME", Icon = FontAwesomeIcons.SignOutAlt, Divider = true });
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

            items.Add(new SideBarMenuItem { Type = MenuItemType.HomePage, Name = "HOME", Icon = FontAwesomeIcons.Home });
            items.Add(new SideBarMenuItem { Type = MenuItemType.SingleGame, Name = "SINGLE GAME", Icon = FontAwesomeIcons.User });
            items.Add(new SideBarMenuItem { Type = MenuItemType.CreateGame, Name = "CREATE GAME", Icon = FontAwesomeIcons.Users });
            items.Add(new SideBarMenuItem { Type = MenuItemType.JoinGame, Name = "JOIN GAME", Icon = FontAwesomeIcons.BroadcastTower, Divider = true });

            if (_deviceInfoService.IsIgorPhone)
            {
                items.Add(new SideBarMenuItem { Type = MenuItemType.Debug, Name = "Debug", Icon = FontAwesomeIcons.Code });
            }

            items.Add(new SideBarMenuItem { Type = MenuItemType.Settings, Name = "Settings", Icon = FontAwesomeIcons.Cogs });
            items.Add(new SideBarMenuItem { Type = MenuItemType.ShareApp, Name = "Share", Icon = FontAwesomeIcons.ShareAlt });
            items.Add(new SideBarMenuItem { Type = MenuItemType.Contribute, Name = "Contribute", Icon = FontAwesomeIcons.HandsHelping });
            items.Add(new SideBarMenuItem { Type = MenuItemType.About, Name = "About", Icon = FontAwesomeIcons.InfoCircle });

            return items;
        }
    }
}