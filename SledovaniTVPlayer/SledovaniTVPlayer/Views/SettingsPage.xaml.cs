using LoggerService;
using NLog;
using SledovaniTVPlayer.Models;
using SledovaniTVPlayer.Services;
using SledovaniTVPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SledovaniTVPlayer.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingsPage : ContentPage
	{
        private SettingsViewModel _viewModel;
        private ISledovaniTVConfiguration _config;

        public SettingsPage (ILoggingService loggingService, ISledovaniTVConfiguration config)
		{
			InitializeComponent ();

            _config = config;

            BindingContext = _viewModel = new SettingsViewModel(loggingService, config);

            // two way binding does not work in real Android !?
            UsernameEntry.Text = _config.Username;
            PasswordEntry.Text = _config.Password;
            PinEntry.Text = _config.ChildLockPIN;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            _config.Username = UsernameEntry.Text;
            _config.Password = PasswordEntry.Text;
            _config.ChildLockPIN = PinEntry.Text;
        }
    }
}