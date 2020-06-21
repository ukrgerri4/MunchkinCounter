using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using TcpMobile.Views;
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

        public MainPage(System.IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;
            IsPresented = false;

            Master = _serviceProvider.GetService<MenuPage>();
            Detail = _serviceProvider.GetService<CreateGamePage>();

        }

        //public async void GoBack()
        //{
        //    await Navigation.PopAsync();
        //}
    }
}
