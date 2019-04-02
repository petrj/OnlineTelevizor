using LoggerService;
using NLog;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using SledovaniTVPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SledovaniTVPlayer.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        private SettingsViewModel _viewModel;
        private ISledovaniTVConfiguration _config;

        public SettingsPage(ILoggingService loggingService, ISledovaniTVConfiguration config)
        {
            InitializeComponent();

            _config = config;

            BindingContext = _viewModel = new SettingsViewModel(loggingService, config);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            //NavigationPage.SetHasNavigationBar(this, true);
            //NavigationPage.SetHasBackButton(this, true);
        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }
    }
}