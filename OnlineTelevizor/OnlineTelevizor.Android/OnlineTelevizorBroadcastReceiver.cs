﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using OnlineTelevizor.ViewModels;
using OnlineTelevizor.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace OnlineTelevizor.Droid
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { "net.petrjanousek.net.OnlineTelevizorBroadcastReceiver" })]
    public class OnlineTelevizorBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                if (intent.Action == "Stop")
                {
                    MessagingCenter.Send<string>(string.Empty, BaseViewModel.StopPlay);
                }
                if (intent.Action == "Quit")
                {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}