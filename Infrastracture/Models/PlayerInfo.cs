using System;
using System.IO;
using System.Text;

namespace TcpMobile.Game.Models
{
    public class PlayerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public byte Level { get; set; }
        public byte Modifiers { get; set; }


        public override string ToString()
        {
            return $"Id - [{Id}]\nName - [{Name ?? "undifeined"}]\nLVL - [{Level}]\nMOD - [{Modifiers}]\nPWR - [{Level + Modifiers}]";
        }

        public static void Serialize(MemoryStream memoryStream, PlayerInfo obj)
        {
            memoryStream.WriteByte(obj.Level);
            memoryStream.WriteByte(obj.Modifiers);

            var byteId = Encoding.UTF8.GetBytes(obj.Id ?? string.Empty);
            memoryStream.WriteByte((byte)byteId.Length);
            memoryStream.Write(byteId, 0, byteId.Length);

            var byteName = Encoding.UTF8.GetBytes(obj.Name ?? string.Empty);
            memoryStream.WriteByte((byte)byteName.Length);
            memoryStream.Write(byteName, 0, byteName.Length);

            
        }

        public static PlayerInfo Deserialize(MemoryStream memoryStream)
        {
            var playerInfo = new PlayerInfo();

            playerInfo.Level = (byte)memoryStream.ReadByte();
            playerInfo.Modifiers = (byte)memoryStream.ReadByte();

            var nameByteLength = (byte)memoryStream.ReadByte();
            var buffer = new byte[nameByteLength];
            memoryStream.Read(buffer, 0, nameByteLength);
            playerInfo.Name = Encoding.UTF8.GetString(buffer);

            return playerInfo;
        }

    }
}
