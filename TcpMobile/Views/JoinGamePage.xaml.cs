using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using MunchkinCounterLan.Models;
using MunchkinCounterLan.Views;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
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

        private Subject<PageEventType> _innerSubject;
        private Subject<Unit> _destroy = new Subject<Unit>();
        public ICommand ToolsClick { get; set; }
        private bool _toolsClickHandling = false;

        public JoinGameViewModel ViewModel { get; set; }

        public bool _searching = false;

        public JoinGamePage(IServiceProvider serviceProvider, IGameLogger gameLogger, IGameClient gameClient)
        {
            _serviceProvider = serviceProvider;
            _gameLogger = gameLogger;
            _gameClient = gameClient;

            InitializeComponent();

            _innerSubject = new Subject<PageEventType>();
            ToolsClick = new Command<PageEventType>((eventType) => _innerSubject.OnNext(eventType));

            Appearing += (s, e) =>
            {
                StartSearching();

                _innerSubject.AsObservable()
                    .TakeUntil(_destroy)
                    .Where(_ => !_toolsClickHandling)
                    .Where(eventType => eventType == PageEventType.ResetMunchkin || eventType == PageEventType.ThrowDice)
                    .Do(_ => _toolsClickHandling = true)
                    .Subscribe(async eventType => {
                        switch (eventType)
                        {
                            case PageEventType.ResetMunchkin:
                                await ResetMunchkinHandler();
                                break;
                            case PageEventType.ThrowDice:
                                await PopupNavigation.Instance.PushAsync(new DicePage());
                                break;
                        }

                        _toolsClickHandling = false;
                    });
            };

            Disappearing += (s, e) =>
            {
                StopSearching();
                _destroy.OnNext(Unit.Default);
            };

            ViewModel = new JoinGameViewModel(_gameClient);
            ViewModel.MyPlayer.PropertyChanged += (s,e) => _gameClient.SendUpdatedPlayerState();

            BindingContext = ViewModel;

            MessagingCenter.Subscribe<IGameClient>(this, "HostsUpdated", (sender) => {
                ViewModel.OnPropertyChanged(nameof(ViewModel.ConnectionsExists));
            });

            MessagingCenter.Subscribe<IGameClient>(this, "PlayersUpdated", (sender) => {
                ViewModel.OnPropertyChanged(nameof(ViewModel.LanPlayers));
            });

            MessagingCenter.Subscribe<SettingsViewModel>(this, "SettingsChanged", (sender) =>
            {
                ViewModel.OnPropertyChanged(nameof(ViewModel.LanPlayers));
            });

            MessagingCenter.Subscribe<IGameClient>(this, "LostServerConnection", async (sender) => {
                if (ViewModel.HostSearch) { return; }

                ViewModel.OnPropertyChanged(nameof(ViewModel.LanPlayers));
                ExitGame();

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
            

            MessagingCenter.Subscribe<MenuPage>(this, "EndGame",(sender) => ExitGame());
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
                    ViewModel.MyPlayer.Name = ev.Name;
                    ViewModel.MyPlayer.Sex = ev.Sex;

                    Preferences.Set(PreferencesKey.DefaultPlayerName, ev.Name);
                    Preferences.Set(PreferencesKey.DefaultPlayerSex, ev.Sex);

                    _gameClient.Connect(munchkinHost.IpAddress);
                    _gameClient.StartUpdatePlayers();
                    _gameClient.SendPlayerInfo();
                    _gameClient.StartListeningServerDisconnection();
                                        
                    Preferences.Set(PreferencesKey.LastConnectedHostIp, munchkinHost.IpAddress.ToString());

                    StopSearching();
                    ViewModel.Process = true;
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
                ViewModel.Process = true;
                return true;
            }
            return false;
        }

        private async Task ResetMunchkinHandler()
        {
            var confirmPage = new ConfirmPage();
            confirmPage.OnReset += (s, ev) => {
                switch (ev)
                {
                    case "level":
                        ViewModel.MyPlayer.ResetLevel();
                        break;
                    case "modifiers":
                        ViewModel.MyPlayer.ResetModifyers();
                        break;
                    case "all":
                        ViewModel.MyPlayer.ResetLevel();
                        ViewModel.MyPlayer.ResetModifyers();
                        break;
                }
                _gameClient.SendUpdatedPlayerState();
            };

            await PopupNavigation.Instance.PushAsync(confirmPage);
        }

        private void StartSearching()
        {
            if (!_searching && ViewModel.HostSearch)
            {
                _gameClient.StartSearchHosts();
                _searching = true;

                Device.StartTimer(TimeSpan.FromMilliseconds(600), () =>
                {
                    ViewModel.LoaderPointsCount = ViewModel.LoaderPointsCount < 4 ? ViewModel.LoaderPointsCount + 1 : 0;
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

        public void ExitGame()
        {
            if (ViewModel.HostSearch) { return; }

            _gameClient.CloseConnection();

            ViewModel.HostSearch = true;
        }
    }
}