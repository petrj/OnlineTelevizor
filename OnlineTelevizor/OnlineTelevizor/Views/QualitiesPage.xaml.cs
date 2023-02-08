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
    public partial class QualitiesPage : ContentPage, IOnKeyDown
    {
        private StreamQualityViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;
        protected ILoggingService _loggingService;

        public QualitiesPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, TVService service)
        {
            InitializeComponent();

            _config = config;
            _loggingService = loggingService;
            var dialogService = new DialogService(this);

            BindingContext = _viewModel = new StreamQualityViewModel(loggingService, config, dialogService, service);
        }

        private void FocusOrUnfocusToolBar()
        {
            _viewModel.ToolBarFocused = !_viewModel.ToolBarFocused;

            if (_viewModel.ToolBarFocused)
            {
                _viewModel.SelectedItem = null;
            }
            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                   _viewModel.SelectQualityByConfg();
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.RefreshCommand.Execute(null);
        }

        private async void Quality_Tapped(object sender, ItemTappedEventArgs e)
        {
            await Task.Run(() =>
            {
                var qualityItem = e.Item as QualityItem;
                _config.StreamQuality = qualityItem.Id;
            });
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"QualitiesPage Page OnKeyDown {key}{(longPress ? " (long)" : "")}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Left:
                    FocusOrUnfocusToolBar();
                    break;

                case KeyboardNavigationActionEnum.Down:
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await _viewModel.SelectNextItem();
                    });
                    break;

                case KeyboardNavigationActionEnum.Up:
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await _viewModel.SelectPreviousItem();
                    });
                    break;

                case KeyboardNavigationActionEnum.OK:
                    if (_viewModel.ToolBarFocused)
                    {
                        _viewModel.RefreshCommand.Execute(null);
                    }
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;
            }
        }
    }
}