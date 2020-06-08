using Infrastracture.Interfaces;
using Xamarin.Essentials;

namespace TcpMobile.Services
{
    public class AndroidBrightnessService : IBrightnessService
    {
        public void KeepScreenOn()
        {
            DeviceDisplay.KeepScreenOn = true;
        }

        public void KeepScreenOff()
        {
            DeviceDisplay.KeepScreenOn = false;
        }
    }
}