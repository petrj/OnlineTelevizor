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

                if (value != null)
                    Config.StreamQuality = value.Id;

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
            QualityItem firstItem = null;
            SelectedItem = null;

            foreach (var q in Qualities)
            {
                if (firstItem == null)
                {
                    firstItem = q;
                }

                if ((!String.IsNullOrEmpty(Config.StreamQuality)) && (q.Id == Config.StreamQuality))
                {
                    SelectedItem = q;
                    break;
                }
            }

            if (SelectedItem == null)
            {
                SelectedItem = firstItem;
            }
        }

        public async Task SelectPreviousItem()
        {
            await _semaphoreSlim.WaitAsync();

            await Task.Run(
                () =>
                {
                    try
                    {
                        if (Qualities.Count == 0)
                            return;

                        if (SelectedItem == null)
                        {
                            SelectedItem = Qualities[Qualities.Count-1];
                        }
                        else
                        {
                            bool next = false;

                            for (var i = Qualities.Count-1; i>=0; i--)
                            {
                                var ch = Qualities[i];

                                if (next)
                                {
                                    SelectedItem = ch;
                                    break;
                                }
                                else
                                {
                                    if (ch == SelectedItem)
                                    {
                                        next = true;

                                        if (i == 0)
                                        {
                                            SelectedItem = Qualities[Qualities.Count - 1];
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    };
                });
        }

        public async Task SelectNextItem()
        {
            await _semaphoreSlim.WaitAsync();

            await Task.Run(
                () =>
                {
                    try
                    {
                        if (Qualities.Count == 0)
                            return;

                        if (SelectedItem == null)
                        {
                            SelectedItem = Qualities[0];
                        }
                        else
                        {
                            bool next = false;

                            for (var i = 0; i < Qualities.Count; i++)
                            {
                                var ch = Qualities[i];

                                if (next)
                                {
                                    SelectedItem = ch;
                                    break;
                                }
                                else
                                {
                                    if (ch == SelectedItem)
                                    {
                                        next = true;

                                        if (i == Qualities.Count-1)
                                        {
                                            SelectedItem = Qualities[0];
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    };
                });
        }

        private async Task Refresh()
        {
            await _semaphoreSlim.WaitAsync();

            IsBusy = true;

            try
            {
                Qualities.Clear();

                Qualities.Add(new QualityItem()
                {
                    Id = "",
                    Name = "Auto"
                });

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

                Device.BeginInvokeOnMainThread(() =>
                {
                    SelectQualityByConfg();
                });
            }
        }
    }
}
