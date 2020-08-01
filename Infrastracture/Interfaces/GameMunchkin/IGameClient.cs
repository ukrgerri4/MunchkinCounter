using Core.Models;
using Infrastracture.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface IGameClient
    {
        Player MyPlayer { get; set; }
        List<Player> Players { get; set; }
        ObservableCollection<MunchkinHost> Hosts { get; set; }

        Result Connect(IPAddress ip);
        void ConnectSelf();
        Result SendPlayerInfo();
        Result SendUpdatedPlayerName();
        Result SendUpdatedPlayerState();
        void StartListeningServerDisconnection();
        void StartSearchHosts();
        void StartUpdatePlayers();
        Result CloseConnection();
        void StopSearchHosts();
    }
}