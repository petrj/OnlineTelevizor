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
        private IOnlineTelevizorConfiguration _config;
        private string _appVersion = String.Empty;

        public App(IOnlineTelevizorConfiguration config, ILoggingService loggingService)
        {
            InitializeComponent();

            _config = config;
            _loggingService = loggingService;

            _loggingService.Info($"App constructor");

            _mainPage = new MainPage(_loggingService, _config);

            _mainPage.SubscribeMessages();

            MainPage = new NavigationPage(_mainPage);
        }

        ~App()
        {
            _mainPage.UnsubscribeMessages();
        }

        public string AppVersion
        {
            get
            {
                return _appVersion;
            }
            set
            {
                _appVersion = value;
                if (_mainPage != null)
                    _mainPage.AppVersion = value;
            }
        }

        protected override void OnStart()
        {
            _loggingService.Info($"OnStart");

            _mainPage.RefreshWithnotification();
        }


        protected override void OnSleep()
        {
            _loggingService.Info($"OnSleep");

            if (!_config.PlayOnBackground)
            {
                _mainPage.ActionStop(true);
            }

            _lastSleep = DateTime.Now;
        }

        protected override void OnResume()
        {
            _loggingService.Info($"OnResume");

            //_mainPage.Resume();

            // refresh only when resume after 1 minute
            if ((DateTime.Now - _lastSleep).TotalMinutes > 1)
            {
                _mainPage.Reset();
                _mainPage.Refresh();
            }
        }
    }
}

