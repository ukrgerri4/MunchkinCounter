using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    public class JoinGameViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public ObservableCollection<MunchkinHost> Hosts { get; set; }
        public Player MyPlayer { get; set; }

        public ObservableCollection<Player> Players { get; set; }


        private bool _hostSearch = true;
        private bool _waitingPlayers = false;
        private bool _process = false;

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
                        WaitingPlayers = false;
                        Process = false;
                    }

                    OnPropertyChanged(nameof(HostSearch));
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
                        HostSearch = false;
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
                        HostSearch = false;
                        WaitingPlayers = false;
                    }

                    OnPropertyChanged(nameof(Process));
                }
            }
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class JoinGamePage : ContentPage
    {
        private readonly IGameLogger _gameLogger;
        private readonly IGameClient _gameClient;

        private JoinGameViewModel _viewModel;

        //private Subject<Unit> _destroy = new Subject<Unit>();

        public JoinGamePage(IGameLogger gameLogger, IGameClient gameClient)
        {
            _gameLogger = gameLogger;
            _gameClient = gameClient;

            InitializeComponent();

            _viewModel = new JoinGameViewModel
            {
                Hosts = _gameClient.Hosts,
                Players = _gameClient.Players,
                MyPlayer = _gameClient.MyPlayer,
                HostSearch = true
            };

            BindingContext = _viewModel;
        }

        private void SearchForHosts(object sender, EventArgs e)
        {
            _gameClient.StartSearchHosts();
            
            _gameLogger.Debug("Listening broadcast started");
        }

        private void StopSearching(object sender, EventArgs e)
        {
            _gameClient.StopSearchHosts();

            _gameClient.Hosts.Clear();

            _gameLogger.Debug("Listening broadcast stoped");
        }


        private async void Connect(object sender, EventArgs e)
        {
            if (hostsView.SelectedItem != null && hostsView.SelectedItem is MunchkinHost munchkinHost && munchkinHost?.IpAddress != null)
            {
                _gameClient.Connect(munchkinHost.IpAddress);
                var sendingInfoResult = _gameClient.SendPlayerInfo();

                hostsView.SelectedItem = null;

                if (sendingInfoResult.IsFail)
                {
                    await DisplayAlert("Wooops!", "Somethings went wrong:(", "Ok");
                    return;
                }

                _viewModel.WaitingPlayers = true;
            }
        }

        protected override void OnAppearing()
        {
        }

        protected override void OnDisappearing()
        {
            
        }
    }
}