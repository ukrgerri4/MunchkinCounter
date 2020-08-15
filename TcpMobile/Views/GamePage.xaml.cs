using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using MunchkinCounterLan.Models;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private IGameClient _gameClient => DependencyService.Get<IGameClient>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public GameViewModel()
        {
            _gameClient.Players.CollectionChanged += (s, e) => OnPropertyChanged(nameof(LanPlayers));
            _gameClient.MyPlayer.PropertyChanged += (s, e) => _gameClient.SendUpdatedPlayerState();
        }

        public Player MyPlayer => _gameClient.MyPlayer;
        public List<Player> LanPlayers =>
            _gameClient.Players
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.Modifiers)
                .ThenBy(p => p.Name)
                .ToList();

        public ICommand ToolsCommand { get; set; }
    }
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GamePage : ContentPage
    {
        private class InnerEvent
        {
            public PageEventType EventType { get; set; }
            public object Data { get; set; }
        }

        private IGameServer _gameServer => DependencyService.Get<IGameServer>();
        private IGameClient _gameClient => DependencyService.Get<IGameClient>();
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();

        private Subject<InnerEvent> _innerSubject;
        private Subject<Unit> _destroy = new Subject<Unit>();
        private bool _toolsClickHandling = false;
        private bool _needConfirmNavigation = true;

        public GameViewModel ViewModel { get; set; }

        public GamePage()
        {
            InitializeComponent();

            NavigationPage.SetHasBackButton(this, false);

            _innerSubject = new Subject<InnerEvent>();

            ViewModel = new GameViewModel();
            ViewModel.ToolsCommand = new Command<PageEventType>((eventType) => _innerSubject.OnNext(new InnerEvent { EventType = eventType }));

            Appearing += (s, e) =>
            {
                _innerSubject.AsObservable()
                    .TakeUntil(_destroy)
                    .Where(_ => !_toolsClickHandling)
                    .Where(_ => _.EventType == PageEventType.ResetMunchkin || _.EventType == PageEventType.ThrowDice || _.EventType == PageEventType.Fight)
                    .Do(_ => _toolsClickHandling = true)
                    .Subscribe(async _ => {
                        switch (_.EventType)
                        {
                            case PageEventType.ResetMunchkin:
                                await ResetMunchkinHandlerAsync();
                                break;
                            case PageEventType.ThrowDice:
                                await ThrowDiceHandler();
                                break;
                            case PageEventType.Fight:
                                await FightHandler(_);
                                break;
                        }

                        _toolsClickHandling = false;
                    });

                Shell.Current.Navigating += Navigating;
            };

            Disappearing += (s, e) =>
            {
                _destroy.OnNext(Unit.Default);
                Shell.Current.Navigating -= Navigating;
            };

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(async () => await Shell.Current.GoToAsync("../.."))
            });

            MessagingCenter.Subscribe<IGameClient>(this, "LostServerConnection", async (sender) => {

                var alert = new AlertPage("Connection to server lost, try reconnect");
                alert.Confirmed += async (s, e) =>
                {
                    _gameClient.SavePlayerData();
                    _gameClient.CloseConnection();
                    _gameServer.Stop();

                    _needConfirmNavigation = false;
                    await Shell.Current.GoToAsync("../..");
                };
                await PopupNavigation.Instance.PushAsync(alert);
            });

            BindingContext = ViewModel;
        }

        private async void Navigating(object sender, ShellNavigatingEventArgs e)
        {
            if (e.CanCancel && _needConfirmNavigation)
            {
                var route = e.Target.Location.OriginalString;
                e.Cancel();
                
                var alert = new AlertPage("Leaving this page you will be disconnected from current game. If you created this game all players also will be disconnected.", "Ok", "Cancel", closeOnConfirm: true);
                alert.Confirmed += (s, ev) =>
                {
                    _needConfirmNavigation = false;
                    
                    _gameClient.SavePlayerData();
                    _gameClient.CloseConnection();
                    _gameServer.Stop();

                    _gameLogger.Debug("Discon");

                    Shell.Current.GoToAsync(route);
                };
                await PopupNavigation.Instance.PushAsync(alert);
                return;
            }

            _needConfirmNavigation = true;
        }

        private async Task ResetMunchkinHandlerAsync()
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

        private async Task ThrowDiceHandler()
        {
            var dicePage = new DicePage();
            dicePage.Throwed += (s, diceValue) =>
            {
                ViewModel.MyPlayer.Dice = diceValue;
                _gameClient.SendUpdatedPlayerState();
            };

            await PopupNavigation.Instance.PushAsync(dicePage);
        }

        private async Task FightHandler(InnerEvent iev)
        {
            string partnerId = null;
            if (iev != null)
            {
                partnerId = ((Player)iev.Data).Id != ViewModel.MyPlayer.Id ? ((Player)iev.Data).Id : null;
            }

            await PopupNavigation.Instance.PushAsync(new FightPage(partnerId));
        }

        private void FightClick(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is Player p && p != null)
            {
                _innerSubject.OnNext(new InnerEvent { EventType = PageEventType.Fight, Data = p });
            }
        }
    }
}