using Core.Utils;
using Infrastracture.Definitions;
using Rg.Plugins.Popup.Pages;
using System;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DicePage : PopupPage
    {
        public event EventHandler<byte> Throwed;

        private byte _diceValue = 0;
        public string DiceIconValue => DiceHelper.FaSet[_diceValue];

        public ICommand ThrowDiceCommand { get; set; }

        public DicePage()
        {
            InitializeComponent();

            Appearing += (s,e) => ThrowDice();

            ThrowDiceCommand = new Command(() => ThrowDice());

            BindingContext = this;
        }

        private void ThrowDice()
        {
            _diceValue = (byte)new Random().Next(1, 7);
            Throwed?.Invoke(this, _diceValue);
            OnPropertyChanged(nameof(DiceIconValue));
        }
    }
}