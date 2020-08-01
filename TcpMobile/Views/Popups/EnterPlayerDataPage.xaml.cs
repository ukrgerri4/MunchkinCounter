using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EnterPlayerDataPage : PopupPage
    {
        public event EventHandler<(string Name, byte Sex)> OnNextPressed;
        public string Name { get; set; }
        public byte Sex { get; set; }

        public EnterPlayerDataPage()
        {
            InitializeComponent();

            BindingContext = this;
        }

        private async void Next(object sender, EventArgs e)
        {
            OnNextPressed?.Invoke(this, (Name, Sex));
            await PopupNavigation.Instance.PopAsync();
        }

        private async void Cancel(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}