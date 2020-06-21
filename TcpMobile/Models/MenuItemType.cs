using System;

namespace TcpMobile.Models
{
    public enum MenuItemType
    {
        Default,
        CreateGamePage,
        JoinGamePage,
        SingleGamePage
    }

    public class SideBarMenuItem
    {
        public MenuItemType Type { get; set; }
        public string Name { get; set; }
    }
}
