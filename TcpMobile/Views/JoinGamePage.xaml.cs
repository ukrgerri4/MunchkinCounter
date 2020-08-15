using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    public class JoinGameViewModel : INotifyPropertyChanged
    {
        private IGameClient _gameClient => DependencyService.Get<IGameClient>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public JoinGameViewModel()
        {
            _gameClient.Hosts.CollectionChanged += (s, e) => OnPropertyChanged(nameof(ConnectionsExists));
        }

        public ObservableCollection<MunchkinHost> Hosts => _gameClient.Hosts;
        public Player MyPlayer => _gameClient.MyPlayer;

        public bool ConnectionsExists
        {
            get => Hosts.Any();
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
        private IGameClient _gameClient => DependencyService.Get<IGameClient>();
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();

        public JoinGameViewModel ViewModel { get; set; }

        public bool _searching = false;

        
        private bool _connecting = false;
        public bool Connecting
        { 
            get => _connecting; 
            set
            {
                if (_connecting != value)
                {
                    _connecting = value;
                    OnPropertyChanged(nameof(Connecting));
                }
            }
        }

        public JoinGamePage()
        {
            InitializeComponent();

            ViewModel = new JoinGameViewModel();

            Appearing += (s, e) => StartSearching();

            Disappearing += (s, e) => StopSearching();
            
            BindingContext = ViewModel;
        }

        private async void Connect(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null && e.Item is MunchkinHost munchkinHost && munchkinHost?.IpAddress != null)
            {
                var enterPlayerDataPage = new EnterPlayerDataPage();
                enterPlayerDataPage.Name = Preferences.Get(PreferencesKey.DefaultPlayerName, string.Empty);
                enterPlayerDataPage.Sex = (byte)Preferences.Get(PreferencesKey.DefaultPlayerSex, 0);
                enterPlayerDataPage.OnNextPressed += async (s, ev) =>
                {
                    ViewModel.MyPlayer.Name = ev.Name;
                    ViewModel.MyPlayer.Sex = ev.Sex;

                    Preferences.Set(PreferencesKey.DefaultPlayerName, ev.Name);
                    Preferences.Set(PreferencesKey.DefaultPlayerSex, ev.Sex);

                    Connecting = true;
                    var connectResult = _gameClient.Connect(munchkinHost.IpAddress);
                    Connecting = false;

                    if (connectResult.IsFail)
                    {
                        _gameLogger.Error(connectResult.Error);
                        await PopupNavigation.Instance.PushAsync(new AlertPage("Join game error, please check your lan connection."));
                        return;
                    }

                    _gameClient.StartUpdatePlayers();
                    _gameClient.SendPlayerInfo();
                    _gameClient.StartListeningServerDisconnection();
                                        
                    Preferences.Set(PreferencesKey.LastConnectedHostIp, munchkinHost.IpAddress.ToString());

                    StopSearching();
                    await Shell.Current.GoToAsync($"gameprocess");
                };
                await PopupNavigation.Instance.PushAsync(enterPlayerDataPage);
            }
        }

        private async void Reconnect(object sender, EventArgs e)
        {
            Connecting = true;
            var reconnectResult = TryReconnectToLastHost();
            Connecting = false;

            if (!reconnectResult)
            {
                await PopupNavigation.Instance.PushAsync(new AlertPage("Reconnect to host failed, search for new host."));
                return;
            }
            await Shell.Current.GoToAsync($"gameprocess");
        }

        private bool TryReconnectToLastHost()
        {
            var lastHostIp = Preferences.Get(PreferencesKey.LastConnectedHostIp, null);
            if (lastHostIp != null)
            {
                var ipAdress = IPAddress.Parse(lastHostIp);

                var connectResult =  _gameClient.Connect(ipAdress);
                if (connectResult.IsFail) { return false; }

                _gameClient.RestorePlayerData();
                _gameClient.StartUpdatePlayers();
                _gameClient.SendPlayerInfo();
                _gameClient.StartListeningServerDisconnection();

                StopSearching();
                return true;
            }
            return false;
        }

        public void StartSearching()
        {
            if (!_searching)
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
            _gameClient.SavePlayerData();
            _gameClient.CloseConnection();
        }
    }
}