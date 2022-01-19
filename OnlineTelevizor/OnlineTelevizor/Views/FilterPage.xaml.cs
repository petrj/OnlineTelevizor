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
    public partial class FilterPage : ContentPage
    {
        private FilterPageViewModel _viewModel;
        private View _lastFocusedView = null;

        public FilterPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, TVService service)
        {
            InitializeComponent();

            var dialogService = new DialogService(this);

            GroupPicker.Unfocused += GroupPicker_Unfocused;
            TypePicker.Unfocused += TypePicker_Unfocused;

            BindingContext = _viewModel = new FilterPageViewModel(loggingService, config, dialogService, service);
        }

        private void TypePicker_Unfocused(object sender, FocusEventArgs e)
        {
            FocusView(GroupPicker);
        }

        private void GroupPicker_Unfocused(object sender, FocusEventArgs e)
        {
            FocusView(ChannelNameEntry);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.NotifyFontSizeChange();
            _viewModel.RefreshCommand.Execute(null);
        }

        private void FocusView(View view)
        {
            ClearButton.BackgroundColor = Color.Gray;
            ClearButton.TextColor = Color.Black;

            RefreshButton.BackgroundColor = Color.Gray;
            RefreshButton.TextColor = Color.Black;

            view.Focus();
            _lastFocusedView = view;

            if (view is Button)
            {
                (view as Button).BackgroundColor = Color.Blue;
                (view as Button).TextColor = Color.White;
            }
        }

        public void SendOKButton()
        {
            if (_lastFocusedView == null)
                return;

            if (_lastFocusedView is Button)
            {
                if (_lastFocusedView == ClearButton)
                    _viewModel.ClearFilterCommand.Execute(null);

                if (_lastFocusedView == RefreshButton)
                    _viewModel.RefreshCommand.Execute(null);
            }
        }

        public void SelectNextItem()
        {
            if (_lastFocusedView == FavouriteSwitch)
            {
                FocusView(TypePicker);
            }
            else
          if (_lastFocusedView == TypePicker)
            {
                FocusView(GroupPicker);
            }
            else
          if (_lastFocusedView == GroupPicker)
            {
                FocusView(ChannelNameEntry);
            }
            else
          if (_lastFocusedView == ChannelNameEntry)
            {
                FocusView(RefreshButton);
            }
            else
          if (_lastFocusedView == RefreshButton)
            {
                FocusView(ClearButton);
            }
            else
          if (_lastFocusedView == ClearButton)
            {
                FocusView(FavouriteSwitch);
            }
            else
            {
                FocusView(FavouriteSwitch);
            }
        }
    }
}