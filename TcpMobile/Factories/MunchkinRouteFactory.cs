using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TcpMobile.Views;
using Xamarin.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace TcpMobile.Factories
{
    public class MunchkinRouteFactory : RouteFactory
    {
        private Type _type;
        private readonly IServiceProvider _serviceProvider;

        public MunchkinRouteFactory(Type type, IServiceProvider serviceProvider)
        {
            _type = type;
            _serviceProvider = serviceProvider;
        }

        public override Element GetOrCreate()
        {
            return _serviceProvider.GetService(_type) is Element element ? element : null;
        }
    }
}
