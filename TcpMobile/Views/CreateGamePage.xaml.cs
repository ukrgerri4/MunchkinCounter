using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Game.Models;
using TcpMobile.Services;
using TcpMobile.Tcp.Enums;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    public class CreateGameViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public MunchkinHost Host { get; set; } = new MunchkinHost();
        public Player Player { get; set; } = new Player();

        public ConcurrentDictionary<string, PlayerInfo> ServerPlayers { get; set; } = new ConcurrentDictionary<string, PlayerInfo>();

        public ObservableCollection<Player> Players { get; set; } = new ObservableCollection<Player>();

        private bool _creatingGame;
        private bool _waitingPlayers;
        private bool _process;

        public CreateGameViewModel()
        {
            Player.Name = "Player 1";
        }

        public string HostName
        {
            get => Host.Name;
            set
            {
                if (Host.Name != value)
                {
                    Host.Name = value;
                    OnPropertyChanged(nameof(HostName));
                }
            }
        }

        public byte HostСapacity
        {
            get => Host.Capacity;
            set
            {
                if (Host.Capacity != value)
                {
                    Host.Capacity = value;
                    OnPropertyChanged(nameof(HostСapacity));
                }
            }
        }

        public string PlayerName
        {
            get => Player.Name;
            set
            {
                if (Player.Name != value)
                {
                    Player.Name = value;
                    OnPropertyChanged(nameof(PlayerName));
                }
            }
        }

        public bool CreatingGame
        {
            get => _creatingGame;
            set
            {
                if (_creatingGame != value)
                {
                    _creatingGame = value;
                    if (_creatingGame)
                    {
                        WaitingPlayers = false;
                        Process = false;
                    }

                    OnPropertyChanged(nameof(CreatingGame));
                }
            }
        }

        public bool WaitingPlayers
        {
            get => _waitingPlayers;
            set
            {
                if (_waitingPlayers != value)
                {
                    _waitingPlayers = value;
                    if (_waitingPlayers)
                    {
                        CreatingGame = false;
                        Process = false;
                    }

                    OnPropertyChanged(nameof(WaitingPlayers));
                }
            }
        }

        public bool Process
        {
            get => _process;
            set
            {
                if (_process != value)
                {
                    _process = value;
                    if (_process)
                    {
                        CreatingGame = false;
                        WaitingPlayers = false;
                    }
                    
                    OnPropertyChanged(nameof(Process));
                }
            }
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateGamePage : ContentPage
    {
        private readonly IGameLogger _gameLogger;
        private readonly IConfiguration _configuration;
        private readonly IMultiPlayerService<Player> _multiPlayerService;
        private readonly ILanClient _lanClient;
        private readonly ILanServer _gameServer;
        private readonly ServerPlayersData _serverPlayersData;

        private CreateGameViewModel _viewModel = new CreateGameViewModel();

        private Subject<Unit> _updatePlayersSubject = new Subject<Unit>();

        private IDisposable _hostBroadcaster;

        private Subject<Unit> _destroy = new Subject<Unit>();

        public CreateGamePage(
            IGameLogger gameLogger,
            IConfiguration configuration,
            IMultiPlayerService<Player> multiPlayerService,
            ILanClient gameClient,
            ILanServer gameServer,
            ServerPlayersData serverPlayersData)
        {
            _gameLogger = gameLogger;
            _configuration = configuration;
            _multiPlayerService = multiPlayerService;
            _lanClient = gameClient;
            _gameServer = gameServer;
            _serverPlayersData = serverPlayersData;

            InitializeComponent();

            _viewModel = new CreateGameViewModel() { 
                Player = new Player { 
                    Id = !string.IsNullOrWhiteSpace(_configuration["DeviceId"]) ? _configuration["DeviceId"] : Guid.NewGuid().ToString(),
                    Name = "Player 1"
                },
                CreatingGame = true
            };
            _viewModel.Players.Add(new Player());

            BindingContext = _viewModel;
        }

        private void TryCreate(object sender, EventArgs args)
        {
            //var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            _gameServer.StartTcpServer();
            _gameServer.StartUdpServer();

            BroadcastHostData();
            SubscribeForNewPlayers();
            BroadcastUpdatedPlayersData();

            StartUpdatePlayers();

            var localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            var localIp = localIPs.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && ip.GetAddressBytes()[0] == 192);
            
            if (localIp != null)
            {
                _lanClient.Connect(localIp);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                    memoryStream.WriteByte((byte)MunchkinMessageType.InitInfo);

                    var byteId = Encoding.UTF8.GetBytes(_viewModel.Player.Id ?? string.Empty);
                    memoryStream.WriteByte((byte)byteId.Length);
                    memoryStream.Write(byteId, 0, byteId.Length);

                    var byteName = Encoding.UTF8.GetBytes(_viewModel.Player.Name ?? string.Empty);
                    memoryStream.WriteByte((byte)byteName.Length);
                    memoryStream.Write(byteName, 0, byteName.Length);

                    memoryStream.WriteByte(_viewModel.Player.Level);
                    memoryStream.WriteByte(_viewModel.Player.Modifiers);

                    memoryStream.WriteByte(10);
                    memoryStream.WriteByte(4);

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                    memoryStream.Seek(0, SeekOrigin.End);
                    var initMessageResult = _lanClient.SendMessage(memoryStream.ToArray());
                    if (initMessageResult.IsFail) {
                        _gameLogger.Debug("Init message during connect to self fail.");
                    }
                }
            }

            //_viewModel.ConnectedPlayers.Add

            _viewModel.WaitingPlayers = true;
        }

        private async void TryStart(object sender, EventArgs args)
        {
            try
            {
                var confirm = await DisplayAlert("Create new game", "Are you shure want to start new game?", "Yes", "No");
                if (confirm)
                {
                    _hostBroadcaster.Dispose();
                    _gameServer.StopUdpServer();

                    _viewModel.Process = true;
                }
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Error start game: {e.Message}");
            }
        }

        private void BroadcastHostData()
        {
            _hostBroadcaster = Observable.Interval(TimeSpan.FromSeconds(1))
                //.Throttle(TimeSpan.FromSeconds(1))
                .Finally(() => { _gameLogger.Debug("Test obs end."); })
                .Select(data => {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                        memoryStream.WriteByte((byte)MunchkinMessageType.HostFound);

                        var byteId = Encoding.UTF8.GetBytes(_viewModel.Host.Id ?? string.Empty);
                        memoryStream.WriteByte((byte)byteId.Length);
                        memoryStream.Write(byteId, 0, byteId.Length);

                        var byteName = Encoding.UTF8.GetBytes(_viewModel.Host.Name ?? string.Empty);
                        memoryStream.WriteByte((byte)byteName.Length);
                        memoryStream.Write(byteName, 0, byteName.Length);

                        memoryStream.WriteByte(_viewModel.Host.Capacity);
                        memoryStream.WriteByte(_viewModel.Host.Fullness);

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                        memoryStream.Seek(0, SeekOrigin.End);

                        return memoryStream.ToArray();
                    }
                })
                .Subscribe(message =>
                {
                    _gameServer.BroadcastMessage(message);
                });
        }

        private void SubscribeForNewPlayers()
        {
            _gameServer.PacketSubject.AsObservable()
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
                .Where(tcpEvent => tcpEvent.Data != null)
                .Where(tcpEvent => tcpEvent.Data.MessageType == MunchkinMessageType.InitInfo ||
                    tcpEvent.Data.MessageType == MunchkinMessageType.UpdatePlayerState ||
                    tcpEvent.Data.MessageType == MunchkinMessageType.UpdatePlayerName)
                .Do(tcpEvent => _gameLogger.Debug($"Recieved message {tcpEvent.Data.MessageType}"))
                .Subscribe(tcpEvent =>
                {
                    var packet = tcpEvent.Data;
                    var position = 3;

                    switch (packet.MessageType)
                    {
                        case MunchkinMessageType.InitInfo:
                            var playerInfo = new PlayerInfo();

                            playerInfo.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                            position += packet.Buffer[position];
                            position++;

                            playerInfo.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                            position += packet.Buffer[position];
                            position++;

                            playerInfo.Level = packet.Buffer[position++];
                            playerInfo.Modifiers = packet.Buffer[position++];

                            if (packet.SenderId != playerInfo.Id)
                            {
                                // log some warning
                            }

                            if (!_viewModel.ServerPlayers.TryAdd(packet.SenderId, playerInfo))
                            {
                                _viewModel.ServerPlayers[packet.SenderId].Name = playerInfo.Name;
                                _viewModel.ServerPlayers[packet.SenderId].Level = playerInfo.Level;
                                _viewModel.ServerPlayers[packet.SenderId].Modifiers = playerInfo.Modifiers;
                            }

                            break;
                        case MunchkinMessageType.UpdatePlayerState:
                            var level = packet.Buffer[position++];
                            var modifiers = packet.Buffer[position++];

                            if (_viewModel.ServerPlayers.ContainsKey(packet.SenderId))
                            {
                                _viewModel.ServerPlayers[packet.SenderId].Level = level;
                                _viewModel.ServerPlayers[packet.SenderId].Modifiers = modifiers;
                            }

                            break;
                        case MunchkinMessageType.UpdatePlayerName:
                            var name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);

                            if (_viewModel.ServerPlayers.ContainsKey(packet.SenderId))
                            {
                                _viewModel.ServerPlayers[packet.SenderId].Name = name;
                            }
                            break;
                    }

                    _updatePlayersSubject.OnNext(Unit.Default);
                });
        }

        private void BroadcastUpdatedPlayersData()
        {
            _updatePlayersSubject.AsObservable()
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Do(_ => _gameLogger.Debug("Start send players data."))
                .Subscribe(_ =>
                {
                    var playerKeys = _viewModel.ServerPlayers.Keys.ToList();

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                        memoryStream.WriteByte((byte)MunchkinMessageType.UpdatePlayers);
                        memoryStream.WriteByte((byte)playerKeys.Count);

                        foreach (var key in playerKeys)
                        {
                            var byteId = Encoding.UTF8.GetBytes(_viewModel.ServerPlayers[key].Id ?? string.Empty);
                            memoryStream.WriteByte((byte)byteId.Length);
                            memoryStream.Write(byteId, 0, byteId.Length);

                            var byteName = Encoding.UTF8.GetBytes(_viewModel.ServerPlayers[key].Name ?? string.Empty);
                            memoryStream.WriteByte((byte)byteName.Length);
                            memoryStream.Write(byteName, 0, byteName.Length);

                            memoryStream.WriteByte(_viewModel.ServerPlayers[key].Level);
                            memoryStream.WriteByte(_viewModel.ServerPlayers[key].Modifiers);
                        }

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                        memoryStream.Seek(0, SeekOrigin.End);

                        var message = memoryStream.ToArray();

                        foreach (var key in playerKeys)
                        {
                            _gameServer.SendMessage(key, message);
                        }
                    }
                });
        }

        private void StartUpdatePlayers()
        {
            _lanClient.PacketSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
                .Where(tcpEvent => tcpEvent.Data != null)
                .Where(tcpEvent => tcpEvent.Data.MessageType == MunchkinMessageType.UpdatePlayers)
                .Do(tcpEvent => _gameLogger.Debug($"Recieved {MunchkinMessageType.UpdatePlayers} message"))
                .Select(tcpEvent =>
                {
                    var packet = tcpEvent.Data;
                    var position = 3;
                    var players = new List<PlayerInfo>();
                    var playersCount = packet.Buffer[position++];
                    for (byte i = 0; i < playersCount; i++)
                    {
                        var p = new PlayerInfo();

                        p.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                        position += packet.Buffer[position];
                        position++;

                        p.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                        position += packet.Buffer[position];
                        position++;

                        p.Level = packet.Buffer[position++];
                        p.Modifiers = packet.Buffer[position++];
                        players.Add(p);
                    }
                    return players;
                })
                .Subscribe(updatedPlayers =>
                {
                    UpdatePlayers(updatedPlayers);
                });
        }

        private void UpdatePlayers(List<PlayerInfo> updatedPlayers)
        {
            var indexesToDelete = new List<int>();
            for (var i = 0; i < _viewModel.Players.Count; i++)
            {
                if (!updatedPlayers.Any(p => p.Id == _viewModel.Players[i].Id))
                {
                    indexesToDelete.Add(i);
                }
            }
            indexesToDelete.ForEach(i => _viewModel.Players.RemoveAt(i));

            foreach (var updatedPlayer in updatedPlayers)
            {
                var p = _viewModel.Players.FirstOrDefault(pl => pl.Id == updatedPlayer.Id);
                if (p != null)
                {
                    p.Name = updatedPlayer.Name;
                    p.Level = updatedPlayer.Level;
                    p.Modifiers = updatedPlayer.Modifiers;
                }
                else
                {
                    _viewModel.Players.Add(new Player
                    {
                        Id = updatedPlayer.Id,
                        Name = updatedPlayer.Name,
                        Level = updatedPlayer.Level,
                        Modifiers = updatedPlayer.Modifiers
                    });
                }

            }
        }
    }
}