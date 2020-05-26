namespace TcpMobile.Tcp.Enums
{
    public enum MunchkinMessageType : byte
    {
        Undefined,
        InitInfo = 1,
        UpdatePlayerState = 2,
        UpdatePlayerName = 3,
        UpdatePlayers = 4
    }
}
