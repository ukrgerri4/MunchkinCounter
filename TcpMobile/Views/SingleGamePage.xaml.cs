using Infrastracture.Definitions;
using Infrastracture.Models;
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

        public Player MyPlayer { get; set; }

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

        private int _rotateValue = 180;
        public int RotateValue
        {
            get => _rotateValue;
            set
            {
                if (value != _rotateValue)
                {
                    _rotateValue = value;
                    OnPropertyChanged(nameof(RotateValue));
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

            MyPlayer = new Player();
            MyPlayer.PropertyChanged += (s,e) => _expandSubject?.OnNext(Unit.Default);

            InitializeComponent();

            Appearing += (s, e) =>
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
            };

            Disappearing += (s, e) =>
            {
                _expandSubscription?.Dispose();
                IsControlsVisible = true;
            };

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => {
                if (!IsControlsVisible)
                {
                    IsControlsVisible = true;
                }
                _expandSubject.OnNext(Unit.Default);
            };
            gameViewGrid.GestureRecognizers.Add(tapGestureRecognizer);

            BindingContext = this;
        }


        private void RotateView(object sender, EventArgs e)
        {
            RotateValue = RotateValue == 180 ? 0 : 180;
            _expandSubject.OnNext(Unit.Default);
        }

        private async void ResetMunchkin(object sender, EventArgs e)
        {
            var confirmPage = new ConfirmPage();
            confirmPage.OnReset += (s, ev) => {
                switch (ev)
                {
                    case "level":
                        MyPlayer.ResetLevel();
                        break;
                    case "modifiers":
                        MyPlayer.ResetModifyers();
                        break;
                    case "all":
                        MyPlayer.ResetLevel();
                        MyPlayer.ResetModifyers();
                        break;
                }
            };

            await PopupNavigation.Instance.PushAsync(confirmPage);
        }

        private async void ThrowDice(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(_serviceProvider.GetService<DicePage>());
        }
    }
}