using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;


namespace SledovaniTVPlayer.Services
{
    public interface IDialogService
    {
        Page DialogPage { get; set; }

        Task<bool> Confirm(string message, string title = "Confirm");
        Task Information(string message, string title = "Information");
    }
}
