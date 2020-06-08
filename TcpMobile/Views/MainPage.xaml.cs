using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TcpMobile.ExtendedComponents;
using TcpMobile.Models;
using Xamarin.Forms;

namespace TcpMobile
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        private readonly System.IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        Dictionary<MenuItemType, Page> MenuPages;

        public MainPage(System.IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            InitializeComponent();
            
            MasterBehavior = MasterBehavior.Popover;
            MenuPages = new Dictionary<MenuItemType, Page>
            {
                { MenuItemType.ServerPage, new MunchkinNavigationPage(_serviceProvider.GetService<ServerPage>(), _serviceProvider) },
                { MenuItemType.JoinGamePage, new MunchkinNavigationPage(_serviceProvider.GetService<JoinGamePage>(), _serviceProvider) },
                { MenuItemType.SingleGamePage, new MunchkinNavigationPage(_serviceProvider.GetService<SingleGamePage>(), _serviceProvider) },
                { MenuItemType.MultiPlayerGamePage, new MunchkinNavigationPage(_serviceProvider.GetService<MultiPlayerGamePage>(), _serviceProvider) }
            };

            var defaultPage = (MenuItemType)Convert.ToInt32(_configuration["DefaultPage"]);

            var menuPage = _serviceProvider.GetService<MenuPage>();

            Master = menuPage;
            Detail = MenuPages[defaultPage];
        }

        public void NavigateFromMenu(MenuItemType page)
        {
            Detail = MenuPages[page];
            IsPresented = false;
        }
    }
}
