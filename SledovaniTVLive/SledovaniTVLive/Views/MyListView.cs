using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Xamarin.Forms
{
    public class MyListView : ListView
    {
        public event EventHandler<KeyEventArgs> KeyPressed;

        public void SendKeyPressed(object sender, KeyEventArgs e)
        {
            KeyPressed?.Invoke(sender, e);
        }

        public MyListView()
        {
            KeyPressed += MyListView_KeyPressed;
        }

        private void MyListView_KeyPressed(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Key pressed: " + e.Key);
        }
    }
}

