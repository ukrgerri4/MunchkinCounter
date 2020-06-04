using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Text;
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
        public Player player;
        public ObservableCollection<Player> lanPlayers;

        public MultiPlayerViewModel multiPlayerViewModel;

        public MultiPlayerGamePage(IConfiguration configuration,
            IMultiPlayerService<Player> multiPlayerService,
            IGameClient gameClient,
            IGameServer gameServer)
        {
            _configuration = configuration;
            _multiPlayerService = multiPlayerService;
            _gameClient = gameClient;
            _gameServer = gameServer;
            InitializeComponent();

            player = new Player();
            player.Id = _configuration["DeviceId"];
            player.Name = "гн.Костин";

            lanPlayers = _multiPlayerService.GetPlayers();

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


            for(int i = 0; i < 6; i++)
            {
                lanPlayers.Add(new Player { Id = $"ID[{i}]", Name = $"N_a_m_e - [{i}]"});
            }

            lanPlayersView.ItemsSource = lanPlayers;
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

        private void OpenRoom(object sender, EventArgs e)
        {
            multiPlayerViewModel.IsMenuMode = false;
            multiPlayerViewModel.IsServerMode = true;
            multiPlayerViewModel.IsClientMode = false;
            multiPlayerViewModel.IsClientManualMode = false;

            _gameServer.Start();
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

                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                memoryStream.Seek(0, SeekOrigin.End);
                var initMessageResult = _gameClient.SendMessage(memoryStream.ToArray());
                if (initMessageResult.IsFail) { return; }
            }

            _multiPlayerService.StartUpdatePlayers();
        }
    }
}