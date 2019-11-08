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

        public QualityItem _selectedItem;

        public StreamQualityViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
           : base(loggingService, config, dialogService)
        {
            _service = service;
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;

            RefreshCommand = new Command(async () => await Refresh());
        }

        public QualityItem SelectedItem
        {
            get
            {
                return _selectedItem;
            } set
            {
                _selectedItem = value;

                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public string FontSizeForQualityItem
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public void SelectQualityByConfg()
        {
            foreach (var q in Qualities)
            {
                if ((!String.IsNullOrEmpty(Config.StreamQuality)) && (q.Id == Config.StreamQuality))
                {
                    SelectedItem = q;
                    break;
                }
            }
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
                }
            }
            finally
            {
                IsBusy = false;

                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(Qualities));

                SelectQualityByConfg();
            }
        }
    }
}
