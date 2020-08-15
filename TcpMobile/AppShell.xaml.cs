using Infrastracture.Definitions;
using MunchkinCounterLan.Models;
using MunchkinCounterLan.Views;
using Rg.Plugins.Popup.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AppShell : Shell
    {
        public ICommand OpenModalCommand { get; set; }
        
        /*current page*/
        //(Current?.CurrentItem?.CurrentItem as IShellSectionController)?.PresentedPage;
        
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("host", typeof(HostPage));
            Routing.RegisterRoute("gameprocess", typeof(GamePage));

            Navigating += (s, e) => Console.WriteLine(e.Target.Location.OriginalString);

            OpenModalCommand = new Command<MenuItemType>(async type => await OpenModal(type));

            BindingContext = this;
        }

        private async Task OpenModal(MenuItemType type)
        {
            switch (type)
            {
                //case MenuItemType.Debug:
                //    await PopupNavigation.Instance.PushAsync(DependencyService.Get<DebugPage>());
                //    break;
                case MenuItemType.Settings:
                    await PopupNavigation.Instance.PushAsync(DependencyService.Get<SettingsPage>());
                    break;
                case MenuItemType.ShareApp:
                    await Share.RequestAsync(new ShareTextRequest
                    {
                        Uri = "https://play.google.com/store/apps/details?id=com.kivgroupua.munchkincounterlan",
                        Title = "Share MunchkinCounterLan"
                    });
                    break;
                case MenuItemType.Contribute:
                    await PopupNavigation.Instance.PushAsync(DependencyService.Get<ContributePage>());
                    break;
                case MenuItemType.About:
                    await PopupNavigation.Instance.PushAsync(DependencyService.Get<AboutPage>());
                    break;
            }
        }
    }
}