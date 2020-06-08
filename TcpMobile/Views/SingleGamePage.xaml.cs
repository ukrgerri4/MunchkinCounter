﻿using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Microsoft.Extensions.Configuration;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleGamePage : ContentPage
    {
        public Player player = new Player();
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IBrightnessService _brightnessService;

        public SingleGamePage(IServiceProvider serviceProvider,
            IConfiguration configuration,
            IBrightnessService brightnessService)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _brightnessService = brightnessService;

            InitializeComponent();

            var userLevelBinding = new Binding { Source = player, Path = "Level" };
            userLevelLabel.SetBinding(Label.TextProperty, userLevelBinding);
            
            var userModifyersBinding = new Binding { Source = player, Path = "Modifiers" };
            userModifyersLabel.SetBinding(Label.TextProperty, userModifyersBinding);

            var userPowerBinding = new Binding { Source = player, Path = "Power" };
            userPowerLabel.SetBinding(Label.TextProperty, userPowerBinding);

            var publicLevelBinding = new Binding { Source = player, Path = "Level", Mode = BindingMode.OneWay };
            var levelColor = new Binding { Source = player, Path = "Color", Mode = BindingMode.OneWay };
            publicLevelLabel.SetBinding(Label.TextProperty, publicLevelBinding);
            publicLevelLabel.SetBinding(Label.TextColorProperty, levelColor);
            publicLevelLabel.LineBreakMode = LineBreakMode.NoWrap;

            var publicModifiersBinding = new Binding { Source = player, Path = "Modifiers", Mode = BindingMode.OneWay };
            publicModifiersLabel.SetBinding(Label.TextProperty, publicModifiersBinding);
            publicModifiersLabel.LineBreakMode = LineBreakMode.NoWrap;

            var publicPowerBinding = new Binding { Source = player, Path = "Power", Mode = BindingMode.OneWay };
            publicPowerLabel.SetBinding(Label.TextProperty, publicPowerBinding);
            publicPowerLabel.LineBreakMode = LineBreakMode.NoWrap;

            var publicNameBinding = new Binding { Source = player, Path = "Name", Mode = BindingMode.OneWay };
            publicNameLabel.SetBinding(Label.TextProperty, publicNameBinding);
            publicNameLabel.LineBreakMode = LineBreakMode.NoWrap;
        }

        private void IncreaseLevel(object sender, EventArgs e)
        {
            if (player.Level < 10)
            {
                player.Level++;
            }
        }

        private void DecreaseLevel(object sender, EventArgs e)
        {
            if (player.Level > 1)
            {
                player.Level--;
            }
        }

        private void IncreaseModifiers(object sender, EventArgs e)
        {
            if (player.Modifiers < 255)
            {
                player.Modifiers++;
            }
        }

        private void DecreaseModifiers(object sender, EventArgs e)
        {
            if (player.Modifiers > 0)
            {
                player.Modifiers--;
            }
        }
    }
}