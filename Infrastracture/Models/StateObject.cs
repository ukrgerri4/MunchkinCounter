using System;
using System.Net;
using System.Net.Sockets;

namespace TcpMobile.Tcp.Models
{
    public class StateObject
    {
        public string Id = Guid.NewGuid().ToString();
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public int offset = 0;

        public StateObject(Socket soket = null)
        {
            workSocket = soket ?? null;
        }

        public bool IdRecived => !string.IsNullOrWhiteSpace(Id);
    }
}
