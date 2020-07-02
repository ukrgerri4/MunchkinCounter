using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using TcpMobile.ExtendedComponents;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleGamePage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IBrightnessService _brightnessService;

        public Player Player { get; set; } = new Player();

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

        public SingleGamePage(IServiceProvider serviceProvider,
            IConfiguration configuration,
            IBrightnessService brightnessService)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _brightnessService = brightnessService;

            InitializeComponent();

            BindingContext = this;

            MessagingCenter.Subscribe<MunchkinNavigationPage>(
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
        }

        private void IncreaseLevel(object sender, EventArgs e)
        {
            if (Player.Level < 10)
            {
                Player.Level++;
            }
        }

        private void DecreaseLevel(object sender, EventArgs e)
        {
            if (Player.Level > 1)
            {
                Player.Level--;
            }
        }

        private void IncreaseModifiers(object sender, EventArgs e)
        {
            if (Player.Modifiers < 255)
            {
                Player.Modifiers++;
            }
        }

        private void DecreaseModifiers(object sender, EventArgs e)
        {
            if (Player.Modifiers > 0)
            {
                Player.Modifiers--;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }
    }
}