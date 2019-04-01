using LoggerService;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;


namespace SledovaniTVPlayer.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        protected ILoggingService _logingService;

        public ISledovaniTVConfiguration Config { get; set; }

        bool isBusy = false;

        // navigation.PushModalAsync enabled only 1 time per 3 seconds
        private DateTime _lastNavigateTime = DateTime.MinValue;

        public BaseViewModel(ILoggingService loggingService, ISledovaniTVConfiguration config)
        {
            _logingService = loggingService;
            Config = config;
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                SetProperty(ref isBusy, value);
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName]string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        public async Task NavigateToPage(Page page, INavigation navigation)
        {
            if ((DateTime.Now - _lastNavigateTime).TotalSeconds > 3)
            {
                _lastNavigateTime = DateTime.Now;

                var navPage = new NavigationPage(page);
                await navigation.PushModalAsync(navPage);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
