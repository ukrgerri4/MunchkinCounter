using MunchkinCounterLan.Views;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System.Threading.Tasks;
using TcpMobile.Models;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainMDPage : MasterDetailPage
    {
        public MenuItemType CurrentPage { get; set; }

        public MainMDPage()
        {
            InitializeComponent();
        
            MasterBehavior = MasterBehavior.Popover;

            Master = DependencyService.Get<MenuPage>();
            Detail = DependencyService.Get<SingleGamePage>();

            CurrentPage = MenuItemType.SingleGame;

            IsPresentedChanged += (s, e) => MessagingCenter.Send(this, "SideMenuOpend", IsPresented);

            MessagingCenter.Subscribe<MenuPage, MenuItemType>(
                this,
                "GoTo",
                async (sender, type) => {
                    switch (type)
                    {
                        case MenuItemType.Debug:
                            await PopupNavigation.Instance.PushAsync(DependencyService.Get<DebugPage>());
                            break;
                        case MenuItemType.Settings:
                            await PopupNavigation.Instance.PushAsync(DependencyService.Get<SettingsPage>());
                            break;
                        case MenuItemType.ShareApp:
                            await Share.RequestAsync(new ShareTextRequest
                            {
                                Uri = "https://play.google.com/store/apps/details?id=com.kivgroupua.munchkincounterlan",
                                Title = "Share Text"
                            });
                            break;
                        case MenuItemType.Contribute:
                            await PopupNavigation.Instance.PushAsync(DependencyService.Get<ContributePage>());
                            break;
                        case MenuItemType.About:
                            await PopupNavigation.Instance.PushAsync(DependencyService.Get<AboutPage>());
                            break;
                        default:
                            if (CurrentPage != type)
                            {
                                var joinGamePage = DependencyService.Get<JoinGamePage>();
                                var createGamePage = DependencyService.Get<CreateGamePage>();

                                if (joinGamePage?.ViewModel?.Process == true || createGamePage?.ViewModel?.WaitingPlayers == true)
                                {
                                    var alert = new AlertPage("If you leave this page, you will be disconnected.", "Ok", "Cancel");
                                    alert.Confirmed += (s, e) =>
                                    {
                                        createGamePage.StopGame();
                                        joinGamePage.ExitGame();

                                        if (CurrentPage == MenuItemType.JoinGame)
                                        {
                                            joinGamePage.StartSearching();
                                        }

                                        GoToPage(type);
                                    };
                                    await PopupNavigation.Instance.PushAsync(alert);
                                    return;
                                }

                                GoToPage(type);
                            }
                            break;
                    }
                }
            );

            MessagingCenter.Subscribe<CreateGamePage>(this, "StartGame", (s) => {
                var joinPage = DependencyService.Get<JoinGamePage>();
                (joinPage.BindingContext as JoinGameViewModel).Process = true;
                GoToPage(MenuItemType.JoinGame);
            });

        }

        private void GoToPage(MenuItemType type)
        {
            switch(type)
            {
                case MenuItemType.SingleGame:
                    Detail = DependencyService.Get<SingleGamePage>();
                    CurrentPage = type;
                    break;
                case MenuItemType.CreateGame:
                    Detail = DependencyService.Get<CreateGamePage>();
                    CurrentPage = type;
                    break;
                case MenuItemType.JoinGame:
                    Detail = DependencyService.Get<JoinGamePage>();
                    CurrentPage = type;
                    break;
                case MenuItemType.EndGame:
                    break;
            }
            
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                IsPresented = false;
            });
        }
    }
}