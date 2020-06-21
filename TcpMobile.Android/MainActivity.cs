using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Java.Lang;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TcpMobile.Droid
{
    [Activity(Label = "TcpMobile", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            var configStream = Assets.Open("appsettings.json");
            LoadApplication(Startup.Init(ConfigureServices, configStream));
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private bool _isBackPressed;
        public override void OnBackPressed()
        {
            if (_isBackPressed)
            {
                FinishAffinity(); // inform Android that we are done with the activity
                return;
            }

            _isBackPressed = true;
            Toast.MakeText(this, "Press back again to exit", ToastLength.Short).Show();

            // Disable back to exit after 2 seconds.
            new Handler().PostDelayed(() => { _isBackPressed = false; }, 2000);
            return;
        }

        void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
        }
    }
}