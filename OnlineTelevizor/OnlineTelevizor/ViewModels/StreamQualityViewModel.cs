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
        private bool _toolBarFocused = false;

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

        public bool ToolBarFocused
        {
            get
            {
                return _toolBarFocused;
            }
            set
            {
                _toolBarFocused = value;
                OnPropertyChanged(nameof(RefreshIcon));
            }
        }

        public string RefreshIcon
        {
            get
            {
                if (ToolBarFocused)
                {
                    return "RefreshSelected.png";
                }
                else
                {
                    return "Refresh.png";
                }
            }
        }

        public QualityItem SelectedItem
        {
            get
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    return _selectedItem;
                }
                finally
                {
                    _semaphoreSlim.Release();
                };

            } set
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    if (value != null)
                        _loggingService.Debug($"Selecting item: {value.Id} {value.Name}");

                    _selectedItem = value;

                    if (value != null)
                        Config.StreamQuality = value.Id;
                }
                finally
                {
                    _semaphoreSlim.Release();
                };

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

                        if (SelectedItem == null || (Qualities.Count == 1) || (SelectedItem == Qualities[0]))
                        {
                            SelectedItem = Qualities[Qualities.Count - 1];
                        }
                        else
                        {
                            for (var i = 1; i < Qualities.Count; i++)
                            {
                                if (SelectedItem == Qualities[i])
                                {
                                    SelectedItem = Qualities[i - 1];
                                    break;
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
                    Qualities.Add(new QualityItem()
                    {
                        Id = q.Id,
                        Name = q.Name
                    });
                }
            }
            finally
            {
                IsBusy = false;

                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(Qualities));

                _semaphoreSlim.Release();

                Device.BeginInvokeOnMainThread(() =>
                {
                    SelectQualityByConfg();
                });
            }
        }
    }
}
