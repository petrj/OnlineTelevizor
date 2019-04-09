using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xamarin.Forms;

namespace SledovaniTVPlayer.Services
{
    public class BackgroundCommandWorker
    {
        public static void RunInBackground(Command command, int repeatIntervalSeconds = 5, int delaySeconds = 0)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Thread.Sleep(delaySeconds * 1000);

                do
                {
                    command.Execute(null);

                    Thread.Sleep(repeatIntervalSeconds * 1000);

                } while (true);
            }).Start();

        }
    }
}
