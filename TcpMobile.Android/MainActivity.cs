using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rg.Plugins.Popup.Services;
using TcpMobile.Views;

namespace TcpMobile.Droid
{
    [Activity(Label = "Munchkin counter", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);
            Xamarin.Forms.Forms.Init(this, savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            var configStream = Assets.Open("appsettings.json");
            LoadApplication(Startup.Init(ConfigureServices, configStream));

            //Window.AddFlags(WindowManagerFlags.Fullscreen);
            //Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.HideNavigation;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public async override void OnBackPressed()
        {
            if (Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed))
            {
                await PopupNavigation.Instance.PopAsync();
                return;
            }

            //if (App.Current.MainPage.Navigation.ModalStack.Count > 0)
            //{
            //    await App.Current.MainPage.Navigation.PopModalAsync();
            //    return;
            //}

            if (App.Current.MainPage is MainMDPage mainPage && mainPage.IsPresented)
            {
                mainPage.IsPresented = false;
                return;
            }

            Intent main = new Intent(Intent.ActionMain);
            main.AddCategory(Intent.CategoryHome);
            StartActivity(main);
        }

        void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
        }
    }
}