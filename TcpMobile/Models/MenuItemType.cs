using System;

namespace TcpMobile.Models
{
    public enum MenuItemType
    {
        Default,
        CreateGame,
        JoinGame,
        SingleGame,
        Settings,
        Debug,
        About
    }

    public class SideBarMenuItem
    {
        public MenuItemType Type { get; set; }
        public string Name { get; set; }
    }
}
