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
	public partial class QualitiesPage : ContentPage
	{
        private StreamQualityViewModel _viewModel;
        private IOnlineTelevizorConfiguration _config;


        public QualitiesPage (ILoggingService loggingService, IOnlineTelevizorConfiguration config, TVService service)
		{
			InitializeComponent ();

            _config = config;
            var dialogService = new DialogService(this);

            BindingContext = _viewModel = new StreamQualityViewModel(loggingService, config, dialogService, service);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.RefreshCommand.Execute(null);
        }

        private async void Quality_Tapped(object sender, ItemTappedEventArgs e)
        {
            await Task.Run( () =>
            {
                var qualityItem = e.Item as QualityItem;
                _config.StreamQuality = qualityItem.Id;
            });
        }
    }
}