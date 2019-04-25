using LoggerService;
using SledovaniTVAPI;
using SledovaniTVLive.Models;
using SledovaniTVLive.Services;
using SledovaniTVLive.ViewModels;
using SledovaniTVLive.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Android.Content;
using Android.Views;

namespace SledovaniTVLive.Views
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;
        private DialogService _dialogService;
        private Context _context;
        private ISledovaniTVConfiguration _config;
        private ILoggingService _loggingService;

        public MainPage(ILoggingService loggingService, ISledovaniTVConfiguration config, Context context)
        {
            InitializeComponent();

            _dialogService = new DialogService(this);

            _config = config;
            _context = context;
            _loggingService = loggingService;

            BindingContext = _viewModel = new MainPageViewModel(loggingService, config, _dialogService, context);
        }

        public bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            Task.Run(async () => await _dialogService.Information($"Key pressed. Code: {keyCode.ToString()}, Event: {e.ToString()}"));
            return true;
        }

        private async void ToolbarItemSettings_Clicked(object sender, EventArgs e)
        {
            var settingsPage = new SettingsPage(_loggingService, _config, _context, _dialogService);
            settingsPage.Disappearing += delegate
            {
                _viewModel.ResetConnectionCommand.Execute(null);
                _viewModel.RefreshCommand.Execute(null);
            };

            await Navigation.PushAsync(settingsPage);
        }

        private async void ToolbarItemQuality_Clicked(object sender, EventArgs e)
        {
            var qualitiesPage = new QualitiesPage(_loggingService, _config, _context, _viewModel.TVService);
            
            await Navigation.PushAsync(qualitiesPage);
        }

        private async void ToolbarItemFilter_Clicked(object sender, EventArgs e)
        {
            var filterPage = new FilterPage(_loggingService, _config, _context, _viewModel.TVService);

            filterPage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };

            await Navigation.PushAsync(filterPage);
        }

        private async void Channel_Tapped(object sender, ItemTappedEventArgs e)
        {
            var channelItem = e.Item as ChannelItem;
            await _viewModel.PlayStream(channelItem.Url);
        }
    }
}
