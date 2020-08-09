using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FightPage : PopupPage
    {
        private IGameClient _gameClient => DependencyService.Get<IGameClient>();

        public ICommand CloseCommand { get; set; }
        public ICommand IncreaceMonsterPowerCommand { get; set; }
        public ICommand DecreaceMonsterPowerCommand { get; set; }

        private int _monsterPower = 0;
        public int MonsterPower
        {
            get => _monsterPower;
            set
            {
                if (_monsterPower != value)
                {
                    _monsterPower = value;
                    OnPropertyChanged(nameof(MonsterPower));
                }
            }
        }

        public Player MyPlayer => _gameClient.MyPlayer;

        public string _partnerId = null;
        public bool HavePartner => !string.IsNullOrWhiteSpace(_partnerId)
            && MyPlayer.Id != _partnerId
            && _gameClient.Players.Any(p => p.Id == _partnerId);
        public Player MyPartner => (string.IsNullOrWhiteSpace(_partnerId) || MyPlayer.Id == _partnerId)
            ? null
            : _gameClient.Players.FirstOrDefault(p => p.Id == _partnerId);

        public FightPage(string partnerId = null)
        {
            _partnerId = partnerId;

            InitializeComponent();

            CloseCommand = new Command(async () => await PopupNavigation.Instance.PopAsync());
            IncreaceMonsterPowerCommand = new Command(() => MonsterPower++);
            DecreaceMonsterPowerCommand = new Command(() => MonsterPower--);

            Appearing += (s, e) =>
            {
                OnPropertyChanged(nameof(HavePartner));
                OnPropertyChanged(nameof(MyPartner));
            };

            BindingContext = this;
        }

        private bool pressed = false;
        private void Button_Pressed(object sender, System.EventArgs e)
        {
            if (!pressed)
            {
                pressed = true;
                _ = Task.Run(async () =>
                {
                    while (pressed)
                    {
                        await Task.Delay(200);
                        IncreaceMonsterPowerCommand?.Execute(null);
                    }
                });
            }
        }

        private void Button_Released(object sender, System.EventArgs e)
        {
            pressed = false;
        }
    }
}