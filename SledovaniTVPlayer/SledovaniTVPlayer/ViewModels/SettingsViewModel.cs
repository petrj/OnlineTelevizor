using LoggerService;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVPlayer.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        public SettingsViewModel(ILoggingService loggingService, ISledovaniTVConfiguration config)
            : base(loggingService, config)
        {

        }
    }
}
