using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public static CancellationTokenSource RunInBackground(Command command, int repeatIntervalSeconds = 5, int delaySeconds = 0)
        {
            return RunInBackground(command, null, repeatIntervalSeconds, delaySeconds);
        }

        /// <summary>
        /// Running command in background
        /// </summary>
        /// <param name="command"></param>
        /// <param name="repeatIntervalSeconds">0 and negative for no repeat</param>
        /// <param name="delaySeconds">start delay</param>
        public static CancellationTokenSource RunInBackground(Command command, ILoggingService loggingService, int repeatIntervalSeconds = 5, int delaySeconds = 0)
        {
            var cancelToken = new CancellationTokenSource();

            if (loggingService != null)
                loggingService.Info("Starting new background command");

            Task.Factory.StartNew(async () => {

                Thread.Sleep(delaySeconds * 1000);

                do
                {
                    if (loggingService != null)
                        loggingService.Info("Executing command");

                    //Xamarin.Forms.Device.BeginInvokeOnMainThread(new Action(delegate { command.Execute(null); }));

                    command.Execute(null);

                    if (repeatIntervalSeconds <= 0)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(repeatIntervalSeconds * 1000);
                    }
                } while (!cancelToken.IsCancellationRequested);

            }, cancelToken.Token);

            return cancelToken;
        }
    }
}
