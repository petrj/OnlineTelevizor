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
    public partial class FilterPage : ContentPage, INavigationSelectNextItem, INavigationSendOKButton, INavigationSendBackButton
    {
        private FilterPageViewModel _viewModel;
        private View _lastFocusedView = null;

        public FilterPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, TVService service)
        {
            InitializeComponent();

            var dialogService = new DialogService(this);

            GroupPicker.Unfocused += GroupPicker_Unfocused;
            TypePicker.Unfocused += TypePicker_Unfocused;
            TypePicker.Focused += TypePicker_Focused;
            GroupPicker.Focused += GroupPicker_Focused;

            BindingContext = _viewModel = new FilterPageViewModel(loggingService, config, dialogService, service);
        }

        private void GroupPicker_Focused(object sender, FocusEventArgs e)
        {
            _lastFocusedView = GroupPicker;
        }

        private void TypePicker_Focused(object sender, FocusEventArgs e)
        {
            _lastFocusedView = TypePicker;
        }

        private void TypePicker_Unfocused(object sender, FocusEventArgs e)
        {
            if (_lastFocusedView == TypePicker)
            {
                FocusView(GroupPicker);
            }
        }

        private void GroupPicker_Unfocused(object sender, FocusEventArgs e)
        {
            if (_lastFocusedView == GroupPicker)
            {
                FocusView(ChannelNameEntry);
            }
        }

        public void SendBackButton()
        {
            if (_lastFocusedView == TypePicker)
            {
                TypePicker.Unfocus();
            }
            else
            if (_lastFocusedView == GroupPicker)
            {
                GroupPicker.Unfocus();
            }
            else
            {
                Navigation.PopAsync();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.NotifyFontSizeChange();
            _viewModel.RefreshCommand.Execute(null);
        }

        private void FocusView(View view)
        {
            if (Device.OS == TargetPlatform.Windows)
            {
                return;
            }

            ClearButton.BackgroundColor = Color.Gray;
            ClearButton.TextColor = Color.Black;

            RefreshButton.BackgroundColor = Color.Gray;
            RefreshButton.TextColor = Color.Black;

            _lastFocusedView = view;
            view.Focus();

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