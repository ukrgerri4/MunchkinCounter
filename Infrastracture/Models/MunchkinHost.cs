using System.Net;

namespace Infrastracture.Models
{
    public class MunchkinHost
    {
        public string Id { get; set; }
        public IPAddress IpAddress { get; set; }
        public string Name { get; set; } = "Game 1";

        public byte Capacity { get; set; } = 6;
        public byte Fullness { get; set; } = 0;
    }
}
