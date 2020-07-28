using GameMunchkin.Models;
using Infrastracture.Definitions;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using Xamarin.Essentials;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EnterPlayerDataPage : PopupPage
    {
        public event EventHandler<Player> OnNextPressed;
        public Player Player { get; set; }

        public EnterPlayerDataPage()
        {
            InitializeComponent();

            Player = new Player();
            Player.Name = Preferences.Get(PreferencesKey.DefaultPlayerName, string.Empty);
            Player.Sex = (byte)Preferences.Get(PreferencesKey.DefaultPlayerSex, 0);

            BindingContext = this;
        }

        private async void Cancel(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }

        private async void Next(object sender, EventArgs e)
        {
            Preferences.Set(PreferencesKey.DefaultPlayerName, Player.Name);
            Preferences.Set(PreferencesKey.DefaultPlayerSex, Player.Sex);

            OnNextPressed?.Invoke(this, Player);
            await PopupNavigation.Instance.PopAsync();
        }
    }
}