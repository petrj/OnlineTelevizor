using Android.App;
using Android.Content;
using LoggerService;
using SledovaniTVAPI;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SledovaniTVPlayer.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private TVService _service;
        private IDialogService _dialogService;
        private ILoggingService _loggingService;
        private Context _context;

        public ObservableCollection<TVChannel> Channels { get; set; }

        public Command RefreshCommand { get; set; }

        public MainPageViewModel(ILoggingService loggingService, ISledovaniTVConfiguration config, IDialogService dialogService, Context context)
           : base(loggingService, config)
        {
            _service = new TVService(loggingService, config);
            _loggingService = loggingService;
            _dialogService = dialogService;
            _context = context;

            Channels = new ObservableCollection<TVChannel>();

            RefreshCommand = new Command(async () => await ReloadChannels());

            BackgroundCommandWorker.RunInBackground(RefreshCommand, 30, 0);
        }

        private async Task ReloadChannels()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var channels = await _service.GetChannels();

                Channels.Clear();

                foreach (var ch in channels)
                    Channels.Add(ch);

            } finally
            {
                IsBusy = false;
            }
        }

        public async Task PlayStream(string url, int resultKeyCode = 0)
        {
            try
            {
                var intent = new Intent(Intent.ActionView);
                var uri = Android.Net.Uri.Parse(url);
                intent.SetDataAndType(uri, "video/*");
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask); // necessary for Android 5
                _context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "PlayStream general error");

               await _dialogService.Information(ex.ToString());
            }
        }
    }
}
