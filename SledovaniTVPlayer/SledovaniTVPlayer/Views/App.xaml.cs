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

            MainPage = new NavigationPage(new MainPage(loggingService, config, context));
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
