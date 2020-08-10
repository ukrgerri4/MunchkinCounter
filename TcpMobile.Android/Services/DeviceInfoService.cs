using Android.OS;
using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using System;
using TcpMobile.Droid.Services;
using Xamarin.Essentials;

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
                if (!string.IsNullOrWhiteSpace(_deviceId)) { return _deviceId; }

                _deviceId = Preferences.Get(PreferencesKey.DeviceId, null);

                if (string.IsNullOrWhiteSpace(_deviceId))
                {
                    var serialDeviceId = Android.OS.Build.GetSerial();
                    if (!string.IsNullOrWhiteSpace(serialDeviceId) && serialDeviceId != Build.Unknown && serialDeviceId != "0")
                    {
                        _deviceId = serialDeviceId;
                        Preferences.Set(PreferencesKey.DeviceId, _deviceId);
                    }
                }

                if (string.IsNullOrWhiteSpace(_deviceId))
                {
                    _deviceId = Android.Provider.Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
                    Preferences.Set(PreferencesKey.DeviceId, _deviceId);
                }

                if (string.IsNullOrWhiteSpace(_deviceId))
                {
                    _deviceId = Guid.NewGuid().ToString();
                    Preferences.Set(PreferencesKey.DeviceId, _deviceId);
                }

                return _deviceId;
            }
        }

        public bool IsIgorPhone => DeviceId.Equals("2c0c3053b1660e9f", System.StringComparison.InvariantCultureIgnoreCase);
    }
}
