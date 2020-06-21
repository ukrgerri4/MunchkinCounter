using Core.Models;
using GameMunchkin.Models;
using Infrastracture.Models;
using System.Collections.ObjectModel;
using System.Net;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface IGameClient
    {
        Player MyPlayer { get; set; }
        ObservableCollection<Player> Players { get; set; }
        ObservableCollection<MunchkinHost> Hosts { get; set; }

        void Connect(IPAddress ip);
        void ConnectSelf();
        Result SendPlayerInfo();
        void StartSearchHosts();
        void StartUpdatePlayers();
        void StopSearchHosts();
    }
}