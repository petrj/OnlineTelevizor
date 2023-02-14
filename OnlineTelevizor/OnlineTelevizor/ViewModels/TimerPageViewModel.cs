using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace OnlineTelevizor.ViewModels
{
    public class TimerPageViewModel : BaseViewModel
    {
        private decimal _timerMinutes = 0;

        public Command MinusCommand { get; set; }
        public Command PlusCommand { get; set; }

        public TimerPageViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService)
        : base(loggingService, config, dialogService)
        {
            MinusCommand = new Command(async () => await AddMinutes(-5));
            PlusCommand = new Command(async () => await AddMinutes(+5));
        }

        private async Task AddMinutes(int m)
        {
            TimerMinutes += m;
        }

        public decimal TimerMinutes
        {
            get
            {
                return _timerMinutes;
            }
            set
            {
                if (value < 0)
                    value = 0;

                if (value > 240)
                    value = 240;

                _timerMinutes = value;

                OnPropertyChanged(nameof(TimerMinutes));
                OnPropertyChanged(nameof(TimerMinutesForLabel));
            }
        }

        public string TimerMinutesForLabel
        {
            get
            {
                if (_timerMinutes == 0)
                    return "Časovač deaktivován";

                return $"Vypnout za {_timerMinutes.ToString("#0")} minut";
            }
        }

        public string FontSizeForCaption
        {
            get
            {
                return GetScaledSize(14).ToString();
            }
        }

        public string FontSizeForText
        {
            get
            {
                return GetScaledSize(12).ToString();
            }
        }
    }
}
