using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace OnlineTelevizor.Services
{
    public static class BackgroundCommandWorker
    {
        private static Dictionary<string, CancellationTokenSource> _commandNameToCancellationTokens = new Dictionary<string, CancellationTokenSource>();

        //private static List<CancellationTokenSource> _cancellationTokens = new List<CancellationTokenSource>();
        //private static List<string> _registeredCommands = new List<string>();

        /// <summary>
        /// Running command in background
        /// </summary>
        /// <param name="command"></param>
        /// <param name="repeatIntervalSeconds">0 and negative for no repeat</param>
        /// <param name="delaySeconds">start delay</param>
        public static void RegisterCommand(Command command, string name, int repeatIntervalSeconds = 5, int delaySeconds = 0)
        {
            RegisterCommand(command, name, null, repeatIntervalSeconds, delaySeconds);
        }

        /// <summary>
        /// Running command in background
        /// </summary>
        /// <param name="command"></param>
        /// <param name="repeatIntervalSeconds">0 and negative for no repeat</param>
        /// <param name="delaySeconds">start delay</param>
        public static void RegisterCommand(Command command, string name, ILoggingService loggingService, int repeatIntervalSeconds = 5, int delaySeconds = 0)
        {
            if (_commandNameToCancellationTokens.ContainsKey(name))
            {
                if (loggingService != null)
                    loggingService.Info("Command is already registered");

                _commandNameToCancellationTokens[name].Cancel();
                _commandNameToCancellationTokens.Remove(name);
            }

            var cancelToken = new CancellationTokenSource();

            if (loggingService != null)
                loggingService.Info("Registering new background command");

            _commandNameToCancellationTokens.Add(name, cancelToken);

            Task.Factory.StartNew(async () => {

                Thread.Sleep(delaySeconds * 1000);

                do
                {
                    if (loggingService != null)
                        loggingService.Info("Executing command");

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
        }

        /// <summary>
        /// Running command in background
        /// </summary>
        /// <param name="command"></param>
        /// <param name="delaySeconds">start delay</param>
        public static void RunCommandWithDelay(Command command, int delaySeconds, ILoggingService loggingService = null)
        {
            if (loggingService != null)
                loggingService.Info($"Running command in background with delay {delaySeconds} seconds");

            Task.Factory.StartNew(async () =>
            {
                Thread.Sleep(delaySeconds * 1000);

                command.Execute(null);
            });
        }

        public static void UnregisterCommands(ILoggingService loggingService = null)
        {
            if (loggingService != null)
                loggingService.Info($"StopBackgroundThreads");

            foreach (var nameAndtoken in _commandNameToCancellationTokens)
            {
                nameAndtoken.Value.Cancel();
            }

            _commandNameToCancellationTokens.Clear();
        }
    }
}
