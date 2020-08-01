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
                    .Throttle(TimeSpan.FromSeconds(Preferences.Get(PreferencesKey.ViewExpandTimeoutSeconds, 15)))
                    .Where(eventType => eventType == PageEventType.ExpandView)
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
                                await PopupNavigation.Instance.PushAsync(new DicePage());
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
            };

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => {
                if (!IsControlsVisible)
                {
                    IsControlsVisible = true;
                }
                _innerSubject.OnNext(PageEventType.ExpandView);
            };
            gameViewGrid.GestureRecognizers.Add(tapGestureRecognizer);

            BindingContext = this;
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

            await PopupNavigation.Instance.PushAsync(confirmPage);
        }
    }
}