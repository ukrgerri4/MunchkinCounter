using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using TcpMobile.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TcpMobile
{
    public partial class App : Application
    {
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();
        private IBrightnessService _brightnessService => DependencyService.Get<IBrightnessService>();

        public App()
        {
            InitializeComponent();

            VersionTracking.Track();

            MainPage = new MainMDPage();
        }

        protected override void OnStart()
        {
            if (Preferences.Get(PreferencesKey.KeepScreenOn, false))
            {
                _brightnessService.KeepScreenOn();
            }

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
