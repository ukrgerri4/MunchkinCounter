using GameMunchkin.Models;
using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Microsoft.Extensions.Configuration;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleGamePage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public Player MyPlayer { get; set; } = new Player();

        private bool _isControlsVisible = true;
        public bool IsControlsVisible
        {
            get => _isControlsVisible;
            set
            {
                if(value != _isControlsVisible)
                {
                    _isControlsVisible = value;
                    NavigationPage.SetHasNavigationBar(this, _isControlsVisible);
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

            InitializeComponent();

            BindingContext = this;

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

        private void IncreaseLevel(object sender, EventArgs e)
        {
            if (MyPlayer.Level < 10)
            {
                MyPlayer.Level++;
            }
            _expandSubject.OnNext(Unit.Default);
        }

        private void DecreaseLevel(object sender, EventArgs e)
        {
            if (MyPlayer.Level > 1)
            {
                MyPlayer.Level--;
            }
            _expandSubject.OnNext(Unit.Default);
        }

        private void IncreaseModifiers(object sender, EventArgs e)
        {
            if (MyPlayer.Modifiers < 255)
            {
                MyPlayer.Modifiers++;
            }
            _expandSubject.OnNext(Unit.Default);
        }

        private void DecreaseModifiers(object sender, EventArgs e)
        {
            if (MyPlayer.Modifiers > 0)
            {
                MyPlayer.Modifiers--;
            }
            _expandSubject.OnNext(Unit.Default);
        }

        
        private void ToggleSex(object sender, EventArgs e)
        {
            MyPlayer.Sex = MyPlayer.Sex == 1 ? (byte)0 : (byte)1;
            _expandSubject.OnNext(Unit.Default);
        }

        private async void ResetMunchkin(object sender, EventArgs e)
        {
            var confirmPage = new ConfirmPage();
            confirmPage.OnReset += (s, ev) => {
                switch (ev)
                {
                    case "level":
                        MyPlayer.Level = 1;
                        break;
                    case "modifiers":
                        MyPlayer.Modifiers = 0;
                        break;
                    case "all":
                        MyPlayer.Level = 1;
                        MyPlayer.Modifiers = 0;
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