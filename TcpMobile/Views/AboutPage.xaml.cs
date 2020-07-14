using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : PopupPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private async void Close(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}