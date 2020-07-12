using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpMobile.Factories;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TcpMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainShell : Shell
    {
        private readonly IServiceProvider _serviceProvider;

        public MainShell(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            InitializeComponent();

            Routing.RegisterRoute("/single-page", new MunchkinRouteFactory(typeof(SingleGamePage), _serviceProvider));
        }
    }
}