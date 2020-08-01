using MunchkinCounterLan.Views;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
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
        private readonly IServiceProvider _serviceProvider;

        Dictionary<MenuItemType, Type> MenuPages;

        public MenuItemType CurrentPage { get; set; }

        public MainMDPage(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            InitializeComponent();

            IsPresentedChanged += (s, e) => MessagingCenter.Send(this, "SideMenuOpend", IsPresented);

            MasterBehavior = MasterBehavior.Popover;

            MenuPages = new Dictionary<MenuItemType, Type>
            {
                { MenuItemType.SingleGame, typeof(SingleGamePage) },
                { MenuItemType.CreateGame, typeof(CreateGamePage) },
                { MenuItemType.JoinGame, typeof(JoinGamePage) },
                { MenuItemType.EndGame, typeof(SingleGamePage) },
                { MenuItemType.Debug, typeof(DebugPage) },
                { MenuItemType.Settings, typeof(SettingsPage) },
                { MenuItemType.About, typeof(AboutPage) },
            };

            Master = _serviceProvider.GetService<MenuPage>();
            Detail = _serviceProvider.GetService<SingleGamePage>();
            CurrentPage = MenuItemType.SingleGame;

            MessagingCenter.Subscribe<MenuPage, MenuItemType>(
                this,
                "GoTo",
                async (sender, type) => {
                    switch (type)
                    {
                        case MenuItemType.Debug:
                        case MenuItemType.Settings:
                        case MenuItemType.About:
                            await PopupNavigation.Instance.PushAsync((PopupPage)_serviceProvider.GetService(MenuPages[type]));
                            break;
                        case MenuItemType.ShareApp:
                            await Share.RequestAsync(new ShareTextRequest
                            {
                                Uri = "https://play.google.com/store/apps/details?id=com.kivgroupua.munchkincounterlan",
                                Title = "Share Text"
                            });
                            break;
                        default:
                            if (Detail.GetType() != MenuPages[type])
                            {
                                var joinGamePage = _serviceProvider.GetService<JoinGamePage>();
                                var createGamePage = _serviceProvider.GetService<CreateGamePage>();

                                if (joinGamePage?.ViewModel?.Process == true || createGamePage?.ViewModel?.WaitingPlayers == true)
                                {
                                    var alert = new AlertPage("If you leave this page, you will be disconnected.", "Ok", "Cancel");
                                    alert.Confirmed += (s, e) =>
                                    {
                                        createGamePage.StopGame();
                                        joinGamePage.ExitGame();
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
                var joinPage = _serviceProvider.GetService<JoinGamePage>();
                (joinPage.BindingContext as JoinGameViewModel).Process = true;
                GoToPage(MenuItemType.JoinGame);
            });

        }

        private void GoToPage(MenuItemType type)
        {
            Detail = (Page)_serviceProvider.GetService(MenuPages[type]);
            CurrentPage = type;
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                IsPresented = false;
            });
        }
    }
}