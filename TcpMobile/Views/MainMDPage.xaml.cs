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
                        case MenuItemType.Settings:
                        case MenuItemType.About:
                            await Navigation.PushModalAsync((Page)_serviceProvider.GetService(MenuPages[type]));
                            break;
                        default:
                            if (Detail.GetType() != MenuPages[type].GetType())
                            {
                                Detail = (Page)_serviceProvider.GetService(MenuPages[type]);
                            }
                            break;
                    }

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(200);
                        IsPresented = false;
                    });
                }
            );
        }
    }
}