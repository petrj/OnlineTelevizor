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
    public partial class CastRenderersPage : ContentPage, IOnKeyDown
    {
        private RenderersViewModel _viewModel;
        private MediaPlayer _mediaPlayer;
        private ChannelItem _channel;
        private Command CheckCastingCommand { get; set; }
        private bool _castingStarted = false;
        protected ILoggingService _loggingService;
        protected DialogService _dialogService;

        public CastRenderersPage(ILoggingService loggingService, IOnlineTelevizorConfiguration config)
        {
            InitializeComponent();
            _loggingService = loggingService;

            _dialogService = new DialogService(this);

            BindingContext = _viewModel = new RenderersViewModel(loggingService, config, _dialogService);

            CheckCastingCommand = new Command(async () => await CheckCasting());

            BackgroundCommandWorker.RegisterCommand(CheckCastingCommand, "CheckCastingCommand", 10, 5);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.SelectedItem = null;
        }


        private async Task CheckCasting()
        {
            if (_castingStarted && !IsCasting())
            {
                MessagingCenter.Send(_channel.ChannelNumber, BaseViewModel.MSG_CastingStopped);
                MessagingCenter.Send($"Odesilání ukončeno", BaseViewModel.MSG_ToastMessage);
                _castingStarted = false;
            }
        }

        private async Task Render(RendererItem item)
        {
            if (!(await _dialogService.Confirm($"Odeslat {_channel.Name} do zařízení \"{item.Name}?\"")))
            {
                return;
            }

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

            MessagingCenter.Send<BaseViewModel,ChannelItem>(_viewModel, BaseViewModel.MSG_CastingStarted, _channel);
            MessagingCenter.Send($"Bylo zahájeno odesilání {_channel.Name} do zařízení \"{item.Name}\"", BaseViewModel.MSG_ToastMessage);

            await Navigation.PopAsync();
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"CastRenderersPage Page OnKeyDown {key}{(longPress ? " (long)" : "")}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Down:
                    await _viewModel.SelectNextItem();
                    break;

                case KeyboardNavigationActionEnum.Up:
                case KeyboardNavigationActionEnum.Left:
                    await _viewModel.SelectPreviousItem();
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:
                    if (_viewModel.SelectNextItem() != null)
                    {
                        await Render(_viewModel.SelectedItem);
                    }
                    break;
            }
        }

        public void OnTextSent(string text)
        {

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

            MessagingCenter.Send(_channel.ChannelNumber, BaseViewModel.MSG_CastingStopped);
            MessagingCenter.Send($"Odesilání bylo ukončeno", BaseViewModel.MSG_ToastMessage);
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