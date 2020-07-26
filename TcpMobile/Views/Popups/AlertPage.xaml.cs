using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MunchkinCounterLan.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AlertPage : PopupPage
    {
        public event EventHandler OnConfirm;
        public string AlertText { get; set; }
        public string ConfirmText { get; set; }
        public string RejectText { get; set; }
        public bool IsRejectButtonVisible => !string.IsNullOrWhiteSpace(RejectText);

        public AlertPage(string alertText, string confirmText = "Ok", string rejectText = null)
        {
            AlertText = !string.IsNullOrWhiteSpace(alertText) ? alertText : "";
            ConfirmText = confirmText;
            RejectText = rejectText;

            InitializeComponent();

            BindingContext = this;
        }

        private async void ConfirmClicked(object sender, EventArgs e)
        {
            OnConfirm?.Invoke(this, null);
            await PopupNavigation.Instance.PopAsync();
        }

        private async void RejectClicked(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}