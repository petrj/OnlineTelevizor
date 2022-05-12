using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using LoggerService;
using Mono.Web;
using SledovaniTVAPI;
using TVAPI;

namespace SledovaniTVDownloadEvent
{
    class MainClass
    {
        public static void Help()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("");
            Console.WriteLine("SledovaniTV.DownloadEvent.exe url [pathToDownloadedMKV] [-silent] [-help]");
            Console.WriteLine("");
        }

        public static PrgSettings ParseArgs(string[] args)
        {
            var res = new PrgSettings();
            res.Url = null;
            res.PathToMKV = null;

            if (args.Length == 0)
            {
                Console.WriteLine($"Missing command line arguments!");
                Console.WriteLine("");
                Help();

                return res;
            }

            foreach (var arg in args)
            {
                Console.WriteLine($"arg: {arg}");

                if ((arg.ToLower() == "--help") ||
                    (arg.ToLower() == "-help") ||
                    (arg.ToLower() == "/help"))
                {
                    Console.WriteLine("Setting ShowHelp");
                    res.ShowHelp = true;
                } else
                if ((arg.ToLower() == "--silent") ||
                    (arg.ToLower() == "-silent") ||
                    (arg.ToLower() == "/silent"))
                {
                    Console.WriteLine("Setting Silent");
                    res.Silent = true;
                } else if (res.Url == null)
                {
                    Console.WriteLine($"Setting Url: {arg}");
                    res.Url = arg;
                } else
                {
                    Console.WriteLine($"Setting PathToMKV: {arg}");
                    res.PathToMKV = arg;
                }
            }

            if (string.IsNullOrEmpty(res.PathToMKV))
            {
                Console.WriteLine("Setting default path to MKV: event.mkv");
                res.PathToMKV = "event.mkv";
            }

            res.Valid = true;

            return res;
        }

        public static void Main(string[] args)
        {
            var _loggerService = new BasicLoggingService();
            _loggerService.MinLevel = LoggingLevelEnum.Debug;

            var prgSettings = ParseArgs(args);
            if (!prgSettings.Valid)
            {
                return;
            }

            var tvService = new SledovaniTV(_loggerService);

            if (!JSONObject.FileExists(prgSettings.CredentialsFilePath))
            {
                Console.WriteLine($"File {prgSettings.CredentialsFilePath} not found!");
                Console.WriteLine("");
                Console.WriteLine("Example:");
                Console.WriteLine("{");
                Console.WriteLine(" \"Username\": \"username\",");
                Console.WriteLine(" \"Password\": \"password\",");
                Console.WriteLine(" \"ChildLockPIN\": \"pin\"");
                Console.WriteLine("}");
                return;
            }

          
            Console.WriteLine($"Url: {prgSettings.Url}");


            var uri = new Uri(prgSettings.Url);
            var eventIdParam = HttpUtility.ParseQueryString(uri.Query).Get("eventId");

            Console.WriteLine($"EventId: {eventIdParam}");

            var credentials = JSONObject.LoadFromFile<Credentials>(prgSettings.CredentialsFilePath);

            tvService.SetCredentials(credentials.Username, credentials.Password, credentials.ChildLockPIN);

            if (JSONObject.FileExists(prgSettings.ConnectionFilePath))
            {
                var conn = JSONObject.LoadFromFile<DeviceConnection>(prgSettings.ConnectionFilePath);
                tvService.SetConnection(conn.deviceId, conn.password);
            }

            Task.Run(
                      async () =>
                      {

                         
                          Console.WriteLine();
                          Console.WriteLine($"Logging SledovaniTV .....");

                          await tvService.Login();

                          if (!JSONObject.FileExists(prgSettings.ConnectionFilePath))
                          {
                              tvService.Connection.SaveToFile(prgSettings.ConnectionFilePath);
                          }

                          Console.WriteLine($"SledovaniTV status: {tvService.Status}");
                          Console.WriteLine($"SledovaniTV PHPSESSID: {tvService.PHPSESSID}");

                          var silentFlag = prgSettings.Silent ? "c" : "";
                          var streamUrl = $"http://sledovanitv.cz/vlc/api-timeshift/event.m3u8?PHPSESSID={tvService.PHPSESSID}&eventId={HttpUtility.UrlEncode(eventIdParam)}";

                          var vlcParams = $"\"{streamUrl}\" --sout='#duplicate{{dst=display,dst=standard{{access=file,mux=mkv,dst={prgSettings.PathToMKV}}}}}'";

                          if (prgSettings.Silent)
                          {
                              vlcParams = $"\"{streamUrl}\" --sout='#standard{{access=file,mux=mkv,dst={prgSettings.PathToMKV}}}'";
                          }

                          Console.WriteLine($"vlcParams: {vlcParams}");

                          Process.Start($"{silentFlag}vlc", vlcParams);

                      }).Wait();

        }
    }
}

