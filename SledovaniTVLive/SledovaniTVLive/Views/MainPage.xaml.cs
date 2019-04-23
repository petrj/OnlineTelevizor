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
        private SettingsPage _settingsPage;
        private DialogService _dialogService;
        private Context _context;
        private ISledovaniTVConfiguration _config;
        private ILoggingService _loggingService;

        public MainPage(ILoggingService loggingService, ISledovaniTVConfiguration config, Context context)
        {
            InitializeComponent();

            _dialogService = new DialogService(this);

            _settingsPage = new SettingsPage(loggingService, config, context, _dialogService);
            _settingsPage.Disappearing += _settingsPage_Disappearing;

            _config = config;
            _context = context;

            BindingContext = _viewModel = new MainPageViewModel(loggingService, config, _dialogService, context);
        }

        public bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            Task.Run(async () => await _dialogService.Information($"Key pressed. Code: {keyCode.ToString()}, Event: {e.ToString()}"));
            return true;
        }

        private void _settingsPage_Disappearing(object sender, EventArgs e)
        {
            _viewModel.ResetConnectionCommand.Execute(null);
            _viewModel.RefreshCommand.Execute(null);
        }

        private async void ToolbarItemSettings_Clicked(object sender, EventArgs e)
        {
            //await Navigation.PushModalAsync(_settingsPage);
            await _viewModel.NavigateToPage(_settingsPage, Navigation);
        }

        private async void ToolbarItemQuality_Clicked(object sender, EventArgs e)
        {
            var qualitiesPage = new QualitiesPage(_loggingService, _config, _context, _viewModel.TVService);
            await _viewModel.NavigateToPage(qualitiesPage, Navigation);
        }        

        private async void ToolbarItemFilter_Clicked(object sender, EventArgs e)
        {
            var filterPage = new FilterPage(_loggingService, _config, _context, _viewModel.TVService);
            filterPage.ChannelNameFilter = _viewModel.ChannelNameFilter;

            filterPage.Disappearing += delegate
            {
                _viewModel.ChannelNameFilter = filterPage.ChannelNameFilter;
                _viewModel.RefreshCommand.Execute(null);
            };

            await _viewModel.NavigateToPage(filterPage, Navigation);
        }        

        private async void Channel_Tapped(object sender, ItemTappedEventArgs e)
        {
            var channelItem = e.Item as ChannelItem;

            //var dlgResult = await _dialogService.Confirm($"Stream {channel.Name}?");
            //if (dlgResult)
            //{
            //    ((ListView)sender).SelectedItem = null;
            //    await _viewModel.PlayStream(channel.Url);
            //}

            await _viewModel.PlayStream(channelItem.Url);
        }
    }
}
