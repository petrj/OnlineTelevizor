using Android.Content;
using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace OnlineTelevizor.ViewModels
{
    class StreamQualityViewModel : BaseViewModel
    {
        private TVService _service;
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public ObservableCollection<QualityItem> Qualities { get; set; } = new ObservableCollection<QualityItem>();

        public Command RefreshCommand { get; set; }

        public QualityItem SelectedItem { get; set; }

        public StreamQualityViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, Context context, TVService service)
           : base(loggingService, config, dialogService, context)
        {
            _service = service;
            _loggingService = loggingService;
            _dialogService = dialogService;
            _context = context;
            Config = config;

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

                    if ((!String.IsNullOrEmpty(Config.StreamQuality)) && (q.Id == Config.StreamQuality))
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
