using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TVAPI;
using Xamarin.Forms;
using static Android.OS.PowerManager;

namespace OnlineTelevizor.ViewModels
{
    public class PlayerPageViewModel : BaseViewModel
    {
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private TVService _service;
        private bool _videoViewVisible = true;
        private string _mediaUrl;
        private string _description;
        private string _timeDescription;
        private string _detailedDescription;
        private string _logoIcon;
        private string _mediaType;
        private string _channelId;
        private EPGItem _epgItem;
        private int _animePos = 2;
        private bool _animePosIncreasing = true;
        private double _EPGProgress = 0;


        public Command RefreshCommand { get; set; }


        public string AudioIcon
        {
            get
            {
                return "Audio" + _animePos.ToString();
            }
        }

        public string LogoIcon
        {
            get
            {
                return _logoIcon;
            }
            set
            {
                _logoIcon = value;

                OnPropertyChanged(nameof(LogoIcon));
            }
        }

        public void Anime()
        {
            if (_animePosIncreasing)
            {
                _animePos++;
                if (_animePos > 3)
                {
                    _animePos = 2;
                    _animePosIncreasing = !_animePosIncreasing;
                }
            } else
            {
                _animePos--;
                if (_animePos < 0)
                {
                    _animePos = 1;
                    _animePosIncreasing = !_animePosIncreasing;
                }
            }

            try
            {
                OnPropertyChanged(nameof(AudioIcon));
            } catch {  /* UWP platform fix */ }
        }

        public string MediaUrl
        {
            get
            {
                return _mediaUrl;
            }
            set
            {
                _mediaUrl = value;

                OnPropertyChanged(nameof(MediaUrl));
            }
        }

        public EPGItem EPGItem
        {
            get
            {
                return _epgItem;
            }
            set
            {
                _epgItem = value;

                OnPropertyChanged(nameof(EPGItem));
            }
        }

        public string ChannelId
        {
            get
            {
                return _channelId;
            }
            set
            {
                _channelId = value;

                OnPropertyChanged(nameof(ChannelId));
            }
        }

        public string MediaType
        {
            get
            {
                return _mediaType;
            }
            set
            {
                _mediaType = value;

                OnPropertyChanged(nameof(MediaType));
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;

                OnPropertyChanged(nameof(Description));
            }
        }

        public Color EPGProgressBackgroundColor
        {
            get
            {
                return Color.White;
            }
        }

        public double EPGProgress
        {
            get
            {
                return _EPGProgress;
            }
            set
            {
                _EPGProgress = value;

                OnPropertyChanged(nameof(EPGProgress));
            }
        }

        public string TimeDescription
        {
            get
            {
                return _timeDescription;
            }
            set
            {
                _timeDescription = value;

                OnPropertyChanged(nameof(TimeDescription));
            }
        }

        public string DetailedDescription
        {
            get
            {
                return _detailedDescription;
            }
            set
            {
                _detailedDescription = value;

                OnPropertyChanged(nameof(DetailedDescription));
            }
        }

        public bool VideoViewVisible
        {
            get
            {
                return _videoViewVisible;
            }
            set
            {
                _videoViewVisible = value;

                OnPropertyChanged(nameof(VideoViewVisible));
                OnPropertyChanged(nameof(AudioViewVisible));
            }
        }

        public bool AudioViewVisible
        {
            get
            {
                return !_videoViewVisible;
            }
            set
            {
                _videoViewVisible = !value;

                OnPropertyChanged(nameof(VideoViewVisible));
                OnPropertyChanged(nameof(AudioViewVisible));
            }
        }

        public string FontSizeForChannel
        {
            get
            {
                return GetScaledSize(22).ToString();
            }
        }

        public string FontSizeForDescription
        {
            get
            {
                return GetScaledSize(18).ToString();
            }
        }

        public string FontSizeForDetailedDescription
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public PlayerPageViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
            : base(loggingService, config, dialogService)
        {
            _service = service;
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;

            RefreshCommand = new Command(async () => await Refresh());

            BackgroundCommandWorker.RunInBackground(RefreshCommand, 30, 15);
        }

        private async Task Refresh()
        {
            await _semaphoreSlim.WaitAsync();

            IsBusy = true;

            try
            {

                var epg = await _service.GetEPG();

                if (epg != null)
                {
                    if (epg.ContainsKey(_channelId) && epg[_channelId] != null)
                    {
                        foreach (var epgItem in epg[_channelId])
                        {
                            if (epgItem.Finish < DateTime.Now || epgItem.Start > DateTime.Now)
                                continue; // only current programs

                            EPGItem = epgItem;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while refreshing epg");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsBusy));

                _semaphoreSlim.Release();
            }
        }
    }
}
