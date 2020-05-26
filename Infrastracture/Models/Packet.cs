using System;
using TcpMobile.Tcp.Enums;

namespace TcpMobile.Tcp.Models
{
    public class Packet
    {
        public byte[] Buffer { get; set; }
        public DateTime RecieveTime;

        public bool Valid => Buffer.Length > 2 && Buffer.Length == BitConverter.ToUInt16(Buffer, 0);
        public int Length => Buffer.Length;
        public MunchkinMessageType MessageType => Buffer.Length > 2 ? (MunchkinMessageType)Buffer[2] : 0;

        public Packet(byte[] buffer)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            RecieveTime = DateTime.UtcNow;
        }
    }
}
