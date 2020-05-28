using Infrastracture.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TcpMobile.Models;
using Xamarin.Forms;

namespace TcpMobile
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        private readonly IGameService _gameService;
        private readonly System.IServiceProvider _serviceProvider;

        Dictionary<MenuItemType, Page> MenuPages;

        public MainPage(System.IServiceProvider serviceProvider, IGameService gameService)
        {
            _serviceProvider = serviceProvider;
            _gameService = gameService;

            InitializeComponent();
            
            MasterBehavior = MasterBehavior.Popover;

            MenuPages = new Dictionary<MenuItemType, Page>
            {
                { MenuItemType.ServerPage, _serviceProvider.GetService<ServerPage>() },
                { MenuItemType.ClientPage, _serviceProvider.GetService<ClientPage>() },
                { MenuItemType.SingleGamePage, _serviceProvider.GetService<SingleGamePage>() },
                { MenuItemType.MultiPlayerGamePage, _serviceProvider.GetService<MultiPlayerGamePage>() }
            };

            Master = _serviceProvider.GetService<MenuPage>();
            Detail = _serviceProvider.GetService<ServerPage>();
        }

        public void NavigateFromMenu(MenuItemType page)
        {
            Detail = MenuPages[page];
        }
    }
}
