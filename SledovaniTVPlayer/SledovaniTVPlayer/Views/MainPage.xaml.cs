using LoggerService;
using SledovaniTVAPI;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using SledovaniTVPlayer.ViewModels;
using SledovaniTVPlayer.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Android.Content;
using Android.Views;

namespace SledovaniTVPlayer.Views
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;
        private NavigationPage _settingsPage;
        private DialogService _dialogService;
        private Context _context;
        private ISledovaniTVConfiguration _config;

        public MainPage(ILoggingService loggingService, ISledovaniTVConfiguration config, Context context)
        {
            InitializeComponent();

            _dialogService = new DialogService(this);

            _settingsPage = new NavigationPage(new SettingsPage(loggingService, config, context, _dialogService));
            _settingsPage.Disappearing += _settingsPage_Disappearing;

            _config = config;
            _context = context;

            BindingContext = _viewModel = new MainPageViewModel(loggingService, config, _dialogService, context);
        }

        public bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            Task.Run(async () => await _dialogService.Information($"Key Code {keyCode.ToString()}, Key event: {e.ToString()}"));
            return true;
        }

        private void _settingsPage_Disappearing(object sender, EventArgs e)
        {
            _viewModel.ResetConnectionCommand.Execute(null);
            _viewModel.RefreshCommand.Execute(null);
        }

        private async void ToolbarItemSettings_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(_settingsPage);
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
