using Android.Content;
using LoggerService;
using SledovaniTVLive.Models;
using SledovaniTVLive.Services;
using SledovaniTVLive.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SledovaniTVLive.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FilterPage : ContentPage
    {
        private FilterPageViewModel _viewModel;

        public FilterPage(ILoggingService loggingService, ISledovaniTVConfiguration config, Context context, TVService service)
        {
            InitializeComponent();

            var dialogService = new DialogService(this);

            BindingContext = _viewModel = new FilterPageViewModel(loggingService, config, dialogService, context, service);            
        }
       
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.RefreshCommand.Execute(null);
        }
    }
}