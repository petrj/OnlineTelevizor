using LibVLCSharp.Shared;
using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using OnlineTelevizor.ViewModels;
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
    public partial class CastRenderersPage : ContentPage
    {
        private RenderersViewModel _viewModel;
        private MediaPlayer _mediaPlayer;

        private string  _url;

        public CastRenderersPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config)
        {
            InitializeComponent();

            var dialogService = new DialogService(this);

            BindingContext = _viewModel = new RenderersViewModel(loggingService, config, dialogService);
        }

        private async void Renderer_Tapped(object sender, ItemTappedEventArgs e)
        {
            // create new media
            using (var media = new Media(_viewModel.LibVLC, new Uri(Url)))
            {
                // create the mediaplayer
                _mediaPlayer = new MediaPlayer(_viewModel.LibVLC);

                _mediaPlayer.SetRenderer(e.Item as RendererItem);

                _mediaPlayer.Play(media);
            }

            await Navigation.PopAsync();
        }

        public bool IsCasting()
        {
            return _mediaPlayer != null && _mediaPlayer.IsPlaying;
        }

        public void StopCasting()
        {
            if (_mediaPlayer != null)
                _mediaPlayer.Stop();
        }

        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
            }
        }
    }
}