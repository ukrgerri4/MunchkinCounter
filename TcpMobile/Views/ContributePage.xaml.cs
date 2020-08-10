using Infrastracture.Interfaces;
using MunchkinCounterLan.Views.Popups;
using Plugin.InAppBilling;
using Plugin.InAppBilling.Abstractions;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContributePage : PopupPage
    {
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();

        private bool _isLoading;
        public bool Loading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoadingVisible));
                    OnPropertyChanged(nameof(IsLoadingInvisible));
                }
            }
        }

        public bool IsLoadingVisible => _isLoading;
        public bool IsLoadingInvisible => !_isLoading;

        public ICommand Rate { get; set; }
        public ICommand Donate { get; set; }
        public ICommand Close { get; set; }

        public ContributePage()
        {
            _isLoading = false;

            InitializeComponent();

            Rate = new Command(async () => await Launcher.OpenAsync(new Uri("market://details?id=com.kivgroupua.munchkincounterlan")));
            Donate = new Command<string>(async (productId) => await DonatePurchaseAsync(productId));
            Close = new Command(async () => await PopupNavigation.Instance.PopAsync());

            BindingContext = this;
        }

        private async Task DonatePurchaseAsync(string productId)
        {
            try
            {
                var connected = await CrossInAppBilling.Current.ConnectAsync();

                if (!connected)
                {
                    await PopupNavigation.Instance.PushAsync(new AlertPage("Connection failed, please check your internet connection", "Ok"));
                    return;
                }

                //try to purchase item
                var purchase = await CrossInAppBilling.Current.PurchaseAsync(productId, ItemType.InAppPurchase, "kivgroup_payload");
                if (purchase == null)
                {
                    await PopupNavigation.Instance.PushAsync(new AlertPage("Unsuccessful payment attempt, service unavailable, thanks for the intention", "Ok"));
                    return;
                }
                else
                {
                    //Purchased, save this information
                    var id = purchase.Id;
                    var token = purchase.PurchaseToken;
                    var state = purchase.State;
                }
            }
            catch (Exception ex)
            {
                _gameLogger.Error($"DonatePurchaseAsync: {ex.Message}");
                await PopupNavigation.Instance.PushAsync(new AlertPage("Oops, something went wrong", "Ok"));
            }
            finally
            {
                //Disconnect, it is okay if we never connected
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }
    }
}