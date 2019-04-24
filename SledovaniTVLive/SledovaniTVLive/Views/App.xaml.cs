using Android.Views;
using LoggerService;
using NLog;
using SledovaniTVLive.Models;
using SledovaniTVLive.Services;
using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace SledovaniTVLive.Views
{
    public partial class App : Application
    {
        SledovaniTVLive.Views.MainPage _mainPage;

        public App()
        {
            InitializeComponent();

            ILoggingService loggingService;

            var context = Android.App.Application.Context;

            var config = new SledovaniTVConfiguration(context);
#if DEBUG            
            config.DebugMode = true;
#endif
            if (config.EnableLogging)
            {
                loggingService = new BasicLoggingService(config.LoggingLevel);

                var logFolder = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "SledovaniTVLive");

                (loggingService as BasicLoggingService).LogFilename = Path.Combine(logFolder, "SledovaniTVLive.log");
            } else
            {
                loggingService = new DummyLoggingService();
            }

            _mainPage = new MainPage(loggingService, config, context);

            MainPage = new NavigationPage(_mainPage);
        }

        public bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return _mainPage.OnKeyDown(keyCode, e);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
