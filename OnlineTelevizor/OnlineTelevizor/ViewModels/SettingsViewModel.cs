using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Reflection;
using Xamarin.Essentials;

namespace OnlineTelevizor.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool _isPruchased;
        private bool _showSledovaniPairedDevice = false;
        private TVService _service;

        public Command StopStreamCommand { get; set; }
        public Command DeactivateSledovaniTVDeviceCommand { get; set; }

        public Command AboutCommand { get; set; }

        public ObservableCollection<ChannelItem> AutoPlayChannels { get; set; } = new ObservableCollection<ChannelItem>();

        private ChannelItem _selectedChannelItem;

        public SettingsViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
            : base(loggingService, config, dialogService)
        {
            _service = service;
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;

            StopStreamCommand = new Command(async () => await StopStream());
            DeactivateSledovaniTVDeviceCommand = new Command(async () => await DeactivateSledovaniTVDevice());
            AboutCommand = new Command(async () => await About());
        }

        public string AppVersion { get; set; } = String.Empty;

        public string FontSizeForCaption
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public bool IsFullScreen
        {
            get
            {
                return Config.Fullscreen;
            }
            set
            {
                Config.Fullscreen = value;
                if (value)
                {
                    MessagingCenter.Send(String.Empty, BaseViewModel.MSG_EnableFullScreen);
                } else
                {
                    MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                }

                OnPropertyChanged(nameof(IsFullScreen));
            }
        }

        public string FontSizeForText
        {
            get
            {
                return GetScaledSize(11).ToString();
            }
        }

        public string FontSizeForEntry
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public string FontSizeForPicker
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }

        public string SledovaniTVDeviceID
        {
            get
            {
                return Config.DeviceId;
            }
            set
            {
                Config.DeviceId = value;
                OnPropertyChanged(nameof(SledovaniTVDeviceID));
            }
        }

        public string SledovaniTVDevicePassword
        {
            get
            {
                return Config.DevicePassword;
            }
            set
            {
                Config.DevicePassword = value;
                OnPropertyChanged(nameof(SledovaniTVDevicePassword));
            }
        }

        public void NotifyFontSizeChange()
        {
            OnPropertyChanged(nameof(FontSizeForCaption));
            OnPropertyChanged(nameof(FontSizeForEntry));
            OnPropertyChanged(nameof(FontSizeForPicker));
            OnPropertyChanged(nameof(FontSizeForText));
        }

        public void NotifySledovaniTVDeviceIChange()
        {
            OnPropertyChanged(nameof(SledovaniTVDeviceID));
            OnPropertyChanged(nameof(SledovaniTVDevicePassword));
        }

        public void FillAutoPlayChannels(ObservableCollection<ChannelItem> channels = null)
        {
            AutoPlayChannels.Clear();

            var first = new ChannelItem()
            {
                Name = "Nespouštět žádný kanál",
                ChannelNumber = "-1"
            };
            var second = new ChannelItem()
            {
                Name = "Posledně vybraný kanál",
                ChannelNumber = "0"
            };

            AutoPlayChannels.Add(first);
            AutoPlayChannels.Add(second);

            var anythingSelected = false;

            foreach (var ch in channels)
            {
                AutoPlayChannels.Add(ch);

                if (ch.ChannelNumber == Config.AutoPlayChannelNumber)
                {
                    anythingSelected = true;
                    SelectedChannelItem = ch;
                }
            }

            if (!anythingSelected)
            {
                if (Config.AutoPlayChannelNumber == "0")
                {
                    SelectedChannelItem = second;
                }
                else
                {
                    SelectedChannelItem = first;
                }
            }

            OnPropertyChanged(nameof(AutoPlayChannels));
        }

        protected async Task StopStream()
        {
            await _service.StopStream();
        }

        protected async Task DeactivateSledovaniTVDevice()
        {
            if (await _dialogService.Confirm("Opravdu si přejete deaktivovat toto zařízení? Zařízení se při příštím přihlášení aktivuje jako nové zařízení."))
            {
                SledovaniTVDeviceID = string.Empty;
                SledovaniTVDevicePassword = string.Empty;

                OnPropertyChanged(nameof(SledovaniTVPaired));
                OnPropertyChanged(nameof(ShowUnpairButton));

                ShowSledovaniPairedDevice = false;
            };
        }

        protected async Task About()
        {
            var uri = new Uri("https://onlinetelevizor.petrjanousek.net");
            await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }

        public int TVAPIIndex
        {
            get
            {
                return (int)Config.TVApi;
            }
            set
            {
                Config.TVApi = (TVAPIEnum)value;

                OnPropertyChanged(nameof(TVAPIIndex));
                OnPropertyChanged(nameof(IsSledovaniTVVisible));
                OnPropertyChanged(nameof(IsKUKITVVisible));
                OnPropertyChanged(nameof(IsDemoVisible));
                OnPropertyChanged(nameof(IsO2TVVisible));
            }
        }

        public bool IsSledovaniTVVisible
        {
            get
            {
                return Config.TVApi == TVAPIEnum.SledovaniTV;
            }
        }

        public bool IsKUKITVVisible
        {
            get
            {
                return Config.TVApi == TVAPIEnum.KUKI;
            }
        }

        public bool IsDemoVisible
        {
            get
            {
                return Config.TVApi == TVAPIEnum.Demo;
            }
        }

        public bool IsO2TVVisible
        {
            get
            {
                return Config.TVApi == TVAPIEnum.O2TV;
            }
        }

        public int AppFontSizeIndex
        {
            get
            {
                return (int)Config.AppFontSize;
            }
            set
            {
                Config.AppFontSize = (AppFontSizeEnum)value;

                OnPropertyChanged(nameof(AppFontSizeIndex));

                NotifyFontSizeChange();
            }
        }

        public bool ShowAdultChannels
        {
            get
            {
                return Config.ShowAdultChannels;
            }
            set
            {
                Config.ShowAdultChannels = value;
                OnPropertyChanged(nameof(IsPINShowed));
            }
        }

        public bool SledovaniTVPaired
        {
            get
            {
                return  !(String.IsNullOrEmpty(Config.DeviceId)) &&
                        !(String.IsNullOrEmpty(Config.DevicePassword));
            }
        }

        public bool ShowSledovaniPairedDevice
        {
            get
            {
                return _showSledovaniPairedDevice;
            }
            set
            {
                _showSledovaniPairedDevice = value;
                OnPropertyChanged(nameof(ShowSledovaniPairedDevice));
                OnPropertyChanged(nameof(ShowUnpairButton));
            }
        }

        public bool ShowUnpairButton
        {
            get
            {
                return SledovaniTVPaired && ShowSledovaniPairedDevice;
            }
        }

        public bool IsPINShowed
        {
            get
            {
                return Config.ShowAdultChannels;
            }
        }

        public ChannelItem SelectedChannelItem
        {
            get
            {
                return _selectedChannelItem;
            }
            set
            {
                _selectedChannelItem = value;

                if (value != null)
                    Config.AutoPlayChannelNumber = value.ChannelNumber;

                OnPropertyChanged(nameof(SelectedChannelItem));
            }
        }

        public bool AllowRemoteAccessService
        {
            get
            {
                return Config.AllowRemoteAccessService;
            }
            set
            {
                Config.AllowRemoteAccessService = value;

                OnPropertyChanged(nameof(Config));
            }
        }

    }
}
