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
        LibVLC _libVLC = null;
        MediaPlayer _mediaPlayer;
        Media _media = null;
        bool _fullscreen = false;
        bool _playInProgress = false;
        ILoggingService _loggingService;

        public Command CheckStreamCommand { get; set; }

        public Command AnimeIconCommand { get; set; }

        private PlayerPageViewModel _viewModel;

        public PlayerPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
        {
            InitializeComponent();

            BindingContext = _viewModel = new PlayerPageViewModel(loggingService, config, dialogService, service);

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            _loggingService = loggingService;

            videoView.MediaPlayer = _mediaPlayer;

            CheckStreamCommand = new Command(async () => await CheckStream());
            AnimeIconCommand = new Command(async () => await Anime());

            BackgroundCommandWorker.RunInBackground(CheckStreamCommand, 3, 5);
            BackgroundCommandWorker.RunInBackground(AnimeIconCommand, 1, 1);
        }

        private async Task Anime()
        {
            await Task.Run( () => { _viewModel.Anime(); } );
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
                        (_mediaPlayer.VideoTrack == -1)
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

        public void SetMediaUrl(MediaDetail detail)
        {
            _viewModel.MediaUrl = detail.MediaUrl;
            _viewModel.Title = detail.Title;
            _viewModel.MediaType = detail.Type;
            _viewModel.ChannelId = detail.ChanneldID;
            _viewModel.EPGItem = detail.CurrentEPGItem;

            var desc = detail.CurrentEPGItem == null ? "" : detail.CurrentEPGItem.Title;
            MessagingCenter.Send($"\u25B6  {detail.Title} - {desc}", BaseViewModel.ToastMessage);

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
        }

        public void Start()
        {
            _media = new Media(_libVLC, _viewModel.MediaUrl, FromType.FromLocation);
            videoView.MediaPlayer.Play(_media);

            _playInProgress = true;
        }

        public void Stop()
        {
            videoView.MediaPlayer.Stop();

            _playInProgress = false;
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
            try
            {
                int currentVol = _mediaPlayer.Volume / 10;

                if (currentVol != 10)
                {
                    currentVol += 1;

                    if (currentVol > 10)
                    {
                        currentVol = 10;
                    }

                    _mediaPlayer.Volume = currentVol * 10;
                }

                MessagingCenter.Send($"Hlasitost {_mediaPlayer.Volume}%", BaseViewModel.ToastMessage);

            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void SwipeGestureRecognizer_Down(object sender, SwipedEventArgs e)
        {
            try
            {
                int currentVol = _mediaPlayer.Volume / 10;

                if (currentVol != 0)
                {
                    currentVol -= 1;

                    if (currentVol < 0)
                    {
                        currentVol = 0;
                    }

                    _mediaPlayer.Volume = currentVol * 10;
                }

                MessagingCenter.Send($"Hlasitost {_mediaPlayer.Volume}%", BaseViewModel.ToastMessage);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }
    }
}
