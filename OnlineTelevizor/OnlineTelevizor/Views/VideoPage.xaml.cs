using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace OnlineTelevizor.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VideoPage : ContentPage
    {
        public VideoPage()
        {
            InitializeComponent();
        }

        public MediaPlayer Player
        {
            get
            {
                return videoView.MediaPlayer;
            }
            set
            {
                videoView.MediaPlayer = value;
            }

        }
    }
}