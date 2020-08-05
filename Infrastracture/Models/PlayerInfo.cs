namespace TcpMobile.Game.Models
{
    public class PlayerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public byte Sex { get; set; } // 0 - female, 1 - male
        public byte Level { get; set; }
        public byte Modifiers { get; set; }
        public byte Dice { get; set; } // 0 - unset, 1-6 values
    }
}
