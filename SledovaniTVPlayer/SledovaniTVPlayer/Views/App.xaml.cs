using LoggerService;
using NLog;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace SledovaniTVPlayer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var loggingService = new BasicLoggingService();
            loggingService.LogFilename = "/storage/emulated/0/Download/SledovaniTV.txt";
            var context = Android.App.Application.Context;

            var config = new SledovaniTVConfiguration(context);

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
