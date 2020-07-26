using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive;
using System.Reactive.Subjects;
using TcpMobile.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TcpMobile
{
    public partial class App : Application
    {
        private readonly System.IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IGameLogger _gameLogger;
        private readonly IBrightnessService _brightnessService;

        public App(System.IServiceProvider serviceProvider,
            IConfiguration configuration,
            IGameLogger gameLogger,
            IBrightnessService brightnessService)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _gameLogger = gameLogger;
            _brightnessService = brightnessService;

            InitializeComponent();

            MainPage = _serviceProvider.GetService<MainMDPage>();

            if (Preferences.Get(PreferencesKey.KeepScreenOn, false))
            {
                _brightnessService.KeepScreenOn();
            }
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
