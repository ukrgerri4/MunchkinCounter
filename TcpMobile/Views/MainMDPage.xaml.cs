using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly System.IServiceProvider _serviceProvider;

        Dictionary<MenuItemType, Type> MenuPages;

        public MenuItemType CurrentPage { get; set; }

        public MainMDPage(System.IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;

            MenuPages = new Dictionary<MenuItemType, Type>
            {
                { MenuItemType.SingleGame, typeof(SingleGamePage) },
                { MenuItemType.CreateGame, typeof(CreateGamePage) },
                { MenuItemType.JoinGame, typeof(JoinGamePage) },
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
                            if (Detail.GetType() != MenuPages[type].GetType())
                            {

                                if (Detail is CreateGamePage cgp && cgp.BindingContext is CreateGameViewModel cgpvm && (cgpvm.WaitingPlayers || cgpvm.Process))
                                {
                                    var alert = new AlertPage("If you leave create game page, game will be ended and players disconnected.", "Ok", "Cancel");
                                    alert.OnConfirm += async (s, e) =>
                                    {
                                        await cgp.Stop();
                                        GoToPage(type);
                                    };
                                    await PopupNavigation.Instance.PushAsync(alert);
                                    return;
                                }

                                if (Detail is JoinGamePage jgp && jgp.BindingContext is JoinGameViewModel jgpvm && jgpvm.Process)
                                {
                                    var alert = new AlertPage("If you leave join game page, you will be disconnected.", "Ok", "Cancel");
                                    alert.OnConfirm += async (s, e) =>
                                    {
                                        jgp.Stop();
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