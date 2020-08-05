using Infrastracture.Interfaces;
using TcpMobile.Droid.Services;

[assembly: Xamarin.Forms.Dependency(typeof(DeviceInfoService))]
namespace TcpMobile.Droid.Services
{
    public class DeviceInfoService : IDeviceInfoService
    {
        private string _deviceId;

        public string DeviceId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_deviceId))
                {
                    _deviceId = Android.Provider.Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
                }

                return _deviceId;
            }
        }

        public bool IsIgorPhone => DeviceId.Equals("2c0c3053b1660e9f", System.StringComparison.InvariantCultureIgnoreCase);
    }
}
