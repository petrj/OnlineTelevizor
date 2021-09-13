using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineTelevizor.Droid
{
    // https://github.com/xamarin/monodroid-samples
    public class NotificationHelper : ContextWrapper
    {
        public const string _channelId = "default";
        public const int _notificationId = 1;
        public const int noti_channel_default = 2131165200;
        NotificationManager _notificationManager;

        private NotificationManager NotificationManager
        {
            get
            {
                if (_notificationManager == null)
                {
                    _notificationManager = (NotificationManager)GetSystemService(NotificationService);
                }
                return _notificationManager;
            }
        }

        int SmallIcon => Android.Resource.Drawable.StatNotifyChat;

        public NotificationHelper(Context context) : base(context)
        {
            var channel = new NotificationChannel(_channelId, GetString(noti_channel_default), NotificationImportance.Default);
            channel.LightColor = Color.Green;
            channel.LockscreenVisibility = NotificationVisibility.Private;
            NotificationManager.CreateNotificationChannel(channel);
        }

        public void ShowNotification(string title, string body)
        {
            var notificationBuilder = new Notification.Builder(ApplicationContext, _channelId)
                     .SetContentTitle(title)
                     .SetContentText(body)
                     .SetSmallIcon(SmallIcon)
                     .SetAutoCancel(false)
                     //.SetPriority(NotificationCompat.PriorityLow)
                     //.SetVibrate(new long[] { 0,0} )
                     .SetVisibility(NotificationVisibility.Public);

            var notificationIntent = Application.Context.PackageManager?.GetLaunchIntentForPackage(Application.Context.PackageName);

            notificationIntent.SetFlags(ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(Application.Context, _notificationId, notificationIntent,
                PendingIntentFlags.CancelCurrent);
            notificationBuilder.SetContentIntent(pendingIntent);

            NotificationManager.Notify(_notificationId, notificationBuilder.Build());
        }

        public void CloseNotification()
        {
            NotificationManager.Cancel(_notificationId);
        }
    }
}