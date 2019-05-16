using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using OnlineTelevizor.Services;
using OnlineTelevizor.Views;
using Plugin.Permissions;
using Plugin.InAppBilling;
using Plugin.Toast;
using Xamarin.Forms;
using OnlineTelevizor.ViewModels;
using OnlineTelevizor.Models;

namespace OnlineTelevizor.Droid
{
    [Activity(Label = "OnlineTelevizor", Icon = "@drawable/Icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private App _app;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            _app = new App(new AndroidOnlineTelevizorConfiguration());

            MessagingCenter.Subscribe<string>(this, BaseViewModel.ToastMessage, (message) =>
            {
                CrossToastPopUp.Current.ShowCustomToast(message, "#0000FF", "#FFFFFF");
            });

            LoadApplication(_app);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            InAppBillingImplementation.HandleActivityResult(requestCode, resultCode, data);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            MessagingCenter.Send(keyCode.ToString(), BaseViewModel.KeyMessage);

            return base.OnKeyDown(keyCode, e);
        }
    }
}