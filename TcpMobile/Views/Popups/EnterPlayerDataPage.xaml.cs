using Core.Utils;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EnterPlayerDataPage : PopupPage
    {
        public event EventHandler<(string Name, byte Sex)> OnNextPressed;

        private string _name;
        public string Name {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public ICommand ToggleSex { get; set; }

        private byte _sex;
        public byte Sex
        {
            get => _sex;
            set
            {
                _sex = value;
                OnPropertyChanged(nameof(Sex));
                OnPropertyChanged(nameof(SexIcon));
            }
        }

        public string SexIcon
        {
            get => _sex == 1 ? FontAwesomeIcons.Mars : FontAwesomeIcons.Venus;
        }

        public EnterPlayerDataPage()
        {
            InitializeComponent();

            ToggleSex = new Command(() => Sex = Sex == 1 ? (byte)0 : (byte)1);

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