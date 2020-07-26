using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfirmPage : PopupPage
    {
        public event EventHandler<string> OnReset;

        public ConfirmPage()
        {
            InitializeComponent();
        }

        private async void OnResetLevel(object sender, EventArgs e)
        {
            OnReset?.Invoke(this, "level");
            await PopupNavigation.Instance.PopAsync();
        }

        private async void OnResetModifiers(object sender, EventArgs e)
        {
            OnReset?.Invoke(this, "modifiers");
            await PopupNavigation.Instance.PopAsync();
        }

        private async void OnResetAll(object sender, EventArgs e)
        {
            OnReset?.Invoke(this, "all");
            await PopupNavigation.Instance.PopAsync();
        }

        private async void Close(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}