namespace TcpMobile.Tcp.Enums
{
    public enum MunchkinMessageType : byte
    {
        Undefined,
        HostFound,
        GetId,
        InitInfo,
        UpdatePlayerState,
        UpdatePlayerName,
        UpdatePlayers
    }
}
