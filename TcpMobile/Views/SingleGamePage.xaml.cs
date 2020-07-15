using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Microsoft.Extensions.Configuration;
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
        private readonly IBrightnessService _brightnessService;

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

        public SingleGamePage(IServiceProvider serviceProvider,
            IConfiguration configuration,
            IBrightnessService brightnessService)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _brightnessService = brightnessService;

            InitializeComponent();

            BindingContext = this;

            MessagingCenter.Subscribe<SingleGamePage>(
                this,
                "ExpandView",
                (sender) => {
                    IsControlsVisible = !IsControlsVisible;
                }
            );

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => {
                if (!IsControlsVisible)
                {
                    IsControlsVisible = true;
                }
            };
            gameViewGrid.GestureRecognizers.Add(tapGestureRecognizer);

            _expandSubject.AsObservable()
                .Throttle(TimeSpan.FromSeconds(15))
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

        private void Expand(object sender, EventArgs e)
        {
            MessagingCenter.Send(this, "ExpandView");
        }
    }
}