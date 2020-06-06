using System;

namespace TcpMobile.Models
{
    public enum MenuItemType
    {
        Default,
        ServerPage,
        ClientPage,
        SingleGamePage,
        MultiPlayerGamePage,
        Settings
    }

    public class SideBarMenuItem
    {
        public MenuItemType Type { get; set; }
        public string Name { get; set; }
    }
}
