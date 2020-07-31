using GameMunchkin.Models;
using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Microsoft.Extensions.Configuration;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly IGameLogger _gameLogger;
        private readonly IGameClient _gameClient;
        private readonly IGameServer _gameServer;

        private CreateGameViewModel _viewModel;

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

            _viewModel = new CreateGameViewModel(_gameClient, _gameServer);
            TrySetPlayerDefaults();

            BindingContext = _viewModel;

            MessagingCenter.Subscribe<IGameClient>(
                this,
                "PlayersUpdated",
                (sender) => {
                    _viewModel.OnPropertyChanged(nameof(_viewModel.AllPlayers));
                    _viewModel.OnPropertyChanged(nameof(_viewModel.ExeptMePlayers));
                }
            );

            MessagingCenter.Subscribe<MenuPage>(
                this,
                "EndGame",
                async (sender) =>
                {
                    await Stop();
                }
            );
        }

        private async void TryCreate(object sender, EventArgs args)
        {
            try
            {
                _gameServer.Start();

                _gameClient.StartUpdatePlayers();
                _gameClient.ConnectSelf();
                _gameClient.SendPlayerInfo();

                SavePlayerDefaults();

                _viewModel.WaitingPlayers = true;
            }
            catch (Exception e)
            {
                await PopupNavigation.Instance.PushAsync(new AlertPage("Create game error, please check your lan connection."));
                
                var serverStopResult = _gameServer.Stop();
                var clientStopResult = _gameClient.Stop();
                
                _gameLogger.Error($"Create game error: {e.Message}");
            }
        }

        private void TryStart(object sender, EventArgs args)
        {
            var stopResult = _gameServer.StopBroadcast();

            if (stopResult.IsFail) { _gameLogger.Error(stopResult.Error); }

            _viewModel.Process = true;
        }

        private async void TryStop(object sender, EventArgs args)
        {
            await Stop();
        }

        public async Task Stop()
        {
            if (_viewModel.CreatingGame) { return; }

            //var alert = new AlertPage("Stopping the game will entail disconnecting players.", "Ok", "Cancel");
            //alert.OnConfirm += (sender, e) =>
            //{
            //    var stopResult = _gameServer.Stop();

            //    if (stopResult.IsFail) { _gameLogger.Error(stopResult.Error); }

            //    _gameClient.Players.Clear();

            //    _viewModel.CreatingGame = true;
            //};
            //await PopupNavigation.Instance.PushAsync(alert);

            _gameServer.Stop();
            
            _gameClient.StopSearchHosts();
            _gameClient.Stop();
            _gameClient.Players.Clear();

            _viewModel.CreatingGame = true;
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

        private void ToggleSex(object sender, EventArgs e)
        {
            _gameClient.MyPlayer.Sex = _gameClient.MyPlayer.Sex == 1 ? (byte)0 : (byte)1;
            _gameClient.SendUpdatedPlayerState();
        }

        private async void ResetMunchkin(object sender, EventArgs e)
        {
            var confirmPage = new ConfirmPage();
            confirmPage.OnReset += (s, ev) => {
                switch (ev)
                {
                    case "level":
                        _viewModel.MyPlayer.Level = 1;
                        break;
                    case "modifiers":
                        _viewModel.MyPlayer.Modifiers = 0;
                        break;
                    case "all":
                        _viewModel.MyPlayer.Level = 1;
                        _viewModel.MyPlayer.Modifiers = 0;
                        break;
                }
                _gameClient.SendUpdatedPlayerState();
            };

            await PopupNavigation.Instance.PushAsync(confirmPage);
        }

        private async void ThrowDice(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(_serviceProvider.GetService<DicePage>());
        }

        private void SavePlayerDefaults()
        {
            Preferences.Set(PreferencesKey.DefaultGameName, _viewModel.Host.Name);
            Preferences.Set(PreferencesKey.DefaultPlayerName, _viewModel.MyPlayer.Name);
            Preferences.Set(PreferencesKey.DefaultPlayerSex, _viewModel.MyPlayer.Sex);
        }

        private void TrySetPlayerDefaults()
        {
            var defGameName = Preferences.Get(PreferencesKey.DefaultGameName, null);
            if (!string.IsNullOrWhiteSpace(defGameName))
            {
                _viewModel.Host.Name = defGameName;
            }
            var defPlayerName = Preferences.Get(PreferencesKey.DefaultPlayerName, null);
            if (!string.IsNullOrWhiteSpace(defGameName))
            {
                _viewModel.MyPlayer.Name = defPlayerName;
            }
            var defPlayerSex = Preferences.Get(PreferencesKey.DefaultPlayerSex, -1);
            if (defPlayerSex >= 0)
            {
                _viewModel.MyPlayer.Sex = (byte)defPlayerSex;
            }
            
            
        }
    }
}