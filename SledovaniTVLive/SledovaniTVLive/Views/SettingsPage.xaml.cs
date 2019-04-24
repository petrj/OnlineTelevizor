using Android.Content;
using LoggerService;
using NLog;
using SledovaniTVLive.Models;
using SledovaniTVLive.Services;
using SledovaniTVLive.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SledovaniTVLive.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        private SettingsViewModel _viewModel;
        private ISledovaniTVConfiguration _config;

        public SettingsPage(ILoggingService loggingService, ISledovaniTVConfiguration config, Context context, IDialogService dialogService)
        {
            InitializeComponent();

            _config = config;

            BindingContext = _viewModel = new SettingsViewModel(loggingService, config, context, dialogService);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }
    }
}