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
    public partial class ChannelDetailPage : ContentPage
    {
        private ChannelDetailViewModel _viewModel;

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

        public ChannelDetailPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
        {
            InitializeComponent();

            BindingContext = _viewModel = new ChannelDetailViewModel(loggingService, config, dialogService);
        }
    }
}