using Infrastracture.Interfaces;
using MunchkinCounterLan.Models;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : ContentPage
    {
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();
        private IScreenshotService _screenshotService => DependencyService.Get<IScreenshotService>();

        public ICommand GoTo { get; set; }
        public HomePage()
        {
            InitializeComponent();

            GoTo = new Command<string>(async (route) => await Shell.Current.GoToAsync($"{route}"));

            BindingContext = this;
        }
    }
}