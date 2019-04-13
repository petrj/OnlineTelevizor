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
	public partial class QualitiesPage : ContentPage
	{
        private StreamQualityViewModel _viewModel;
        private ISledovaniTVConfiguration _config;


        public QualitiesPage (ILoggingService loggingService, ISledovaniTVConfiguration config, Context context, TVService service)
		{
			InitializeComponent ();

            _config = config;
            var dialogService = new DialogService(this);

            BindingContext = _viewModel = new StreamQualityViewModel(loggingService, config, dialogService, context, service);

            
        }
               
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.RefreshCommand.Execute(null);
        }

        private async void Quality_Tapped(object sender, ItemTappedEventArgs e)
        {
            var qualityItem = e.Item as QualityItem;
            _config.StreamQuality = qualityItem.Id;
        }
    }
}