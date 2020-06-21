using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Core.Helpers
{
    public static class DnsHelper
    {
        public static IPAddress GetLocalIp()
        {
            var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            var localIp = localIPs.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && ip.GetAddressBytes()[0] == 192);

            return localIp != null ? localIp : IPAddress.Any;
        }
    }
}
