using LoggerService;
using NLog;
using Plugin.InAppBilling;
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
using System.Collections.ObjectModel;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HelpPage : ContentPage
    {
        private HelpViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        private IDialogService _dialogService;

        public HelpPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
        {
            InitializeComponent();
            _config = config;
            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new HelpViewModel(loggingService, config, dialogService, service);
        }
    }
}