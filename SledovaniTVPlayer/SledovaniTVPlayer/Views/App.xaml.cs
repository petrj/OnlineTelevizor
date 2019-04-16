using Android.Views;
using LoggerService;
using NLog;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace SledovaniTVPlayer.Views
{
    public partial class App : Application
    {
        SledovaniTVPlayer.Views.MainPage _mainPage;

        public App()
        {
            InitializeComponent();

            ILoggingService loggingService;

            var context = Android.App.Application.Context;

            var config = new SledovaniTVConfiguration(context);
            if (config.EnableLogging)
            {
                loggingService = new BasicLoggingService(config.LoggingLevel);

                var logFolder = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "SledovaniTVPlayer");
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                (loggingService as BasicLoggingService).LogFilename = Path.Combine(logFolder, "SledovaniTV.log");
            } else
            {
                loggingService = new DummyLoggingService();
            }

            _mainPage = new MainPage(loggingService, config, context);

            MainPage = new NavigationPage(_mainPage);
        }

        //public bool OnKeyDown(Keycode keyCode, KeyEvent e)
        //{
        //    return _mainPage.OnKeyDown(keyCode, e);
        //}

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
