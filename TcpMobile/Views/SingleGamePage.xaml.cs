using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
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

        public SingleGamePage(IServiceProvider serviceProvider,
            IConfiguration configuration,
            IBrightnessService brightnessService)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _brightnessService = brightnessService;

            InitializeComponent();

            BindingContext = this;
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
            if (Player.Power < 91)
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
    }
}