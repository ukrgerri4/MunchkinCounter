using Infrastracture.Interfaces;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DebugPage : ContentPage
    {
        private readonly IGameLogger _gameLogger;

        public DebugPage(IGameLogger gameLogger)
        {
            _gameLogger = gameLogger;

            InitializeComponent();

            debugConsoleView.HasUnevenRows = true;
            debugConsoleView.ItemsSource = _gameLogger.GetHistory();
            debugConsoleView.ItemTemplate = new DataTemplate(() => {
                var dateLabel = new Label();
                dateLabel.SetBinding(
                    Label.TextProperty, 
                    new Binding { 
                        Path = "Date",
                        Mode = BindingMode.OneWay,
                        StringFormat = "{0:dd.MM HH:mm:ss}"
                    }
                );
                dateLabel.Padding = new Thickness(0, 0, 10, 0);

                var messageLabel = new Label();
                messageLabel.SetBinding(Label.TextProperty, "Message");
                messageLabel.SetBinding(Label.BackgroundColorProperty, "Color");

                var duplicateMessagesCounterLabel = new Label();
                duplicateMessagesCounterLabel.SetBinding(Label.IsVisibleProperty, "ShowDuplicateMessagesCounter");
                duplicateMessagesCounterLabel.SetBinding(Label.TextProperty, "DuplicateMessagesCounter");
                duplicateMessagesCounterLabel.Padding = new Thickness(5, 0, 5, 0);
                duplicateMessagesCounterLabel.BackgroundColor = Color.FromHex("#AAAAAA");

                return new ViewCell
                {
                    View = new StackLayout
                    {
                        Padding = new Thickness(0, 2),
                        Orientation = StackOrientation.Horizontal,
                        VerticalOptions = LayoutOptions.Center,
                        Children = { dateLabel, messageLabel, duplicateMessagesCounterLabel }
                    }
                };
            });
        }

        private async void Close(object sender, System.EventArgs e)
        {
            await Navigation.PopModalAsync(false);
        }
    }
}