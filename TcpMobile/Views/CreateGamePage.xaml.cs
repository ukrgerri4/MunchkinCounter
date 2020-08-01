using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Microsoft.Extensions.Configuration;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
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

        public List<Player> AllPlayers =>
                _gameClient.Players
                    .OrderByDescending(p => p.Level)
                    .ThenByDescending(p => p.Modifiers)
                    .ThenBy(p => p.Name)
                    .ToList();

        private bool _creatingGame = true;
        private bool _waitingPlayers = false;

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
                    }

                    OnPropertyChanged(nameof(WaitingPlayers));
                }
            }
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateGamePage : ContentPage
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGameLogger _gameLogger;
        private readonly IGameClient _gameClient;
        private readonly IGameServer _gameServer;

        public CreateGameViewModel ViewModel { get; set; }

        public CreateGamePage(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            IGameLogger gameLogger,
            IGameServer gameServer,
            IGameClient gameClient
        )
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _gameLogger = gameLogger;
            _gameClient = gameClient;
            _gameServer = gameServer;

            InitializeComponent();

            ViewModel = new CreateGameViewModel(_gameClient, _gameServer);

            TrySetPlayerDefaults();

            BindingContext = ViewModel;

            MessagingCenter.Subscribe<IGameClient>(this, "PlayersUpdated", (sender) => {
                ViewModel.OnPropertyChanged(nameof(ViewModel.AllPlayers));
            });

            MessagingCenter.Subscribe<MenuPage>(this, "EndGame", (sender) => StopGame());
        }

        private async void TryCreate(object sender, EventArgs args)
        {
            try
            {
                _gameServer.Start();

                _gameClient.StartUpdatePlayers();
                _gameClient.ConnectSelf();
                _gameClient.SendPlayerInfo();
                _gameClient.StartListeningServerDisconnection();

                SavePlayerDefaults();

                ViewModel.WaitingPlayers = true;
            }
            catch (Exception e)
            {
                await PopupNavigation.Instance.PushAsync(new AlertPage("Create game error, please check your lan connection."));
                
                _ = _gameServer.Stop();
                _ = _gameClient.CloseConnection();
                
                _gameLogger.Error($"Create game error: {e.Message}");
            }
        }

        private void TryStart(object sender, EventArgs args)
        {
            _gameServer.StopBroadcast();
            MessagingCenter.Send(this, "StartGame");
        }

        private void TryStop(object sender, EventArgs args)
        {
            if (!ViewModel.WaitingPlayers) { return; }

            StopGame();
        }

        public void StopGame()
        {
            _gameServer.Stop();

            ViewModel.CreatingGame = true;
        }

        private void SavePlayerDefaults()
        {
            Preferences.Set(PreferencesKey.DefaultGameName, ViewModel.Host.Name);
            Preferences.Set(PreferencesKey.DefaultPlayerName, ViewModel.MyPlayer.Name);
            Preferences.Set(PreferencesKey.DefaultPlayerSex, ViewModel.MyPlayer.Sex);
        }

        private void TrySetPlayerDefaults()
        {
            var defGameName = Preferences.Get(PreferencesKey.DefaultGameName, null);
            if (!string.IsNullOrWhiteSpace(defGameName))
            {
                ViewModel.Host.Name = defGameName;
            }
            var defPlayerName = Preferences.Get(PreferencesKey.DefaultPlayerName, null);
            if (!string.IsNullOrWhiteSpace(defGameName))
            {
                ViewModel.MyPlayer.Name = defPlayerName;
            }
            var defPlayerSex = Preferences.Get(PreferencesKey.DefaultPlayerSex, -1);
            if (defPlayerSex >= 0)
            {
                ViewModel.MyPlayer.Sex = (byte)defPlayerSex;
            }
            
            
        }
    }
}