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

namespace TcpMobile.Views
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public bool SleepModeSwitchValue
        {
            get { return Preferences.Get(PreferencesKey.KeepScreenOn, false); }
            set
            {
                Preferences.Set(PreferencesKey.KeepScreenOn, value);
                OnPropertyChanged(nameof(SleepModeSwitchValue));
            }
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : PopupPage
    {
        private readonly IGameLogger _gameLogger;
        private readonly IBrightnessService _brightnessService;

        private SettingsViewModel viewModel = new SettingsViewModel();

        public SettingsPage(IGameLogger gameLogger,
            IBrightnessService brightnessService)
        {
            _gameLogger = gameLogger;
            _brightnessService = brightnessService;

            InitializeComponent();

            BindingContext = viewModel;
        }

        private void ToggleSleepMode(object sender, EventArgs e)
        {
            if (!(sender is Switch sw)) { return; }

            if (sw.IsToggled)
            {
                _brightnessService.KeepScreenOn();
            }
            else
            {
                _brightnessService.KeepScreenOff();
            }
        }

        private async void Close(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }

        private void SetInfo()
        {
            ipAddressSection.Clear();

            var dnsName = Dns.GetHostName();
            var ips = Dns.GetHostAddresses(dnsName);
            ips.ForEach(ip => ipAddressSection.Add(new TextCell { Text = ip.ToString() }));
        }

        protected override void OnAppearing()
        {
            SetInfo();
        }
        protected override void OnDisappearing()
        {
        }
    }
}