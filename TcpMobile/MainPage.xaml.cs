using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
                { MenuItemType.ServerPage, _serviceProvider.GetService<ServerPage>() },
                { MenuItemType.ClientPage, _serviceProvider.GetService<ClientPage>() },
                { MenuItemType.SingleGamePage, _serviceProvider.GetService<SingleGamePage>() },
                { MenuItemType.MultiPlayerGamePage, _serviceProvider.GetService<MultiPlayerGamePage>() }
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
