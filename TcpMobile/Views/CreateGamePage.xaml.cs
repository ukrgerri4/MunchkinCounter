using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using MunchkinCounterLan.Views;
using MunchkinCounterLan.Views.Popups;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    public class CreateGameViewModel : INotifyPropertyChanged
    {
        private IGameServer _gameServer => DependencyService.Get<IGameServer>();
        private IGameClient _gameClient => DependencyService.Get<IGameClient>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public CreateGameViewModel()
        {
            _gameClient.Players.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AllPlayers));
        }

        public MunchkinHost Host => _gameServer.Host;
        public Player MyPlayer => _gameClient.MyPlayer;

        public List<Player> AllPlayers =>
                _gameClient.Players
                    .OrderByDescending(p => p.Level)
                    .ThenByDescending(p => p.Modifiers)
                    .ThenBy(p => p.Name)
                    .ToList();
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateGamePage : ContentPage
    {
        private IGameServer _gameServer => DependencyService.Get<IGameServer>();
        private IGameClient _gameClient => DependencyService.Get<IGameClient>();
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();

        public CreateGameViewModel ViewModel { get; set; }

        public CreateGamePage()
        {
            InitializeComponent();

            ViewModel = new CreateGameViewModel();

            TrySetPlayerDefaults();

            BindingContext = ViewModel;
        }

        private async void TryCreate(object sender, EventArgs args)
        {
            try
            {
                _gameServer.Start();

                _gameClient.StartUpdatePlayers();
                _gameClient.ConnectSelf();
                _gameClient.SendPlayerInfo();
                _gameClient.StartListeningServerDisconnection();

                SavePlayerDefaults();

                await Shell.Current.GoToAsync("host");
            }
            catch (Exception e)
            {
                await PopupNavigation.Instance.PushAsync(new AlertPage("Create game error, please check your lan connection."));
                
                _ = _gameServer.Stop();
                _ = _gameClient.CloseConnection();
                
                _gameLogger.Error($"Create game error: {e.Message}");
            }
        }

        private void SavePlayerDefaults()
        {
            Preferences.Set(PreferencesKey.DefaultGameName, ViewModel.Host.Name);
            Preferences.Set(PreferencesKey.DefaultPlayerName, ViewModel.MyPlayer.Name);
            Preferences.Set(PreferencesKey.DefaultPlayerSex, ViewModel.MyPlayer.Sex);
        }

        private void TrySetPlayerDefaults()
        {
            var defGameName = Preferences.Get(PreferencesKey.DefaultGameName, null);
            if (!string.IsNullOrWhiteSpace(defGameName))
            {
                ViewModel.Host.Name = defGameName;
            }
            var defPlayerName = Preferences.Get(PreferencesKey.DefaultPlayerName, null);
            if (!string.IsNullOrWhiteSpace(defGameName))
            {
                ViewModel.MyPlayer.Name = defPlayerName;
            }
            var defPlayerSex = Preferences.Get(PreferencesKey.DefaultPlayerSex, -1);
            if (defPlayerSex >= 0)
            {
                ViewModel.MyPlayer.Sex = (byte)defPlayerSex;
            }
            
            
        }
    }
}