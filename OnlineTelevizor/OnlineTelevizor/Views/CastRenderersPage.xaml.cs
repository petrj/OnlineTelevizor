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

            BackgroundCommandWorker.RegisterCommand(CheckCastingCommand, "CheckCastingCommand", 10, 5);
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();

            // workaround for OnAppearing nothing selected
            //if (_viewModel.SelectedItem != null)
            //{
            //    var x = _viewModel.SelectedItem;
            //    _viewModel.SelectedItem = null;
            //    _viewModel.SelectedItem = x;
            //}
        }

        private async Task CheckCasting()
        {
            if (_castingStarted && !IsCasting())
            {
                MessagingCenter.Send(_channel.ChannelNumber, BaseViewModel.CastingStopped);
                _castingStarted = false;
            }
        }

        private async Task Render(RendererItem item)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                // create new media
                using (var media = new Media(_viewModel.LibVLC, new Uri(_channel.Url)))
                {
                    // create the mediaplayer
                    _mediaPlayer = new MediaPlayer(_viewModel.LibVLC);

                    _mediaPlayer.SetRenderer(item);

                    _mediaPlayer.Play(media);
                }

                _castingStarted = true;
            });

            MessagingCenter.Send<BaseViewModel,ChannelItem>(_viewModel, BaseViewModel.CastingStarted, _channel);

            await Navigation.PopAsync();
        }

        private async void Renderer_Tapped(object sender, ItemTappedEventArgs e)
        {
            await Render(e.Item as RendererItem);
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

        public async void SelectNextItem()
        {
            await _viewModel.SelectNextItem();
        }

        public async void SelectPreviousItem()
        {
            await _viewModel.SelectPreviousItem();
        }

        public async void SendOKButton()
        {
            if (_viewModel.SelectedItem == null)
                return;

            await Render(_viewModel.SelectedItem);
        }
    }
}