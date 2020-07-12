using System;
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
            await App.Current.MainPage.Navigation.PopModalAsync();
        }

        private async void GoToJoinGame(object sender, EventArgs e)
        {
            await App.Current.MainPage.Navigation.PushAsync(_serviceProvider.GetService<JoinGamePage>());
            await App.Current.MainPage.Navigation.PopModalAsync();
        }

        private async void GoToSingleGame(object sender, EventArgs e)
        {
            

            var page = App.Current.MainPage.Navigation.NavigationStack.LastOrDefault();
            if (page == null || page is SingleGamePage)
            {
                await App.Current.MainPage.Navigation.PopModalAsync();
                return;
            }

            await App.Current.MainPage.Navigation.PushAsync(_serviceProvider.GetService<SingleGamePage>());
        }

        private async void CloseModal(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void ExitToMenu(object sender, EventArgs e)
        {
            MessagingCenter.Send(this, "EndGame");
            App.Current.MainPage.Navigation.PopToRootAsync();
            ChangeMenuState();
        }

        private void Exit(object sender, EventArgs e)
        {

        }

        private async void OpenSettings(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new MunchkinModalNavigationPage(_serviceProvider.GetService<SettingsPage>()));
        }

        private async void OpenDebug(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new MunchkinModalNavigationPage(_serviceProvider.GetService<DebugPage>()));
        }

        private async void OpenAbout(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new MunchkinModalNavigationPage(_serviceProvider.GetService<AboutPage>()));
        }
        

        protected override void OnAppearing()
        {
            ChangeMenuState();
            base.OnAppearing();
        }

        private void ChangeMenuState()
        {
            var page = App.Current.MainPage.Navigation.NavigationStack.LastOrDefault();
            var inGame = page != null && (page is CreateGamePage || page is JoinGamePage);

            InGame = inGame;
            NotInGame = !inGame;

            OnPropertyChanged(nameof(InGame));
            OnPropertyChanged(nameof(NotInGame));
        }
    }
}