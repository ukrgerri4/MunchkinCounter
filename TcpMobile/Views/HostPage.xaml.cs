using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HostPage : ContentPage
    {
        private IGameServer _gameServer => DependencyService.Get<IGameServer>();
        private IGameClient _gameClient => DependencyService.Get<IGameClient>();

        public MunchkinHost Host => _gameServer.Host;
        public Player MyPlayer => _gameClient.MyPlayer;
        public List<Player> AllPlayers =>
            _gameClient.Players
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.Modifiers)
                .ThenBy(p => p.Name)
                .ToList();

        public ICommand StartGameCommand { get; set; }
        public ICommand EndGameCommand { get; set; }

        public HostPage()
        {
            InitializeComponent();

            _gameClient.Players.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AllPlayers));

            StartGameCommand = new Command(async () => {
                _gameServer.StopBroadcast();
                
                await Shell.Current.GoToAsync($"gameprocess");
            });

            EndGameCommand = new Command(async () => await BackButtonHandler());

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                Command = new Command(async () => await BackButtonHandler())
            });

            BindingContext = this;
        }

        private async Task BackButtonHandler()
        {
            _gameClient.CloseConnection();
            _gameServer.Stop();

            await Shell.Current.GoToAsync("..");
        }
    }
}