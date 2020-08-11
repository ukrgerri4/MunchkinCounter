using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.ComponentModel;
using System.Net;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private IBrightnessService _brightnessService => DependencyService.Get<IBrightnessService>();
        
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        

        public bool SleepModeSwitchValue
        {
            get { return Preferences.Get(PreferencesKey.KeepScreenOn, false); }
            set
            {
                Preferences.Set(PreferencesKey.KeepScreenOn, value);

                if (value)
                {
                    _brightnessService.KeepScreenOn();
                }
                else
                {
                    _brightnessService.KeepScreenOff();
                }

                OnPropertyChanged(nameof(SleepModeSwitchValue));
            }
        }

        public bool IsViewExpandable
        {
            get { return Preferences.Get(PreferencesKey.IsViewExpandable, true); }
            set
            {
                Preferences.Set(PreferencesKey.IsViewExpandable, value);
                OnPropertyChanged(nameof(IsViewExpandable));
            }
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : PopupPage
    {
        public SettingsViewModel viewModel { get; set; }

        public SettingsPage()
        {
            InitializeComponent();

            viewModel = new SettingsViewModel();

            BindingContext = viewModel;
        }

        private void SetInfo()
        {
            ipAddressSection.Clear();

            var dnsName = Dns.GetHostName();
            var ips = Dns.GetHostAddresses(dnsName);
            ips.ForEach(ip => ipAddressSection.Add(new TextCell { Text = ip.ToString() }));
        }

        private async void Close(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SetInfo();
        }
        protected override void OnDisappearing()
        {
        }
    }
}