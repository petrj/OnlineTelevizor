using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xamarin.Forms;

namespace OnlineTelevizor.Services
{
    public class BackgroundCommandWorker
    {
        /// <summary>
        /// Running command in background
        /// </summary>
        /// <param name="command"></param>
        /// <param name="repeatIntervalSeconds">0 and negative for no repeat</param>
        /// <param name="delaySeconds">start delay</param>
        public static void RunInBackground(Command command, int repeatIntervalSeconds = 5, int delaySeconds = 0)
        {
            if (Device.RuntimePlatform == Device.UWP)
            {
                Thread.Sleep(delaySeconds * 1000);

                command.Execute(null);

                if (repeatIntervalSeconds == 0)
                    return;

                Device.StartTimer(TimeSpan.FromSeconds(repeatIntervalSeconds), () =>
                {
                    command.Execute(null);

                    return true; 
                });
            }
            else
            if (Device.RuntimePlatform == Device.Android)
            {
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    Thread.Sleep(delaySeconds * 1000);

                    do
                    {
                        command.Execute(null);

                        if (repeatIntervalSeconds <= 0)
                        {
                            break;
                        }
                        else
                        {
                            Thread.Sleep(repeatIntervalSeconds * 1000);
                        }
                    } while (true);
                }).Start();
            }
        }

    }
}
