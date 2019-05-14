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
        private DateTime _lastSleep = DateTime.MinValue;
        protected ILoggingService _loggingService;

        public App()
        {
            InitializeComponent();

            var context = Android.App.Application.Context;

            var config = new OnlineTelevizorConfiguration(context);

#if DEBUG
            config.DebugMode = true;
#endif

            if (config.EnableLogging)
            {
                // WRITE_EXTERNAL_STORAGE permission is disabled
#if DEBUG
                //loggingService = new TCPIPLoggingService("http://88.103.80.48:8100", config.LoggingLevel);
                _loggingService = new BasicLoggingService(config.LoggingLevel);
#else                 
                _loggingService = new DummyLoggingService();
#endif
            }
            else
            {
                _loggingService = new DummyLoggingService();
            }

            _mainPage = new MainPage(_loggingService, config, context);

            MainPage = new NavigationPage(_mainPage);
        }

        protected override void OnStart()
        {
            _mainPage.Reset();
        }

        protected override void OnSleep()
        {
            _loggingService.Info($"OnSleep");

            _lastSleep = DateTime.Now;
        }

        protected override void OnResume()
        {
            _loggingService.Info($"OnResume");

            // refresh only when resume after 1 minute

            if ((DateTime.Now - _lastSleep).TotalMinutes > 1)
            {
                _mainPage.Reset();
                _mainPage.Refresh();
            }
        }
    }
}
