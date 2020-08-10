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

        public ICommand AddMonsterPowerCommand { get; set; }

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


        private bool _considerMyPlayerLevel = true;
        public bool ConsiderMyPlayerLevel
        {
            get => _considerMyPlayerLevel;
            set
            {
                if (_considerMyPlayerLevel != value)
                {
                    _considerMyPlayerLevel = value;
                    OnPropertyChanged(nameof(ConsiderMyPlayerLevel));
                    OnPropertyChanged(nameof(CalculatedPower));
                }
            }
        }

        private bool _considerMyPlayerModifiers = true;
        public bool ConsiderMyPlayerModifiers
        {
            get => _considerMyPlayerModifiers;
            set
            {
                if (_considerMyPlayerModifiers != value)
                {
                    _considerMyPlayerModifiers = value;
                    OnPropertyChanged(nameof(ConsiderMyPlayerModifiers));
                    OnPropertyChanged(nameof(CalculatedPower));
                }
            }
        }

        private bool _considerMyPartnerLevel = true;
        public bool ConsiderMyPartnerLevel
        {
            get => _considerMyPartnerLevel;
            set
            {
                if (_considerMyPartnerLevel != value)
                {
                    _considerMyPartnerLevel = value;
                    OnPropertyChanged(nameof(ConsiderMyPartnerLevel));
                    OnPropertyChanged(nameof(CalculatedPower));
                }
            }
        }

        private bool _considerMyPartnerModifiers = true;
        public bool ConsiderMyPartnerModifiers
        {
            get => _considerMyPartnerModifiers;
            set
            {
                if (_considerMyPartnerModifiers != value)
                {
                    _considerMyPartnerModifiers = value;
                    OnPropertyChanged(nameof(ConsiderMyPartnerModifiers));
                    OnPropertyChanged(nameof(CalculatedPower));
                }
            }
        }

        public ICommand AddAdditionalPlayerPowerCommand { get; set; }
        private int _additionalPlayerPower = 0;
        public int AdditionalPlayerPower
        {
            get => _additionalPlayerPower;
            set
            {
                if (_additionalPlayerPower != value)
                {
                    _additionalPlayerPower = value;
                    OnPropertyChanged(nameof(AdditionalPlayerPower));
                    OnPropertyChanged(nameof(CalculatedPower));
                }
            }
        }

        public int CalculatedPower
        {
            get
            {
                var calculatedPower = 0;

                if (ConsiderMyPlayerLevel) calculatedPower += MyPlayer.Level;
                if (ConsiderMyPlayerModifiers) calculatedPower += MyPlayer.Modifiers;
                if (ConsiderMyPartnerLevel) calculatedPower += MyPartner?.Level ?? 0;
                if (ConsiderMyPartnerModifiers) calculatedPower += MyPartner?.Modifiers ?? 0;

                calculatedPower += AdditionalPlayerPower;

                return calculatedPower;
            }
        }

        public FightPage(string partnerId = null)
        {
            _partnerId = partnerId;

            InitializeComponent();

            CloseCommand = new Command(async () => await PopupNavigation.Instance.PopAsync());
            AddMonsterPowerCommand = new Command<int>((power) => MonsterPower += power);
            AddAdditionalPlayerPowerCommand = new Command<int>((power) => AdditionalPlayerPower += power);

            if (MyPartner != null)
            {
                MyPartner.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CalculatedPower));
            }
            if (MyPlayer != null)
            {
                MyPlayer.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CalculatedPower));
            }

            Appearing += (s, e) =>
            {
                OnPropertyChanged(nameof(HavePartner));
                OnPropertyChanged(nameof(MyPartner));
            };

            BindingContext = this;
        }
    }
}