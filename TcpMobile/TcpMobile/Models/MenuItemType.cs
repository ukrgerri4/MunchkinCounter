using System;

namespace TcpMobile.Models
{
    public enum MenuItemType
    {
        ServerPage,
        ClientPage,
        SingleGamePage
    }

    public class SideBarMenuItem
    {
        public MenuItemType Type { get; set; }
        public string Name { get; set; }
    }
}
