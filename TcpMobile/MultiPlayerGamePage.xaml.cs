using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using TcpMobile.Services;
using TcpMobile.Tcp.Enums;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    public class MultiPlayerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));


        private bool _isMenuMode;
        public bool IsMenuMode
        {
            get => _isMenuMode;
            set
            {
                _isMenuMode = value;
                OnPropertyChanged(nameof(IsMenuMode));
            }
        }
        private bool _isServerMode = false;
        public bool IsServerMode
        {
            get => _isServerMode;
            set
            {
                _isServerMode = value;
                OnPropertyChanged(nameof(IsServerMode));
            }
        }
        private bool _isClientMode;
        public bool IsClientMode
        {
            get => _isClientMode;
            set
            {
                _isClientMode = value;
                OnPropertyChanged(nameof(IsClientMode));
            }
        }

        private bool _isClientManualMode;
        public bool IsClientManualMode
        {
            get => _isClientManualMode;
            set
            {
                _isClientManualMode = value;
                OnPropertyChanged(nameof(IsClientManualMode));
            }
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MultiPlayerGamePage : ContentPage
    {
        private readonly IConfiguration _configuration;
        private readonly IMultiPlayerService<Player> _multiPlayerService;
        private readonly IGameClient _gameClient;
        private readonly IGameServer _gameServer;
        private readonly ServerPlayersData _serverPlayersData;
        public Player player;

        public MultiPlayerViewModel multiPlayerViewModel;

        public MultiPlayerGamePage(IConfiguration configuration,
            IMultiPlayerService<Player> multiPlayerService,
            IGameClient gameClient,
            IGameServer gameServer,
            ServerPlayersData serverPlayersData)
        {
            _configuration = configuration;
            _multiPlayerService = multiPlayerService;
            _gameClient = gameClient;
            _gameServer = gameServer;
            _serverPlayersData = serverPlayersData;
            InitializeComponent();

            player = new Player();
            player.Id = _configuration["DeviceId"];
            player.Name = "гн.Костин";

            multiPlayerViewModel = new MultiPlayerViewModel
            {
                IsMenuMode = true,
                IsServerMode = false,
                IsClientMode = false,
                IsClientManualMode = false
            };
            var menuVisibleBinding = new Binding { Source = multiPlayerViewModel, Path = "IsMenuMode" };
            menuLayout.SetBinding(StackLayout.IsVisibleProperty, menuVisibleBinding);

            var serverVisibleBinding = new Binding { Source = multiPlayerViewModel, Path = "IsServerMode" };
            serverLayout.SetBinding(StackLayout.IsVisibleProperty, serverVisibleBinding);

            clientManualrLayout.SetBinding(IsVisibleProperty, new Binding { Source = multiPlayerViewModel, Path = "IsClientManualMode" });

            var userLevelBinding = new Binding { Source = player, Path = "Level" };
            userLevelLabel.SetBinding(Label.TextProperty, userLevelBinding);

            var userModifyersBinding = new Binding { Source = player, Path = "Modifiers" };
            userModifyersLabel.SetBinding(Label.TextProperty, userModifyersBinding);

            var userPowerBinding = new Binding { Source = player, Path = "Power" };
            userPowerLabel.SetBinding(Label.TextProperty, userPowerBinding);


            //for(int i = 0; i < 6; i++)
            //{
            //    _multiPlayerService.Players.Add(new Player { Id = $"ID[{i}]", Name = $"N_a_m_e - [{i}]"});
            //}

            lanPlayersView.ItemsSource = _multiPlayerService.Players;
            lanPlayersView.ItemTemplate = new DataTemplate(() => {
                var idLabel = new Label();
                idLabel.SetBinding(Label.TextProperty, "Id");

                var nameLabel = new Label();
                nameLabel.SetBinding(Label.TextProperty, "Name");

                var levelLabel = new Label();
                levelLabel.SetBinding(Label.TextProperty, "Level");

                var modifyersLabel = new Label();
                modifyersLabel.SetBinding(Label.TextProperty, "Modifiers");

                var powerLabel = new Label();
                powerLabel.SetBinding(Label.TextProperty, "Power");

                return new ViewCell
                {
                    View = new StackLayout
                    {
                        Padding = new Thickness(0, 5),
                        Orientation = StackOrientation.Horizontal,
                        Children = { idLabel, nameLabel, levelLabel, modifyersLabel, powerLabel }
                    }
                };
            });
        }

        private void IncreaseLevel(object sender, EventArgs e)
        {
            if (player.Level < 10)
            {
                player.Level++;
                UpdatePlayerStateOnServer(player);
                //lanPlayers.ForEach(p => p.Level++);
            }

        }

        private void DecreaseLevel(object sender, EventArgs e)
        {
            if (player.Level > 1)
            {
                player.Level--;
                UpdatePlayerStateOnServer(player);
            }
        }

        private void UpdatePlayerStateOnServer(Player player)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                memoryStream.WriteByte((byte)MunchkinMessageType.UpdatePlayerState);
                memoryStream.WriteByte(player.Level);
                memoryStream.WriteByte(player.Modifiers);

                memoryStream.WriteByte(10);
                memoryStream.WriteByte(4);

                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                memoryStream.Seek(0, SeekOrigin.End);
                var idMessageResult = _gameClient.SendMessage(memoryStream.ToArray());
            }
        }

        private void IncreaseModifiers(object sender, EventArgs e)
        {
            if (player.Modifiers < 255)
            {
                player.Modifiers++;
            }
        }

        private void DecreaseModifiers(object sender, EventArgs e)
        {
            if (player.Modifiers > 0)
            {
                player.Modifiers--;
            }
        }

        private void BackToMenu(object sender, EventArgs e)
        {
            multiPlayerViewModel.IsMenuMode = true;
            multiPlayerViewModel.IsServerMode = false;
            multiPlayerViewModel.IsClientMode = false;
            multiPlayerViewModel.IsClientManualMode = false;
        }

        bool started = false;
        private void OpenRoom(object sender, EventArgs e)
        {
            multiPlayerViewModel.IsMenuMode = false;
            multiPlayerViewModel.IsServerMode = true;
            multiPlayerViewModel.IsClientMode = false;
            multiPlayerViewModel.IsClientManualMode = false;

            if (!started)
            {
                _gameServer.Start();
                started = true;
            }
            //_multiPlayerService.Players.Add(player);

            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());

            Console.WriteLine(string.Join(",", hostAddresses.Select(ip => ip.ToString())));

            //_gameServer.StartBroadcast();
        }

        private void JoinRoom(object sender, EventArgs e)
        {
            multiPlayerViewModel.IsMenuMode = false;
            multiPlayerViewModel.IsServerMode = false;
            multiPlayerViewModel.IsClientMode = true;
            multiPlayerViewModel.IsClientManualMode = false;
        }

        private void JoinRoomManual(object sender, EventArgs e)
        {
            multiPlayerViewModel.IsMenuMode = false;
            multiPlayerViewModel.IsServerMode = false;
            multiPlayerViewModel.IsClientMode = false;
            multiPlayerViewModel.IsClientManualMode = true;
        }

        private void TryConnect(object sender, EventArgs e)
        {
            var ipValue = ipEntry.Text;
            var portValue = portEntry.Text;

            var ip = IPAddress.Parse(ipValue);
            var port = Convert.ToInt32(portValue);

            var connectResult = _gameClient.Connect(ip, port);
            if (connectResult.IsFail) { return; }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                memoryStream.WriteByte((byte)MunchkinMessageType.GetId);

                var byteId = Encoding.UTF8.GetBytes(player.Id ?? string.Empty);
                memoryStream.WriteByte((byte)byteId.Length);
                memoryStream.Write(byteId, 0, byteId.Length);

                memoryStream.WriteByte(10);
                memoryStream.WriteByte(4);

                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                memoryStream.Seek(0, SeekOrigin.End);
                var idMessageResult = _gameClient.SendMessage(memoryStream.ToArray());
                if (idMessageResult.IsFail) { return; }
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                memoryStream.WriteByte((byte)MunchkinMessageType.InitInfo);

                var byteId = Encoding.UTF8.GetBytes(player.Id ?? string.Empty);
                memoryStream.WriteByte((byte)byteId.Length);
                memoryStream.Write(byteId, 0, byteId.Length);

                var byteName = Encoding.UTF8.GetBytes(player.Name ?? string.Empty);
                memoryStream.WriteByte((byte)byteName.Length);
                memoryStream.Write(byteName, 0, byteName.Length);

                memoryStream.WriteByte(player.Level);
                memoryStream.WriteByte(player.Modifiers);

                memoryStream.WriteByte(10);
                memoryStream.WriteByte(4);

                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                memoryStream.Seek(0, SeekOrigin.End);
                var initMessageResult = _gameClient.SendMessage(memoryStream.ToArray());
                if (initMessageResult.IsFail) { return; }
            }

            _multiPlayerService.StartUpdatePlayers();
        }
        private void TryDisconnect(object sender, EventArgs e)
        {
            _gameClient.Disconnect();
        }
    }
}