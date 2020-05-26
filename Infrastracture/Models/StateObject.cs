﻿using System.Net;
using System.Net.Sockets;

namespace TcpMobile.Tcp.Models
{
    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];

        public StateObject(Socket soket = null)
        {
            workSocket = soket ?? null;
        }
    }
}
