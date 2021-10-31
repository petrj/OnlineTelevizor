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
        private ChannelItem _channel;
        private Command CheckCastingCommand { get; set; }
        private bool _castingStarted = false;
            

        public CastRenderersPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config)
        {
            InitializeComponent();

            var dialogService = new DialogService(this);

            BindingContext = _viewModel = new RenderersViewModel(loggingService, config, dialogService);

            CheckCastingCommand = new Command(async () => await CheckCasting());

            BackgroundCommandWorker.RunInBackground(CheckCastingCommand, 10, 5);
        }

        private async Task CheckCasting()
        {
            if (_castingStarted && !IsCasting())
            {
                MessagingCenter.Send(_channel.ChannelNumber, BaseViewModel.CastingStopped);
                _castingStarted = false;
            }
        }

        private async void Renderer_Tapped(object sender, ItemTappedEventArgs e)
        {
            // create new media
            using (var media = new Media(_viewModel.LibVLC, new Uri(_channel.Url)))
            {
                // create the mediaplayer
                _mediaPlayer = new MediaPlayer(_viewModel.LibVLC);

                _mediaPlayer.SetRenderer(e.Item as RendererItem);                

                _mediaPlayer.Play(media);
            }

            MessagingCenter.Send<BaseViewModel,ChannelItem>(_viewModel, BaseViewModel.CastingStarted, _channel);

            _castingStarted = true;

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

            _castingStarted = false;

            MessagingCenter.Send(_channel.ChannelNumber, BaseViewModel.CastingStopped);
        }

        public ChannelItem Channel
        {
            get
            {
                return _channel;
            }
            set
            {
                _channel = value;
            }
        }
    }
}