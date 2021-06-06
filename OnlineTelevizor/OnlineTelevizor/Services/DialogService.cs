using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace OnlineTelevizor.Services
{
    public class DialogService : IDialogService
    {
        public DialogService(Page page = null)
        {
            DialogPage = page;
        }

        public Page DialogPage { get; set; }

        public async Task<bool> Confirm(string message, string title = "Potvrzení")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            var result = await dp.DisplayAlert(title, message, "Ano", "Ne");

            return result;
        }

        public async Task ConfirmSingleButton(string message, string title = "Potvrzení", string btnOK = "OK")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            await dp.DisplayAlert(title, message, btnOK);
        }

        public async Task Information(string message, string title = "Informace")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            await dp.DisplayAlert(title, message, "OK");
        }

        public async Task<string> Select(List<string> options, string title = "Výběr", string cancel = "Zpět")
        {
            var dp = DialogPage == null ? Application.Current.MainPage : DialogPage;
            return await dp.DisplayActionSheet(title, cancel, null, options.ToArray());
        }        
    }
}
