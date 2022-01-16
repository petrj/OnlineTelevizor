using LoggerService;
using OnlineTelevizor.Models;
using OnlineTelevizor.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        public HelpViewModel(ILoggingService loggingService, IOnlineTelevizorConfiguration config, IDialogService dialogService, TVService service)
          : base(loggingService, config, dialogService)
        {
        }

        public string FontSizeForGroupCaption
        {
            get
            {
                return GetScaledSize(16).ToString();
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

        public string FontSizeForNote
        {
            get
            {
                return GetScaledSize(10).ToString();
            }
        }
    }
}
