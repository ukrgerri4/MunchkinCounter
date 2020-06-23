using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    public class CreateGameViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private readonly IGameClient _gameClient;
        private readonly IGameServer _gameServer;
        public CreateGameViewModel(IGameClient gameClient, IGameServer gameServer)
        {
            _gameClient = gameClient;
            _gameServer = gameServer;
        }

        public MunchkinHost Host => _gameServer.Host;
        public Player MyPlayer => _gameClient.MyPlayer;
        public ObservableCollection<Player> AllPlayers => new ObservableCollection<Player>(_gameClient.Players);
        public ObservableCollection<Player> ExeptMePlayers => new ObservableCollection<Player>(_gameClient.Players.Where(p => p.Id != _gameClient.MyPlayer.Id));

        private bool _creatingGame = true;
        private bool _waitingPlayers = false;
        private bool _process = false;

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
        private readonly IConfiguration _configuration;
        private readonly IGameLogger _gameLogger;
        private readonly IGameClient _gameClient;
        private readonly IGameServer _gameServer;

        private CreateGameViewModel _viewModel;

        public CreateGamePage(
            IConfiguration configuration,
            IGameLogger gameLogger,
            IGameServer gameServer,
            IGameClient gameClient
        )
        {
            _configuration = configuration;
            _gameLogger = gameLogger;
            _gameClient = gameClient;
            _gameServer = gameServer;

            InitializeComponent();

            _viewModel = new CreateGameViewModel(_gameClient, _gameServer);
            //{
            //    Players = _gameClient.Players,
            //    Host = _gameServer.Host,
            //    MyPlayer = _gameClient.MyPlayer,
            //    CreatingGame = true
            //};

            BindingContext = _viewModel;

            MessagingCenter.Subscribe<IGameClient>(
                this,
                "PlayersUpdated",
                (sender) => {
                    _viewModel.OnPropertyChanged(nameof(_viewModel.AllPlayers));
                    _viewModel.OnPropertyChanged(nameof(_viewModel.ExeptMePlayers));
                });
        }

        private async void TryCreate(object sender, EventArgs args)
        {
            try
            {
                _gameServer.Start();

                _gameClient.StartUpdatePlayers();
                _gameClient.ConnectSelf();
                _gameClient.SendPlayerInfo();

                _viewModel.WaitingPlayers = true;
            }
            catch (Exception e)
            {
                await DisplayAlert("Create game error:", "Please check your lan connection.", "Ok");
                
                var serverStopResult = _gameServer.Stop();
                var clientStopResult = _gameClient.Stop();
                
                _gameLogger.Error($"Create game error: {e.Message}");
            }
        }

        private async void TryStart(object sender, EventArgs args)
        {
            var confirm = await DisplayAlert("Create new game", "Are you shure want to start new game?", "Yes", "No");
            if (confirm)
            {
                var stopResult = _gameServer.StopBroadcast();

                if (stopResult.IsFail) { _gameLogger.Error(stopResult.Error); }

                _viewModel.Process = true;
            }
        }

        private async void TryStop(object sender, EventArgs args)
        {
            var confirm = await DisplayAlert("Stop game!", "stopping the game will entail disconnecting players", "Yes", "No");
            if (confirm)
            {
                var stopResult = _gameServer.Stop();

                if (stopResult.IsFail) { _gameLogger.Error(stopResult.Error); }

                _gameClient.Players.Clear();

                _viewModel.CreatingGame = true;
            }
        }

        private void IncreaseLevel(object sender, EventArgs e)
        {
            if (_gameClient.MyPlayer.Level < 10)
            {
                _gameClient.MyPlayer.Level++;
                _gameClient.SendUpdatedPlayerState();
            }

        }

        private void DecreaseLevel(object sender, EventArgs e)
        {
            if (_gameClient.MyPlayer.Level > 1)
            {
                _gameClient.MyPlayer.Level--;
                _gameClient.SendUpdatedPlayerState();
            }
        }

        private void IncreaseModifiers(object sender, EventArgs e)
        {
            if (_gameClient.MyPlayer.Modifiers < 255)
            {
                _gameClient.MyPlayer.Modifiers++;
                _gameClient.SendUpdatedPlayerState();
            }
        }

        private void DecreaseModifiers(object sender, EventArgs e)
        {
            if (_gameClient.MyPlayer.Modifiers > 0)
            {
                _gameClient.MyPlayer.Modifiers--;
                _gameClient.SendUpdatedPlayerState();
            }
        }
    }
}