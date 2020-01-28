using LibVLCSharp.Shared;
using OnlineTelevizor.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerPage : ContentPage
    {
        LibVLC _libVLC = null;
        MediaPlayer _mediaPlayer;
        Media _media = null;
        private string _mediaUrl;
        bool _fullscreen = false;

        public PlayerPage()
        {
            InitializeComponent();

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };            

            videoView.MediaPlayer = _mediaPlayer;            
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
                return videoView.MediaPlayer.IsPlaying;
            }
        }

        public void SetMediaUrl(string mediaUrl)
        {
            _mediaUrl = mediaUrl;

            if (Playing)
            {
                videoView.MediaPlayer.Stop();
                _media = new Media(_libVLC, _mediaUrl, FromType.FromLocation);
                
                videoView.MediaPlayer.Play(_media);
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _media = new Media(_libVLC, _mediaUrl, FromType.FromLocation);            
            videoView.MediaPlayer.Play(_media);

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

        public void Stop()
        {
            videoView.MediaPlayer.Stop();
        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            // go back
            Navigation.PopModalAsync();
        }
    }
}