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

        public App(IOnlineTelevizorConfiguration config)
        {
            InitializeComponent();

            if (config.EnableLogging)
            {
                _loggingService = new BasicLoggingService(config.LoggingLevel);
            }
            else
            {
                _loggingService = new DummyLoggingService();
            }

            _mainPage = new MainPage(_loggingService, config);

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

