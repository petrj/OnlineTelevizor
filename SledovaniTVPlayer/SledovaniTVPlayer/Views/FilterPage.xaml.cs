using Android.Content;
using LoggerService;
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
    public partial class FilterPage : ContentPage
    {
        private FilterPageViewModel _viewModel;
        private ISledovaniTVConfiguration _config;

        public FilterPage(ILoggingService loggingService, ISledovaniTVConfiguration config, Context context, TVService service)
        {
            InitializeComponent();

            _config = config;
            var dialogService = new DialogService(this);

            BindingContext = _viewModel = new FilterPageViewModel(loggingService, config, dialogService, context, service);            
        }

        public string ChannelNameFilter
        {
            get
            {
                return _viewModel.ChannelNameFilter;
            }
            set
            {
                _viewModel.ChannelNameFilter = value;
            }
        }

        private async void Group_Tapped(object sender, ItemTappedEventArgs e)
        {
            await Task.Run(() =>           
            {
                var filterItem = e.Item as FilterItem;
                if (filterItem == _viewModel.Groups[0])
                {
                    _config.ChannelGroup = "*";
                }
                else
                {
                    _config.ChannelGroup = filterItem.Name;
                }
            });            
        }

        private async void Type_Tapped(object sender, ItemTappedEventArgs e)
        {
            await Task.Run(() =>
            {
                var filterItem = e.Item as FilterItem;

                if (filterItem == _viewModel.Types[0])
                {
                    _config.ChannelType = "*";
                }
                else
                {
                    _config.ChannelType = filterItem.Name;
                }
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.RefreshCommand.Execute(null);
        }
    }
}