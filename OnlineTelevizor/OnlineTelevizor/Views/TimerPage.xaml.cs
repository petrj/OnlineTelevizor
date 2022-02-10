using LoggerService;
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
    public partial class TimerPage : ContentPage
    {
        private IOnlineTelevizorConfiguration _config;
        private ILoggingService _loggingService;
        private IDialogService _dialogService;
        private TimerPageViewModel _viewModel;

        public TimerPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
        {
            InitializeComponent();

            _config = config;
            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new TimerPageViewModel(loggingService, config, dialogService);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.TimerMinutes = 0;
        }

        private void OnStepperValueChanged(object sender, ValueChangedEventArgs e)
        {
            _viewModel.TimerMinutes += Convert.ToDecimal(e.NewValue - e.OldValue);
        }

        public void IncreaseTime()
        {
            _viewModel.TimerMinutes += 10;
        }

        public void DecreaseTime()
        {
            _viewModel.TimerMinutes -= 10;
        }

        public Decimal TimerMinutes
        {
            get
            {
                return _viewModel.TimerMinutes;
            }
        }
    }
}