using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using Xamarin.Forms;

namespace OnlineTelevizor.ViewModels
{
    public class ChannelDetailViewModel : BaseViewModel
    {
        private ChannelItem _channel;

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

                OnPropertyChanged(nameof(EPGDescription));
                OnPropertyChanged(nameof(EPGDate));
                OnPropertyChanged(nameof(EPGTime));
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

        public String EPGDescription
        {
            get { return (Channel == null || Channel.CurrentEPGItem == null) ? null : Channel.CurrentEPGItem.Description; }
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

        public ChannelDetailViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, Context context)
            : base(loggingService, config, dialogService, context)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            _context = context;
            Config = config;
        }
    }
}
