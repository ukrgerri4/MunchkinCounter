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
        public event EventHandler Confirmed;
        public event EventHandler Rejected;

        public string AlertText { get; set; }
        public string ConfirmText { get; set; }
        public string RejectText { get; set; }
        public bool IsRejectButtonVisible => !string.IsNullOrWhiteSpace(RejectText);
        public bool CloseOnConfirm { get; set; }

        public AlertPage(string alertText, string confirmText = "Ok", string rejectText = null, bool closeOnConfirm = true)
        {
            AlertText = !string.IsNullOrWhiteSpace(alertText) ? alertText : "";
            ConfirmText = confirmText;
            RejectText = rejectText;
            CloseOnConfirm = closeOnConfirm;

            InitializeComponent();

            BindingContext = this;
        }

        private async void ConfirmClicked(object sender, EventArgs e)
        {
            Confirmed?.Invoke(this, null);
            if (CloseOnConfirm)
            {
                await PopupNavigation.Instance.PopAsync();
            }
        }

        private async void RejectClicked(object sender, EventArgs e)
        {
            Rejected?.Invoke(this, null);
            await PopupNavigation.Instance.PopAsync();
        }
    }
}