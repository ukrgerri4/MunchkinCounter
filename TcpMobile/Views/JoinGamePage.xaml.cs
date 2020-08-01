using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TcpMobile.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    public class JoinGameViewModel : INotifyPropertyChanged
    {
        private readonly IGameClient _gameClient;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public JoinGameViewModel(IGameClient gameClient)
        {
            _gameClient = gameClient;
        }

        public ObservableCollection<MunchkinHost> Hosts => _gameClient.Hosts;
        public Player MyPlayer => _gameClient.MyPlayer;

        public List<Player> LanPlayers
        {
            get
            {
                if (Preferences.Get(PreferencesKey.ShowSelfMunchkinInLanGame, true))
                {
                    return _gameClient.Players
                        .OrderByDescending(p => p.Level)
                        .ThenByDescending(p => p.Modifiers)
                        .ThenBy(p => p.Name)
                        .ToList();
                }
                else
                {
                    return _gameClient.Players
                        .Where(p => p.Id != _gameClient.MyPlayer.Id)
                        .OrderByDescending(p => p.Level)
                        .ThenByDescending(p => p.Modifiers)
                        .ThenBy(p => p.Name)
                        .ToList();
                }
            }
        }

        private bool _hostSearch = true;
        public bool HostSearch
        {
            get => _hostSearch;
            set
            {
                if (_hostSearch != value)
                {
                    _hostSearch = value;
                    if (_hostSearch)
                    {
                        Process = false;
                    }

                    OnPropertyChanged(nameof(HostSearch));
                }
            }
        }

        private bool _process = false;
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
                        HostSearch = false;
                    }

                    OnPropertyChanged(nameof(Process));
                }
            }
        }

        public bool ConnectionsExists
        {
            get => _hostSearch && Hosts.Any();
        }

        public int _loaderPointsCount = 0;
        public int LoaderPointsCount
        {
            get => _loaderPointsCount;
            set
            {
                if (_loaderPointsCount != value)
                {
                    _loaderPointsCount = value;
                    OnPropertyChanged(nameof(LoaderPointsValue));
                }
            }
        }

        public string LoaderPointsValue
        {
            get
            {
                switch (LoaderPointsCount)
                {
                    case 0:
                        return "";
                    case 1:
                        return ".";
                    case 2:
                        return "..";
                    case 3:
                        return "...";
                    case 4:
                        return "....";
                    default:
                        return "";
                }
            }
        } 
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class JoinGamePage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGameLogger _gameLogger;
        private readonly IGameClient _gameClient;

        private JoinGameViewModel _viewModel;

        public bool _searching = false;

        public JoinGamePage(IServiceProvider serviceProvider, IGameLogger gameLogger, IGameClient gameClient)
        {
            _serviceProvider = serviceProvider;
            _gameLogger = gameLogger;
            _gameClient = gameClient;

            InitializeComponent();

            Appearing += (s, e) => StartSearching();
            Disappearing += (s, e) => StopSearching();

            _viewModel = new JoinGameViewModel(_gameClient);
            _viewModel.MyPlayer.PropertyChanged += (s,e) => _gameClient.SendUpdatedPlayerState();

            BindingContext = _viewModel;

            MessagingCenter.Subscribe<IGameClient>(this, "HostsUpdated", (sender) => {
                _viewModel.OnPropertyChanged(nameof(_viewModel.ConnectionsExists));
            });

            MessagingCenter.Subscribe<IGameClient>(this, "PlayersUpdated", (sender) => {
                _viewModel.OnPropertyChanged(nameof(_viewModel.LanPlayers));
            });

            MessagingCenter.Subscribe<SettingsViewModel>(this, "SettingsChanged", (sender) =>
            {
                _viewModel.OnPropertyChanged(nameof(_viewModel.LanPlayers));
            });

            MessagingCenter.Subscribe<IGameClient>(this, "LostServerConnection", async (sender) => {
                _viewModel.OnPropertyChanged(nameof(_viewModel.LanPlayers));
                StopProcess();

                var alert = new AlertPage("Connection to server lost.", "TryReconnect", "Exit");
                alert.Confirmed += async (s, e) =>
                {
                    var reconnectResult = TryReconnectToLastHost();
                    if (!reconnectResult)
                    {
                        await PopupNavigation.Instance.PushAsync(new AlertPage("Reconnect to host failed, search for new host."));
                        StartSearching();
                    }
                };
                await PopupNavigation.Instance.PushAsync(alert);
            });
            

            MessagingCenter.Subscribe<MenuPage>(this, "EndGame",(sender) => StopProcess());
        }

        private async void Connect(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null && e.Item is MunchkinHost munchkinHost && munchkinHost?.IpAddress != null)
            {
                var enterPlayerDataPage = new EnterPlayerDataPage();
                enterPlayerDataPage.Name = Preferences.Get(PreferencesKey.DefaultPlayerName, string.Empty);
                enterPlayerDataPage.Sex = (byte)Preferences.Get(PreferencesKey.DefaultPlayerSex, 0);
                enterPlayerDataPage.OnNextPressed += (se, ev) =>
                {
                    _viewModel.MyPlayer.Name = ev.Name;
                    _viewModel.MyPlayer.Sex = ev.Sex;

                    Preferences.Set(PreferencesKey.DefaultPlayerName, ev.Name);
                    Preferences.Set(PreferencesKey.DefaultPlayerSex, ev.Sex);

                    _gameClient.Connect(munchkinHost.IpAddress);
                    _gameClient.StartUpdatePlayers();
                    _gameClient.SendPlayerInfo();
                    _gameClient.StartListeningServerDisconnection();
                                        
                    Preferences.Set(PreferencesKey.LastConnectedHostIp, munchkinHost.IpAddress.ToString());

                    StopSearching();
                    _viewModel.Process = true;
                };
                await PopupNavigation.Instance.PushAsync(enterPlayerDataPage);
            }
        }

        private async void Reconnect(object sender, EventArgs e)
        {
            var reconnectResult = TryReconnectToLastHost();
            if (!reconnectResult)
            {
                await PopupNavigation.Instance.PushAsync(new AlertPage("Reconnect to host failed, search for new host."));
                return;
            }
        }

        private bool TryReconnectToLastHost()
        {
            var lastHostIp = Preferences.Get(PreferencesKey.LastConnectedHostIp, null);
            if (lastHostIp != null)
            {
                var ipAdress = IPAddress.Parse(lastHostIp);

                var connectResult =  _gameClient.Connect(ipAdress);
                if (connectResult.IsFail) { return false; }

                _gameClient.StartUpdatePlayers();
                _gameClient.SendPlayerInfo();
                _gameClient.StartListeningServerDisconnection();

                StopSearching();
                _viewModel.Process = true;
                return true;
            }
            return false;
        }

        private async void ResetMunchkin(object sender, EventArgs e)
        {
            var confirmPage = new ConfirmPage();
            confirmPage.OnReset += (s, ev) => {
                switch (ev)
                {
                    case "level":
                        _viewModel.MyPlayer.ResetLevel();
                        break;
                    case "modifiers":
                        _viewModel.MyPlayer.ResetModifyers();
                        break;
                    case "all":
                        _viewModel.MyPlayer.ResetLevel();
                        _viewModel.MyPlayer.ResetModifyers();
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

        private void StartSearching()
        {
            if (!_searching && _viewModel.HostSearch)
            {
                _gameClient.StartSearchHosts();
                _searching = true;

                Device.StartTimer(TimeSpan.FromMilliseconds(600), () =>
                {
                    _viewModel.LoaderPointsCount = _viewModel.LoaderPointsCount < 4 ? _viewModel.LoaderPointsCount + 1 : 0;
                    return _searching;
                });
            }
        }

        private void StopSearching()
        {
            _gameClient.StopSearchHosts();

            _gameClient.Hosts.Clear();

            _searching = false;
        }

        public void StopProcess()
        {
            if (_viewModel.HostSearch) { return; }

            _gameClient.CloseConnection();

            _viewModel.HostSearch = true;
        }
    }
}