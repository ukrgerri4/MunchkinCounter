using System;

namespace TcpMobile.Game.Models
{
    [Serializable]
    public class PlayerInfo
    {
        public byte I { get; set; }
        public string N { get; set; }
        public byte L { get; set; }
        public byte M { get; set; }

        public override string ToString()
        {
            return $"Player [{N ?? "undifeined"}]\nLVL - [{L}]\nMOD - [{M}]\nPWR - [{L + M}]";
        }
    }
}
