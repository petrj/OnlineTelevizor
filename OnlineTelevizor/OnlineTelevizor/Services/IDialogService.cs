using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;


namespace OnlineTelevizor.Services
{
    public interface IDialogService
    {
        Page DialogPage { get; set; }

        Task<bool> Confirm(string message, string title = "Potvrzení");
        Task Information(string message, string title = "Informace");
        Task ConfirmSingleButton(string message, string title = "Potvrzení", string btnOK = "OK");
        Task<string> Select(List<string> options, string title = "Výběr", string cancel = "Zpět");
    }
}
