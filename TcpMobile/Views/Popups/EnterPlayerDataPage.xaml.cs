using GameMunchkin.Models;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
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

            BindingContext = this;
        }

        private async void Cancel(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }

        private async void Next(object sender, EventArgs e)
        {
            OnNextPressed?.Invoke(this, Player);
            await PopupNavigation.Instance.PopAsync();
        }
    }
}