using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using Xamarin.Forms;

namespace OnlineTelevizor.ViewModels
{
    public class ChannelDetailViewModel : BaseViewModel
    {
        private ChannelItem _channel;
        private string _EPGDescription = string.Empty;
        private TVService _service;

        public ChannelItem Channel
        {
            get
            {
                return _channel;
            }
            set
            {
                _channel = value;

                OnPropertyChanged(nameof(ChannelName));
                OnPropertyChanged(nameof(LogoUrl));
                OnPropertyChanged(nameof(EPGTitle));

                OnPropertyChanged(nameof(EPGDate));
                OnPropertyChanged(nameof(EPGTime));
                OnPropertyChanged(nameof(EPGTimeStart));
                OnPropertyChanged(nameof(EPGTimeFinish));
                OnPropertyChanged(nameof(EPGProgress));
                OnPropertyChanged(nameof(VideoDescription));

                Task.Run( async () => await UpdateChannelEPGDescription());
            }
        }

        public string FontSizeForChannel
        {
            get
            {
                return GetScaledSize(20).ToString();
            }
        }

        public string FontSizeForTitle
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
                return GetScaledSize(16).ToString();
            }
        }

        public string FontSizeForVideoDescription
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public string FontSizeForDatetime
        {
            get
            {
                return GetScaledSize(16).ToString();
            }
        }

        public string ChannelName
        {
            get { return Channel == null ? string.Empty : Channel.Name; }
        }

        public string LogoUrl
        {
            get { return Channel == null ? string.Empty : Channel.LogoUrl; }
        }

        public string EPGTitle
        {
            get { return Channel == null ? string.Empty : Channel.CurrentEPGTitle; }
        }

        public double EPGProgress
        {
            get { return Channel == null || Channel.CurrentEPGItem == null ? 0 : Channel.CurrentEPGItem.Progress; }
        }

        public String EPGDescription
        {
            get
            {
                return _EPGDescription;
            }
            set
            {
                _EPGDescription = value;
                OnPropertyChanged(nameof(EPGDescription));
            }
        }

        public String VideoDescription
        {
            get
            {
                return Channel == null || String.IsNullOrEmpty(Channel.VideoTrackDescription) ? string.Empty : Channel.VideoTrackDescription;
            }
        }

        public string EPGDate
        {
            get { return (Channel == null || Channel.CurrentEPGItem == null) ? null : Channel.CurrentEPGItem.Start.ToString("d.M."); }
        }

        public string EPGTime
        {
            get
            {
                return (Channel == null || Channel.CurrentEPGItem == null)
                    ? null
                    : Channel.CurrentEPGItem.Start.ToString("HH:mm")
                        + " - " +
                      Channel.CurrentEPGItem.Finish.ToString("HH:mm");
            }
        }

        public string EPGTimeStart
        {
            get
            {
                return (Channel == null || Channel.CurrentEPGItem == null)
                    ? null
                    : Channel.EPGTimeStart;
            }
        }

        public string EPGTimeFinish
        {
            get
            {
                return (Channel == null || Channel.CurrentEPGItem == null)
                    ? null
                    : Channel.EPGTimeFinish;
            }
        }

        public ChannelDetailViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
            : base(loggingService, config, dialogService)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            _service = service;
            Config = config;
        }

        private async Task UpdateChannelEPGDescription()
        {
            if (Channel == null || Channel.CurrentEPGItem == null)
            {
                EPGDescription = String.Empty;
                return;
            }

            EPGDescription = await _service.GetEPGItemDescription(Channel.CurrentEPGItem);
        }
    }
}
