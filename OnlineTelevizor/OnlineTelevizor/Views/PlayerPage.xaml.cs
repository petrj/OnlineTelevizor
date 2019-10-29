using LibVLCSharp.Shared;
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

        public PlayerPage()
        {
            InitializeComponent();

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            videoView.MediaPlayer = _mediaPlayer;
        }

        public void SetMediaUrl(string mediaUrl)
        {
            _mediaUrl = mediaUrl;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _media = new Media(_libVLC, _mediaUrl, FromType.FromLocation);

            videoView.MediaPlayer.Play(_media);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            videoView.MediaPlayer.Stop();
        }
    }
}