using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Rg.Plugins.Popup.Services;
using TcpMobile.Views;
using Xamarin.Forms;
using TcpMobile.Services;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using TcpMobile.Tcp;
using MunchkinCounterLan.Views;
using TcpMobile.Droid.Services;
using Plugin.InAppBilling;
using Android.Content;

namespace TcpMobile.Droid
{
    [Activity(Label = "Munchkin Counter", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;


            base.OnCreate(savedInstanceState);

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity = this;

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            RegisterDependancies();

            LoadApplication(new App());

            //Window.AddFlags(WindowManagerFlags.Fullscreen);
            //Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.HideNavigation;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            InAppBillingImplementation.HandleActivityResult(requestCode, resultCode, data);
        }

        public async override void OnBackPressed()
        {
            if (Rg.Plugins.Popup.Popup.SendBackPressed(null))
            {
                await PopupNavigation.Instance.PopAsync();
                return;
            }

            if (App.Current.MainPage is MainMDPage mainPage && mainPage.IsPresented)
            {
                mainPage.IsPresented = false;
                return;
            }

            //Intent main = new Intent(Intent.ActionMain);
            //main.AddCategory(Intent.CategoryHome);
            //StartActivity(main);
            MoveTaskToBack(false);
        }

        private void RegisterDependancies()
        {
            // services
            DependencyService.Register<IBrightnessService, AndroidBrightnessService>();
            DependencyService.Register<IDeviceInfoService, DeviceInfoService>();
            DependencyService.Register<IGameLogger, GameLogger>();
            DependencyService.Register<ILanServer, LanServer>();
            DependencyService.Register<ILanClient, LanClient>();
            DependencyService.Register<IGameClient, GameClient>();
            DependencyService.Register<IGameServer, GameServer>();

            // default pages
            DependencyService.Register<MenuPage>();
            DependencyService.Register<SingleGamePage>();
            DependencyService.Register<CreateGamePage>();
            DependencyService.Register<JoinGamePage>();

            //// modal pages
            DependencyService.Register<DebugPage>();
            DependencyService.Register<SettingsPage>();
            DependencyService.Register<ContributePage>();
            DependencyService.Register<AboutPage>();
        }
    }
}