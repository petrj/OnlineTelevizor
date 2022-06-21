using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AVFoundation;
using AVKit;
using CoreGraphics;
using Foundation;
using LoggerService;
using MediaPlayer;
using OnlineTelevizor.Models;
using OnlineTelevizor.ViewModels;
using OnlineTelevizor.Views;
using UIKit;
using Xamarin.Forms;

namespace OnlineTelevizor.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init();

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UriMessage, (url) =>
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(
                    delegate
                    {
                        // working, but asking user for download or play:
                        Device.OpenUri(new System.Uri($"vlc://{url}"));

                        /*
                        // does not work:
                        Device.OpenUri(new System.Uri($"videos://{url}"));
                        */

                        /* asking user for realm username and password:
                        app.OpenUrl(new System.Uri($"vlc-x-callback://x-callback-url/stream?url={url}"));
                        */

                        /*
                        // does not work 
                        var asset = AVAsset.FromUrl(new NSUrl(url));
                        var playerItem = new AVPlayerItem(asset);
                        var avPlayer = new AVPlayer(playerItem);
                        var playerLayer = AVPlayerLayer.FromPlayer(avPlayer);
                        playerLayer.Frame = app.KeyWindow.Frame;
                        app.KeyWindow.Layer.AddSublayer(playerLayer);
                        avPlayer.Play();                             
                        */

                        /*
                        // working, but streams not start (only empty video window shown, does MPMoviePlayerViewController support mp4?)

                        var moviePlayer = new MPMoviePlayerViewController(new NSUrl(url));
                        //set appearance of video player
                        moviePlayer.View.Frame = new RectangleF(10, 80, 300, 200);
                        moviePlayer.View.BackgroundColor = UIColor.Blue;
                        moviePlayer.MoviePlayer.SourceType = MPMovieSourceType.Streaming;
                        // Set this property True if you want the video to be auto played on page load
                        moviePlayer.MoviePlayer.ShouldAutoplay = true;
                        // If you want to keep the Video player on-ready-to-play state, then enable this
                        // This will keep the video content loaded from the URL, untill you play it.
                        moviePlayer.MoviePlayer.PrepareToPlay();
                        // Enable the embeded video controls of the Video Player, this has several types of Embedded controls for you to choose
                        moviePlayer.MoviePlayer.ControlStyle = MPMovieControlStyle.Default;

                        app.KeyWindow.AddSubview(moviePlayer.View);

                        //moviePlayer.MoviePlayer.Play();
                        */
                    });
            });            

            LoadApplication(new App(new IOSOnlineTelevizorConfiguration(), new DummyLoggingService()));

            return base.FinishedLaunching(app, options);
        }
    }
}
