using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : PopupPage
    {
        public ICommand RedirectCommand => 
            new Command<string>(async url => await Launcher.OpenAsync(new Uri(url)));

        public AboutPage()
        {
            InitializeComponent();

            BindingContext = this;
        }

        private async void Close(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}