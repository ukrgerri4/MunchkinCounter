using GameMunchkin.Models;
using Infrastracture.Definitions;
using Microsoft.Extensions.Configuration;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    public class SingleGameViewModel
    {
        private Subject<Unit> _expandSubject;

        public Player MyPlayer { get; set; }
        public ICommand IncreaseLevel { get; set; }
        public ICommand DecreaseLevel { get; set; }
        public ICommand IncreaseModifiers { get; set; }
        public ICommand DecreaseModifiers { get; set; }
        public ICommand ToggleSex { get; set; }

        public SingleGameViewModel(Subject<Unit> expandSubject)
        {
            _expandSubject = expandSubject;

            MyPlayer = new Player();

            IncreaseLevel = new Command(
                () => {
                    if (MyPlayer.Level < 10)
                    {
                        MyPlayer.Level++;
                        RefreshLevelCanExecutes();
                    }
                    _expandSubject?.OnNext(Unit.Default);
                },
                () => { return MyPlayer.Level < 10; }
            );

            DecreaseLevel = new Command(
                () => {
                    if (MyPlayer.Level > 1)
                    {
                        MyPlayer.Level--;
                        RefreshLevelCanExecutes();
                    }
                    _expandSubject?.OnNext(Unit.Default);
                },
                () => { return MyPlayer.Level > 1; }
            );

            IncreaseModifiers = new Command(
                () => {
                    if (MyPlayer.Modifiers < 255)
                    {
                        MyPlayer.Modifiers++;
                        RefreshModifiersCanExecutes();
                    }
                    _expandSubject?.OnNext(Unit.Default);
                },
                () => { return MyPlayer.Modifiers < 255; }
            );

            DecreaseModifiers = new Command(
                () => {
                    if (MyPlayer.Modifiers > 0)
                    {
                        MyPlayer.Modifiers--;
                        RefreshModifiersCanExecutes();
                    }
                    _expandSubject?.OnNext(Unit.Default);
                },
                () => { return MyPlayer.Modifiers > 0; }
            );

            ToggleSex = new Command(
                () => { 
                    MyPlayer.Sex = MyPlayer.Sex == 1 ? (byte)0 : (byte)1;
                    _expandSubject?.OnNext(Unit.Default);
                }
            );
        }

        private void RefreshLevelCanExecutes()
        {
            (IncreaseLevel as Command).ChangeCanExecute();
            (DecreaseLevel as Command).ChangeCanExecute();
        }

        private void RefreshModifiersCanExecutes()
        {
            (IncreaseModifiers as Command).ChangeCanExecute();
            (DecreaseModifiers as Command).ChangeCanExecute();
        }

        public void ResetLevel()
        {
            MyPlayer.Level = 1;
            RefreshLevelCanExecutes();
        }

        public void ResetModifyers()
        {
            MyPlayer.Modifiers = 0;
            RefreshModifiersCanExecutes();
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleGamePage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public SingleGameViewModel viewModel { get; set; }

        private bool _isControlsVisible = true;
        public bool IsControlsVisible
        {
            get => _isControlsVisible;
            set
            {
                if (value != _isControlsVisible)
                {
                    _isControlsVisible = value;
                    OnPropertyChanged(nameof(IsControlsVisible));
                }
            }
        }

        private Subject<Unit> _expandSubject = new Subject<Unit>();
        private IDisposable _expandSubscription;

        public SingleGamePage(IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            viewModel = new SingleGameViewModel(_expandSubject);

            InitializeComponent();

            BindingContext = viewModel;

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => {
                if (!IsControlsVisible)
                {
                    IsControlsVisible = true;
                }
                _expandSubject.OnNext(Unit.Default);
            };
            gameViewGrid.GestureRecognizers.Add(tapGestureRecognizer);
        }

        private async void ResetMunchkin(object sender, EventArgs e)
        {
            var confirmPage = new ConfirmPage();
            confirmPage.OnReset += (s, ev) => {
                switch (ev)
                {
                    case "level":
                        viewModel.ResetLevel();
                        break;
                    case "modifiers":
                        viewModel.ResetModifyers();
                        break;
                    case "all":
                        viewModel.ResetLevel();
                        viewModel.ResetModifyers();
                        break;
                }
            };

            await PopupNavigation.Instance.PushAsync(confirmPage);
        }

        private async void ThrowDice(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(_serviceProvider.GetService<DicePage>());
        }

        protected override void OnAppearing()
        {
            _expandSubscription?.Dispose();
            _expandSubscription = _expandSubject.AsObservable()
                .Throttle(TimeSpan.FromSeconds(Preferences.Get(PreferencesKey.ViewExpandTimeoutSeconds, 15)))
                .Where(_ => Preferences.Get(PreferencesKey.IsViewExpandable, true))
                .Subscribe(_ => {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (IsControlsVisible)
                        {
                            IsControlsVisible = false;
                        }
                    });
                });

            _expandSubject.OnNext(Unit.Default);

            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            _expandSubscription?.Dispose();
            base.OnDisappearing();
        }
    }
}