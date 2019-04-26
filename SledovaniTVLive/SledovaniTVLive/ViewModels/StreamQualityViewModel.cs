using Android.Content;
using LoggerService;
using SledovaniTVLive.Models;
using SledovaniTVLive.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SledovaniTVLive.ViewModels
{
    class StreamQualityViewModel : BaseViewModel
    {
        private TVService _service;
        private ISledovaniTVConfiguration _config;
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public ObservableCollection<QualityItem> Qualities { get; set; } = new ObservableCollection<QualityItem>();

        public Command RefreshCommand { get; set; }

        public QualityItem SelectedItem { get; set; }

        public StreamQualityViewModel(ILoggingService loggingService, ISledovaniTVConfiguration config, IDialogService dialogService, Context context, TVService service)
           : base(loggingService, config, dialogService, context)
        {
            _service = service;
            _loggingService = loggingService;
            _dialogService = dialogService;
            _context = context;
            _config = config;

            RefreshCommand = new Command(async () => await Refresh());
        }

        private async Task Refresh()
        {
            await _semaphoreSlim.WaitAsync();

            IsBusy = true;

            try
            {
                Qualities.Clear();

                var qualities = await _service.GetStreamQualities();

                foreach (var q in qualities)
                {
                    Qualities.Add(q);
              
                    if ((!String.IsNullOrEmpty(_config.StreamQuality)) && (q.Id == _config.StreamQuality))
                    {
                        SelectedItem = q;
                    }                    
                }
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(SelectedItem));

                _semaphoreSlim.Release();
            }
        }
    }
}
