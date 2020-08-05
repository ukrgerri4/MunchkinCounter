namespace MunchkinCounterLan.Models
{
    public enum MenuItemType
    {
        Default,
        HomePage,
        SingleGame,
        CreateGame,
        JoinGame,

        EndGame,

        Debug,
        Settings,
        ShareApp,
        Contribute,
        About
    }

    public class SideBarMenuItem
    {
        public MenuItemType Type { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool Divider { get; set; } = false;
    }
}
