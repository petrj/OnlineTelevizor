using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Xamarin.Forms;

namespace OnlineTelevizor.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            LoadApplication(new OnlineTelevizor.Views.App(new UWPOnlineTelevizorConfiguration(), new DummyLoggingService()));

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UriMessage, (url) =>
            {
                Task.Run(async () => await LaunchUrl(url));
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ToastMessage, (message) =>
            {
                // No support for toast messages in UWP
            });

            MessagingCenter.Subscribe<string>(string.Empty, BaseViewModel.MSG_StopPlayInternalNotificationAndQuit, (sender) =>
            {
                CoreApplication.Exit();
            });

            KeyDown += MainPage_KeyDown;
        }

        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var sendKey = false;

            switch (e.Key)
            {
                case VirtualKey.Escape:
                case VirtualKey.Enter:
                case VirtualKey.Down:
                case VirtualKey.Up:
                case VirtualKey.Left:
                case VirtualKey.Right:
                case VirtualKey.Number0:
                case VirtualKey.Number1:
                case VirtualKey.Number2:
                case VirtualKey.Number3:
                case VirtualKey.Number4:
                case VirtualKey.Number5:
                case VirtualKey.Number6:
                case VirtualKey.Number7:
                case VirtualKey.Number8:
                case VirtualKey.Number9:
                    sendKey = true; break;
            }

            if (sendKey)
            {
                MessagingCenter.Send(e.Key.ToString(), BaseViewModel.MSG_KeyMessage);
                e.Handled = true;
            }

        }

        private async Task LaunchUrl(string url)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(
              new Action(
                  async () =>
                  {
                      var uri = new Uri($"vlc://openstream/?from=url&url={System.Web.HttpUtility.UrlEncode(url)}");
                      await Launcher.LaunchUriAsync(uri);
                  }));
        }
    }
}

