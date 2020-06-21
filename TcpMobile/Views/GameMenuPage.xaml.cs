﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpMobile.ExtendedComponents;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GameMenuPage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;

        public bool InGame { get; set; }
        public bool NotInGame { get; set; }
        //public bool InGame
        //{
        //    get
        //    {
        //        var page = Navigation.NavigationStack.Last();
        //        return page != null && (page is CreateGamePage || page is JoinGamePage);
        //    }
        //}

        //public bool NotInGame
        //{
        //    get
        //    {
        //        var page = Navigation.NavigationStack.Last();
        //        return page == null || !(page is CreateGamePage || page is JoinGamePage);
        //    }
        //}

        public GameMenuPage(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            InitializeComponent();

            BindingContext = this;
        }

        private async void GoToCreateGame(object sender, EventArgs e)
        {
            await App.Current.MainPage.Navigation.PushAsync(_serviceProvider.GetService<CreateGamePage>());
            await App.Current.MainPage.Navigation.PopModalAsync(false);
        }

        private async void GoToJoinGame(object sender, EventArgs e)
        {
            await App.Current.MainPage.Navigation.PushAsync(_serviceProvider.GetService<JoinGamePage>());
            await App.Current.MainPage.Navigation.PopModalAsync(false);
        }

        private async void GoToSingleGame(object sender, EventArgs e)
        {
            

            var page = App.Current.MainPage.Navigation.NavigationStack.LastOrDefault();
            if (page == null || page is SingleGamePage)
            {
                await App.Current.MainPage.Navigation.PopModalAsync(false);
                return;
            }

            await App.Current.MainPage.Navigation.PushAsync(_serviceProvider.GetService<SingleGamePage>());
        }

        private async void CloseModal(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(false);
        }

        private void ExitToMenu(object sender, EventArgs e)
        {

        }

        private void Exit(object sender, EventArgs e)
        {

        }

        private async void OpenSettings(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(_serviceProvider.GetService<SettingsPage>());
        }

        private async void OpenDebug(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(_serviceProvider.GetService<DebugPage>());
        }

        protected override void OnAppearing()
        {
            var page = App.Current.MainPage.Navigation.NavigationStack.LastOrDefault();
            var inGame = page != null && (page is CreateGamePage || page is JoinGamePage);

            InGame = inGame;
            NotInGame = !inGame;

            OnPropertyChanged(nameof(InGame));
            OnPropertyChanged(nameof(NotInGame));
            base.OnAppearing();
        }
    }
}