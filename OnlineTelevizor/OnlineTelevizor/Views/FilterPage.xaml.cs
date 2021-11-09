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
            FocusView(ChannelNameEntry);
        }

        private void GroupPicker_Unfocused(object sender, FocusEventArgs e)
        {
            FocusView(TypePicker);;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.NotifyFontSizeChange();
            _viewModel.RefreshCommand.Execute(null);
        }

        private void FocusView(View view)
        {
            view.Focus();
            _lastFocusedView = view;
        }

        public void SelectNextItem()
        {
            if (_lastFocusedView == ChannelNameEntry)
            {
                FocusView(GroupPicker);
            }
            else
            if (_lastFocusedView == GroupPicker)
            {
                FocusView(TypePicker);
            }
            else
            {
                FocusView(ChannelNameEntry);
            }
        }
    }
}