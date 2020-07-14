using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpMobile.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainMDPage : MasterDetailPage
    {
        private readonly System.IServiceProvider _serviceProvider;

        Dictionary<MenuItemType, Type> MenuPages;

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
                        default:
                            if (Detail.GetType() != MenuPages[type].GetType())
                            {
                                Detail = (Page)_serviceProvider.GetService(MenuPages[type]);
                            }
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(200);
                                IsPresented = false;
                            });

                            break;
                    }


                }
            );
        }
    }
}