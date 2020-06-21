using Infrastracture.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive;
using System.Reactive.Subjects;
using TcpMobile.ExtendedComponents;
using TcpMobile.Views;
using Xamarin.Forms;

namespace TcpMobile
{
    public partial class App : Application
    {
        private readonly System.IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IGameLogger _gameLogger;

        public App(System.IServiceProvider serviceProvider,
            IConfiguration configuration,
            IGameLogger gameLogger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _gameLogger = gameLogger;

            InitializeComponent();

            MainPage = new MunchkinNavigationPage(_serviceProvider.GetService<SingleGamePage>(), _serviceProvider);
        }

        protected override void OnStart()
        {
            _gameLogger.Debug("Game started");
        }

        protected override void OnSleep()
        {
            _gameLogger.Debug("Game go sleep");
        }

        protected override void OnResume()
        {
            _gameLogger.Debug("Game resumed");
        }
    }
}
