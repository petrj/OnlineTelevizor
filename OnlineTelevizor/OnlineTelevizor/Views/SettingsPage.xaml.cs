using Android.Content;
using LoggerService;
using NLog;
using Plugin.InAppBilling;
using Plugin.InAppBilling.Abstractions;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        private SettingsViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        IDialogService _dialogService;

        public SettingsPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, Context context, IDialogService dialogService)
        {
            InitializeComponent();

            _config = config;
            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new SettingsViewModel(loggingService, config, context, dialogService);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }  
    }
}