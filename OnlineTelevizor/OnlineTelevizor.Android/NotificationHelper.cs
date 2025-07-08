using Android.App;
using Android.Content;
using Android.OS;
using OnlineTelevizor.Droid;
using AndroidX.Core.App;

public class NotificationHelper : ContextWrapper
{
    public const string ChannelId = "default_channel";
    private const string ChannelName = "Default Channel";

    NotificationManager _notificationManager;

    private NotificationManager NotificationManager =>
        _notificationManager ??= (NotificationManager)GetSystemService(NotificationService);

    public NotificationHelper(Context context) : base(context)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
            {
                LockscreenVisibility = NotificationVisibility.Public
            };

            // Set only the first time — must reinstall app to change settings
            channel.EnableVibration(false);
            channel.SetSound(null, null);

            NotificationManager.CreateNotificationChannel(channel);
        }
    }

    public void ShowPlayNotification(int notificationId, string title, string body, string detail)
    {
        var launchIntent = Application.Context.PackageManager?.GetLaunchIntentForPackage(Application.Context.PackageName);
        launchIntent?.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        var pendingIntentFlags = PendingIntentFlags.CancelCurrent | PendingIntentFlags.Immutable;
        var contentPendingIntent = PendingIntent.GetActivity(Application.Context, notificationId, launchIntent, pendingIntentFlags);

        // STOP ACTION
        var stopIntent = new Intent(Application.Context, typeof(OnlineTelevizorBroadcastReceiver)).SetAction("Stop");
        var stopPendingIntent = PendingIntent.GetBroadcast(Application.Context, notificationId + 1000, stopIntent, pendingIntentFlags);

        // QUIT ACTION
        var quitIntent = new Intent(Application.Context, typeof(OnlineTelevizorBroadcastReceiver)).SetAction("Quit");
        var quitPendingIntent = PendingIntent.GetBroadcast(Application.Context, notificationId + 2000, quitIntent, pendingIntentFlags);

        var notification = new NotificationCompat.Builder(ApplicationContext, ChannelId)
            .SetContentTitle(body)
            .SetContentText(detail)
            .SetSubText(title)
            .SetSmallIcon(Resource.Drawable.SmallIcon)
            .SetAutoCancel(false)
            .SetOngoing(true)
            .SetSound(null)
            .SetVibrate(new long[] { 0 })
            .AddAction(new NotificationCompat.Action(Resource.Drawable.Stop, "Zastavit přehrávání", stopPendingIntent))
            .AddAction(new NotificationCompat.Action(Resource.Drawable.Quit, "Ukončit", quitPendingIntent))
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetContentIntent(contentPendingIntent)
            .Build();

        NotificationManager.Notify(notificationId, notification);
    }

    public void ShowRecordNotification(int notificationId, string title, string body, string detail)
    {
        var launchIntent = Application.Context.PackageManager?.GetLaunchIntentForPackage(Application.Context.PackageName);
        launchIntent?.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        var pendingIntentFlags = PendingIntentFlags.CancelCurrent | PendingIntentFlags.Immutable;
        var contentPendingIntent = PendingIntent.GetActivity(Application.Context, notificationId, launchIntent, pendingIntentFlags);

        var stopIntent = new Intent(Application.Context, typeof(OnlineTelevizorBroadcastReceiver)).SetAction("StopRecord");
        var stopPendingIntent = PendingIntent.GetBroadcast(Application.Context, notificationId + 1000, stopIntent, pendingIntentFlags);

        var notification = new NotificationCompat.Builder(ApplicationContext, ChannelId)
            .SetContentTitle(body)
            .SetContentText(detail)
            .SetSubText(title)
            .SetSmallIcon(Resource.Drawable.SmallIcon)
            .SetAutoCancel(false)
            .SetOngoing(true)
            .SetSound(null)
            .SetVibrate(new long[] { 0 })
            .AddAction(new NotificationCompat.Action(Resource.Drawable.Stop, "Zastavit nahrávání", stopPendingIntent))
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetContentIntent(contentPendingIntent)
            .Build();

        NotificationManager.Notify(notificationId, notification);
    }

    public void CloseNotification(int notificationId)
    {
        NotificationManager.Cancel(notificationId);
    }
}
