using Android.App;
using Android.Arch.Lifecycle;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace OnlineTelevizor.Droid
{
    [Service(Label = "OnlineTelevizorService", Icon = "@drawable/Icon")]
    public class OnlineTelevizorService : Service
    {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            // application removed from app list

            MessagingCenter.Send<string>(string.Empty, BaseViewModel.StopPlayInternalNotification);
            MessagingCenter.Send<string>(string.Empty, BaseViewModel.StopRecordNotificationMessage);

            base.OnTaskRemoved(rootIntent);
        }
    }
}