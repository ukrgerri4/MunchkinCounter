using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TcpMobile.Views;
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
        //public ObservableCollection<MunchkinHost> Hosts =>
        //    new ObservableCollection<MunchkinHost>
        //    {
        //        new MunchkinHost {Id = "1", Name= "Igor_game", Capacity = 55, Fullness = 55 },
        //        new MunchkinHost {Id = "1", Name= "Igor_game", Capacity = 55, Fullness = 55 },
        //        new MunchkinHost {Id = "1", Name= "Igor_game", Capacity = 55, Fullness = 55 }
        //    };
        public Player MyPlayer => _gameClient.MyPlayer;

        public ObservableCollection<Player> ExeptMePlayers =>
            new ObservableCollection<Player>(
                _gameClient.Players
                    .Where(p => p.Id != _gameClient.MyPlayer.Id)
                    .OrderByDescending(p => p.Id)
                    .ThenByDescending(p => p.Modifiers)
                    .ThenBy(p => p.Name)
            );

        //public ObservableCollection<Player> ExeptMePlayers =>
        //    new ObservableCollection<Player>(
        //        new List<Player>
        //        {
        //            new Player {Id = "1", Name = "KryvenkoIgorVasilievich", Level = 3, Modifiers = 0, Sex = 1 },
        //            new Player {Id = "2", Name = "Murina", Level = 7, Modifiers = 55, Sex = 0 },
        //            new Player {Id = "3", Name = "Stas", Level = 8, Modifiers = 10, Sex = 1 },
        //            new Player {Id = "4", Name = "Zepsen", Level = 2, Modifiers = 15, Sex = 1 },
        //            new Player {Id = "5", Name = "Yulia", Level = 3, Modifiers = 4, Sex = 0 },
        //            new Player {Id = "6", Name = "Saniz", Level = 10, Modifiers = 55, Sex = 1 },
        //            new Player {Id = "1", Name = "Gleb", Level = 2, Modifiers = 0, Sex = 1 }
        //        }.OrderByDescending(p => p.Level).ToList()
        //    );

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

        private bool _searching = false;
        private bool _appeared = false;

        public JoinGamePage(IServiceProvider serviceProvider, IGameLogger gameLogger, IGameClient gameClient)
        {
            _serviceProvider = serviceProvider;
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

            MessagingCenter.Subscribe<MenuPage>(
                this,
                "EndGame",
                async (sender) =>
                {
                    await Stop();
                }
            );

            /* FOR TEST */
            //_viewModel.Process = true;
            /* FOR TEST */
        }

        private void StopSearching(object sender, EventArgs e)
        {
            _gameClient.StopSearchHosts();

            _gameClient.Hosts.Clear();

            _gameLogger.Debug("Listening broadcast stoped");
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
            //_searching = false;
        }



        protected override void OnAppearing()
        {
            _appeared = true;
            base.OnAppearing();

            if (!_searching)
            {
                _gameClient.StartSearchHosts();
                _searching = true;
            }

            Device.StartTimer(TimeSpan.FromMilliseconds(600), () =>
            {
                _viewModel.LoaderPointsCount = _viewModel.LoaderPointsCount < 4 ? _viewModel.LoaderPointsCount + 1 : 0;
                return _appeared && _searching;
            });
        }

        protected override void OnDisappearing()
        {
            _appeared = false;
            base.OnDisappearing();
        }

        private async void HostTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null && e.Item is MunchkinHost munchkinHost && munchkinHost?.IpAddress != null)
            {
                _gameClient.Connect(munchkinHost.IpAddress);
                _gameClient.StartUpdatePlayers();
                var sendingInfoResult = _gameClient.SendPlayerInfo();


                if (sendingInfoResult.IsFail)
                {
                    await DisplayAlert("Wooops!", "Somethings went wrong:(", "Ok");
                    return;
                }

                _viewModel.Process = true;
            }

            //hostsView.SelectedItem = null;
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
    }
}