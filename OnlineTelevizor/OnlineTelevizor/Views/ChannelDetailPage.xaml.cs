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
    public partial class ChannelDetailPage : ContentPage, IOnKeyDown
    {
        private ChannelDetailViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;

        public ChannelItem Channel
        {
            get
            {
                return _viewModel.Channel;
            }
            set
            {
                _viewModel.Channel = value;
            }
        }

        public ChannelDetailPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
        {
            InitializeComponent();
            _config = config;

            BindingContext = _viewModel = new ChannelDetailViewModel(loggingService, config, dialogService, service);
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    await DetailScrollView.ScrollToAsync(DetailScrollView.ScrollX, DetailScrollView.ScrollY + 10 * (1 + (int)_config.AppFontSize), false);
                    break;

                case KeyboardNavigationActionEnum.Up:
                    await DetailScrollView.ScrollToAsync(DetailScrollView.ScrollX, DetailScrollView.ScrollY - 10 * (1 + (int)_config.AppFontSize), false);
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;
            }
        }

        public void OnTextSent(string text)
        {

        }
    }
}