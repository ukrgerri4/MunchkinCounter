//using Android.Content;
//using Android.OS;
//using Android.Support.Design.BottomNavigation;
//using Android.Support.Design.Widget;
//using Android.Support.V7.Widget;
//using Android.Views;
//using MunchkinCounterLan;
//using TcpMobile.Droid.Renderers;
//using Xamarin.Forms;
//using Xamarin.Forms.Platform.Android;

//[assembly: ExportRenderer(typeof(AppShell), typeof(CustomShellRenderer))]
//namespace TcpMobile.Droid.Renderers
//{
//    public class CustomShellRenderer : ShellRenderer
//    {
//        public CustomShellRenderer(Context context) : base(context)
//        {
//        }

//        protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
//        {
//            return new AndroidBottomNavAppearance(this, shellItem);
//        }

//        protected override IShellToolbarAppearanceTracker CreateToolbarAppearanceTracker()
//        {
//            return new AndroidToolbarAppearanceTracker(this);
//        }

//        protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem)
//        {
//            return new AndroidShellItemRenderer(this);
//        }
//    }

//    public class AndroidBottomNavAppearance : ShellBottomNavViewAppearanceTracker
//    {
//        public AndroidBottomNavAppearance(IShellContext shellContext, ShellItem shellItem) : base(shellContext, shellItem)
//        {
//        }

//        public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
//        {
//            base.SetAppearance(bottomView, appearance);
//            bottomView.LayoutParameters.Height = 100;
//            bottomView.SetBackgroundColor(Android.Graphics.Color.Rgb(73, 23, 20));
//        }

//        public override void ResetAppearance(BottomNavigationView bottomView)
//        {
//            base.ResetAppearance(bottomView);
//        }
//    }

//    public class AndroidToolbarAppearanceTracker : ShellToolbarAppearanceTracker
//    {
//        public AndroidToolbarAppearanceTracker(IShellContext shellContext) : base(shellContext)
//        {
//        }

//        public override void ResetAppearance(Toolbar toolbar, IShellToolbarTracker toolbarTracker)
//        {
//            base.ResetAppearance(toolbar, toolbarTracker);
//        }

//        public override void SetAppearance(Toolbar toolbar, IShellToolbarTracker toolbarTracker, ShellAppearance appearance)
//        {
//            base.SetAppearance(toolbar, toolbarTracker, appearance);
//            //toolbar.SetBackgroundResource(Resource.Drawable.custom_gradient);
//        }
//    }

//    public class AndroidShellItemRenderer : ShellItemRenderer
//    {
//        BottomNavigationView _bottomView;

//        public AndroidShellItemRenderer(IShellContext shellContext) : base(shellContext)
//        {
//        }

//        public override Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
//        {
//            var outerlayout = base.OnCreateView(inflater, container, savedInstanceState);
//            _bottomView = outerlayout.FindViewById<BottomNavigationView>(Resource.Id.bottomtab_tabbar);
//            return outerlayout;
//        }
//    }
//}