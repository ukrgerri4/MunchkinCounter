using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TcpMobile.ExtendedComponents;
using TcpMobile.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    public class JoinGameViewModel: INotifyPropertyChanged
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

        public ObservableCollection<Player> ExeptMePlayers =>
            new ObservableCollection<Player>(
                _gameClient.Players
                    .Where(p => p.Id != _gameClient.MyPlayer.Id)
                    .OrderByDescending(p => p.Id)
                    .ThenByDescending(p => p.Modifiers)
                    .ThenBy(p => p.Name)
            );

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
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class JoinGamePage : ContentPage
    {
        private readonly IGameLogger _gameLogger;
        private readonly IGameClient _gameClient;

        private JoinGameViewModel _viewModel;

        private bool _isSearching = false;

        public JoinGamePage(IGameLogger gameLogger, IGameClient gameClient)
        {
            _gameLogger = gameLogger;
            _gameClient = gameClient;

            InitializeComponent();

            _viewModel = new JoinGameViewModel(_gameClient);

            BindingContext = _viewModel;

            MessagingCenter.Subscribe<IGameClient>(
                this,
                "HostsUpdated",
                (sender) => {
                    _viewModel.OnPropertyChanged(nameof(_viewModel.ConnectionsExists));
            });

            MessagingCenter.Subscribe<IGameClient>(
                this,
                "PlayersUpdated",
                (sender) => {
                    //_viewModel.OnPropertyChanged(nameof(_viewModel.AllPlayers));
                    _viewModel.OnPropertyChanged(nameof(_viewModel.ExeptMePlayers));
                });

            MessagingCenter.Subscribe<GameMenuPage>(
                this,
                "EndGame",
                async (sender) =>
                {
                    await Stop();
                }
            );

            /* FOR TEST */
            _viewModel.Process = true;
            /* FOR TEST */
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
                _gameClient.StartUpdatePlayers();
                var sendingInfoResult = _gameClient.SendPlayerInfo();

                hostsView.SelectedItem = null;

                if (sendingInfoResult.IsFail)
                {
                    await DisplayAlert("Wooops!", "Somethings went wrong:(", "Ok");
                    return;
                }

                _viewModel.Process = true;
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

        private void ToggleSex(object sender, EventArgs e)
        {
            _gameClient.MyPlayer.Sex = _gameClient.MyPlayer.Sex == 1 ? (byte)0 : (byte)1;
            _gameClient.SendUpdatedPlayerState();
        }

        private async Task Stop()
        {
            if (!_viewModel.HostSearch)
            {
                var confirm = await DisplayAlert("Stop game!", "Are you sure you want disconnect?", "Yes", "No");
                if (!confirm) { return; }
            }

            _gameClient.StopSearchHosts();
            _gameClient.Stop();

            _viewModel.HostSearch = true;
            _isSearching = false;
        }

        private async void KillMunchkin(object sender, EventArgs e)
        {
            if (!await DisplayAlert("", "Confirm!", "Yes", "No")) { return; }

            _viewModel.MyPlayer.Level = 1;
            _viewModel.MyPlayer.Modifiers = 0;
            _gameClient.SendUpdatedPlayerState();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!_isSearching)
            {
                _gameClient.StartSearchHosts();
                _isSearching = true;
            }
        }
    }
}