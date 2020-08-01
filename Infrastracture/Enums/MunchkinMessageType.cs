namespace TcpMobile.Tcp.Enums
{
    public enum MunchkinMessageType : byte
    {
        Undefined,
        Ping,

        HostFound = 10,
        GetId,
        InitInfo,
        UpdatePlayerState,
        UpdatePlayerName,
        UpdatePlayers
    }
}
