using Infrastracture.Definitions;
using Infrastracture.Models;
using MunchkinCounterLan.Models;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleGamePage : ContentPage
    {
        private const int DEF_EXPAND_TIME_SECONDS = 5;

        private Subject<PageEventType> _innerSubject;
        private Subject<Unit> _destroy = new Subject<Unit>();
        private bool _toolsClickHandling = false;
        public ICommand ToolsClick { get; set; }
        
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

        private int _rotateValue = 0;
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


        public SingleGamePage()
        {
            InitializeComponent();

            _innerSubject = new Subject<PageEventType>();
            ToolsClick = new Command<PageEventType>((eventType) => _innerSubject.OnNext(eventType));
            MyPlayer = new Player();
            MyPlayer.PropertyChanged += (s, e) => _innerSubject?.OnNext(PageEventType.ExpandView);

            Appearing += (s, e) =>
            {
                _innerSubject.AsObservable()
                    .TakeUntil(_destroy)
                    .Throttle(TimeSpan.FromSeconds(Preferences.Get(PreferencesKey.ViewExpandTimeoutSeconds, DEF_EXPAND_TIME_SECONDS)))
                    .Where(eventType => eventType == PageEventType.ExpandView)
                    .Where(_ => Preferences.Get(PreferencesKey.IsViewExpandable, true))
                    .Subscribe(_ => {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (IsControlsVisible)
                            {
                                IsControlsVisible = false;
                                RotateValue = 180;
                            }
                        });
                    });

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
                                await ThrowDiceHandler();
                                break;
                        }

                        _toolsClickHandling = false;
                    });

                _innerSubject.OnNext(PageEventType.ExpandView);
            };

            Disappearing += (s, e) =>
            {
                _destroy.OnNext(Unit.Default);
                IsControlsVisible = true;
                RotateValue = 0;
            };

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => {
                if (!IsControlsVisible)
                {
                    IsControlsVisible = true;
                    RotateValue = 0;
                }
                _innerSubject.OnNext(PageEventType.ExpandView);
            };
            gameViewGrid.GestureRecognizers.Add(tapGestureRecognizer);

            BindingContext = this;
        }

        private async Task ThrowDiceHandler()
        {
            var dicePage = new DicePage();
            dicePage.Disappearing += (s, e) => _innerSubject.OnNext(PageEventType.ExpandView);
            await PopupNavigation.Instance.PushAsync(dicePage);
        }

        private void RotateView(object sender, EventArgs e)
        {
            RotateValue = RotateValue == 180 ? 0 : 180;
            _innerSubject.OnNext(PageEventType.ExpandView);
        }

        private async Task ResetMunchkinHandler()
        {
            var confirmPage = new ConfirmPage();
            confirmPage.OnReset += (sender, ev) => {
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
            confirmPage.Disappearing += (s,e) => _innerSubject.OnNext(PageEventType.ExpandView);
            await PopupNavigation.Instance.PushAsync(confirmPage);
        }
    }
}