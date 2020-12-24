using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor.ViewModels
{
    public class PlayerPageViewModel : BaseViewModel
    {
        private bool _videoViewVisible = true;
        private string _mediaUrl;
        private string _description;
        private string _mediaType;

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
                return GetScaledSize(20).ToString();
            }
        }

        public string FontSizeForDescription
        {
            get
            {
                return GetScaledSize(16).ToString();
            }
        }

        public PlayerPageViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
            : base(loggingService, config, dialogService)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;
        }
    }
}
