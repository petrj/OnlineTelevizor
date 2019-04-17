using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Xamarin.Forms
{
    public class BasePage : ContentPage
    {
        public event EventHandler<KeyEventArgs> KeyPressed;

        public void SendKeyPressed(object sender, KeyEventArgs e)
        {
            KeyPressed?.Invoke(sender, e);
        }

        public BasePage()
        {
            KeyPressed += CustomPage_KeyPressed;
        }

        private void CustomPage_KeyPressed(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Key pressed: " + e.Key);
        }
    }

    public class KeyEventArgs : EventArgs
    {
        public string Key { get; set; }
    }
}
