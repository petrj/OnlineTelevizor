using Android.Views;
using LoggerService;
using NLog;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace OnlineTelevizor.Views
{
    public partial class App : Application
    {
        OnlineTelevizor.Views.MainPage _mainPage;

        public App()
        {
            InitializeComponent();

            ILoggingService loggingService;

            var context = Android.App.Application.Context;

            var config = new OnlineTelevizorConfiguration(context);

#if DEBUG
            config.DebugMode = true;
#endif

            if (config.EnableLogging)
            {
                // WRITE_EXTERNAL_STORAGE permission is disabled
#if DEBUG
                loggingService = new BasicLoggingService(config.LoggingLevel);
#else
                loggingService = new DummyLoggingService();
#endif
            } else
            {
                loggingService = new DummyLoggingService();
            }

            _mainPage = new MainPage(loggingService, config, context);

            MainPage = new NavigationPage(_mainPage);
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
