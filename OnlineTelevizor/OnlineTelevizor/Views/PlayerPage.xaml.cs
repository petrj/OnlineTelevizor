using Android.OS;
using LibVLCSharp.Shared;
using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TVAPI;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static Android.OS.PowerManager;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerPage : ContentPage
    {
        private LibVLC _libVLC = null;
        private MediaPlayer _mediaPlayer;
        private Media _media = null;
        private IOnlineTelevizorConfiguration _config;
        private bool _fullscreen = false;
        private bool _playInProgress = false;
        private ILoggingService _loggingService;
        private DateTime _lastSingleClicked = DateTime.MinValue;

        public Command CheckStreamCommand { get; set; }

        public Command UpdateNotificationCommand { get; set; }

        public Command AnimeIconCommand { get; set; }

        private PlayerPageViewModel _viewModel;

        public PlayerPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
        {
            InitializeComponent();

            BindingContext = _viewModel = new PlayerPageViewModel(loggingService, config, dialogService, service);

            Core.Initialize();

            _config = config;
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            _loggingService = loggingService;

            videoView.MediaPlayer = _mediaPlayer;

            CheckStreamCommand = new Command(async () => await CheckStream());
            UpdateNotificationCommand = new Command(async () => await UpdateNotification());
            AnimeIconCommand = new Command(async () => await Anime());

            BackgroundCommandWorker.RunInBackground(CheckStreamCommand, 3, 5);
            BackgroundCommandWorker.RunInBackground(UpdateNotificationCommand, 10, 5);
            BackgroundCommandWorker.RunInBackground(AnimeIconCommand, 1, 1);
        }

        private async Task Anime()
        {
            await Task.Run( () => { _viewModel.Anime(); } );
        }

        private async Task UpdateNotification()
        {
      /*      if (!Playing || !_config.PlayOnBackground)
            {
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                if (_viewModel.EPGItem != null)
                {

                    _viewModel.Description = _viewModel.EPGItem.Title;
                    _viewModel.DetailedDescription = _viewModel.EPGItem.Description;
                    _viewModel.TimeDescription = _viewModel.EPGItem.Start.ToString("HH:mm") + " - " + _viewModel.EPGItem.Finish.ToString("HH:mm");
                    _viewModel.EPGProgress = _viewModel.EPGItem.Progress;

                    MessagingCenter.Send<PlayerPage, MediaDetail>(this, BaseViewModel.UpdateInternalNotification, new MediaDetail()
                    {
                        MediaUrl = _viewModel.MediaUrl,
                        Title = _viewModel.Title,
                        Type = _viewModel.MediaType,
                        CurrentEPGItem = _viewModel.EPGItem,
                        ChanneldID = _viewModel.ChannelId,
                        LogoUrl = _viewModel.LogoIcon
                    });
                }
            });*/
        }

        private async Task CheckStream()
        {
            if (!Playing)
            {
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                if (!videoView.MediaPlayer.IsPlaying)
                {
                    videoView.MediaPlayer.Play(_media);
                }

                if (
                        (_mediaPlayer.VideoTrackCount == 0)
                        ||
                        (!string.IsNullOrEmpty(_viewModel.MediaType) && (_viewModel.MediaType.ToLower() == "radio"))
                    )
                {
                    _viewModel.AudioViewVisible = true;
                } else
                {
                    _viewModel.AudioViewVisible = false;
                }
            });

            await UpdateEPG();
        }

        public void OnSingleTapped(object sender, EventArgs e)
        {
            ShowJustPlayingNotification();
        }

        public void OnDoubleTapped(object sender, EventArgs e)
        {
            if (!_fullscreen)
            {
                MessagingCenter.Send(String.Empty, BaseViewModel.EnableFullScreen);
                _fullscreen = true;
            } else
            {
                MessagingCenter.Send(String.Empty, BaseViewModel.DisableFullScreen);
                _fullscreen = false;
            }
        }

        public bool Playing
        {
            get
            {
                return _playInProgress;

                //videoView.MediaPlayer.IsPlaying can be false in case of internet disconnection
            }
            set
            {
                _playInProgress = value;
            }
        }

        private async Task UpdateEPG()
        {
            await Task.Run(() => {

                Device.BeginInvokeOnMainThread(() =>
                {
                    if (_viewModel.EPGItem == null)
                    {
                        _viewModel.Description = String.Empty;
                        _viewModel.DetailedDescription = String.Empty;
                        _viewModel.TimeDescription = String.Empty;
                        _viewModel.EPGProgress = 0;
                    }
                    else
                    {
                        _viewModel.Description = _viewModel.EPGItem.Title;
                        _viewModel.DetailedDescription = _viewModel.EPGItem.Description;
                        _viewModel.TimeDescription = _viewModel.EPGItem.Start.ToString("HH:mm") + " - " + _viewModel.EPGItem.Finish.ToString("HH:mm");
                        _viewModel.EPGProgress = _viewModel.EPGItem.Progress;
                    }
                });
            });
        }

  /*      public MediaDetail GetMediaUrl()
        {
            return new MediaDetail()
            {
                MediaUrl = _viewModel.MediaUrl,
                Title = _viewModel.Title,
                Type = _viewModel.MediaType,
                ChanneldID = _viewModel.ChannelId,
                CurrentEPGItem = _viewModel.EPGItem,
                NextEPGItem = _viewModel.NextEPGItem,
                LogoUrl = _viewModel.LogoIcon,
                Position = MediaPosition
            };
        }

        public void SetMediaUrl(MediaDetail detail)
        {
            _viewModel.MediaUrl = detail.MediaUrl;
            _viewModel.Title = detail.Title;
            _viewModel.MediaType = detail.Type;
            _viewModel.ChannelId = detail.ChanneldID;
            _viewModel.EPGItem = detail.CurrentEPGItem;
            _viewModel.NextEPGItem = detail.NextEPGItem;
            _viewModel.LogoIcon = detail.LogoUrl;

            _lastSingleClicked = DateTime.MinValue;

            ShowJustPlayingNotification();

            if (Playing)
            {
                Stop();

                Start();

                Task.Run( async () =>
                {
                    await UpdateEPG();
                });
            }
        }
  */
        public void ShowJustPlayingNotification()
        {
            bool showCurrent;
            string msg;

            if (( (DateTime.Now-_lastSingleClicked).TotalSeconds>5) ||
                _viewModel.NextEPGItem == null)
            {
                showCurrent = true;
            } else
            {
                showCurrent = false;
            }

            if (showCurrent)
            {
                msg = $"\u25B6  {_viewModel.Title}";

                if (_viewModel.EPGItem != null &&
                _viewModel.EPGItem.Start < DateTime.Now &&
                _viewModel.EPGItem.Finish > DateTime.Now &&
                !string.IsNullOrEmpty(_viewModel.EPGItem.Title))
                {
                    msg += $" - {_viewModel.EPGItem.Title}";
                }

                _lastSingleClicked = DateTime.Now;
            } else
            {
                if (_viewModel.NextEPGItem != null &&
                    !string.IsNullOrEmpty(_viewModel.NextEPGItem.Title))
                {
                    msg = $"-> {_viewModel.NextEPGItem.Start.ToString("HH:mm")} - {_viewModel.NextEPGItem.Title}";
                } else
                {
                    msg = $"\u25B6  {_viewModel.Title}";
                }

                _lastSingleClicked = DateTime.MinValue;
            }

            MessagingCenter.Send(msg, BaseViewModel.ToastMessage);
        }

        public string PlayingChannelName
        {
            get
            {
                return _viewModel.Title;
            }
        }

        public string PlayingTitleName
        {
            get
            {
                return _viewModel.EPGItem == null ? String.Empty : _viewModel.EPGItem.Title;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            Start();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Thread.Sleep(500);

                CheckStreamCommand.Execute(null);
            }).Start();

            if (!_fullscreen)
            {
                OnDoubleTapped(this, null);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            Stop();

            if (_fullscreen)
            {
                OnDoubleTapped(this, null);
            }

            if (_config.PlayOnBackground)
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.StopPlayInternalNotification);
            }
        }

        public void Start()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                _media = new Media(_libVLC, _viewModel.MediaUrl, FromType.FromLocation);
                videoView.MediaPlayer.Play(_media);
                Playing = true;
            });
        }

        public void Stop()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                videoView.MediaPlayer.Stop();
                Playing = false;
            });
        }

        public float MediaPosition
        {
            get
            {
                if (_mediaPlayer.VideoTrack != -1)
                {
                    return videoView.MediaPlayer.Position;
                } else
                {
                    return -1;
                }
            }
        }

        public void Resume()
        {
            if (Playing)
            {
                // workaround for black screen after resume (only audio is playing)
                // TODO: resume video without reinitializing

                Device.BeginInvokeOnMainThread(() =>
                {
                    if (_mediaPlayer.VideoTrack != -1)
                    {
                        var pos = videoView.MediaPlayer.Position;
                        videoView.MediaPlayer.Stop();

                        VideoStackLayout.Children.Remove(videoView);
                        VideoStackLayout.Children.Add(videoView);

                        videoView.MediaPlayer.Play();
                        videoView.MediaPlayer.Position = pos;
                    }
                });
            }
        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            // go back
            Navigation.PopModalAsync();
        }

        private void SwipeGestureRecognizer_Up(object sender, SwipedEventArgs e)
        {
            MessagingCenter.Send(String.Empty, BaseViewModel.PlayPrevious);
        }

        private void SwipeGestureRecognizer_Down(object sender, SwipedEventArgs e)
        {
            MessagingCenter.Send(String.Empty, BaseViewModel.PlayNext);
        }
    }
}
