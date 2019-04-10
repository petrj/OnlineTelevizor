using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoggerService;
using SledovaniTVAPI;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var credentials = JSONObject.LoadFromFile<Credentials>("credentials.json");
            var loggingService = new BasicLoggingService();
            loggingService.LogFilename = "TestConsole.log";
            loggingService.MinLevel = LoggingLevelEnum.Debug;

            var sledovaniTV = new SledovaniTV(loggingService );
            sledovaniTV.SetCredentials(credentials.Username, credentials.Password);

            if (JSONObject.FileExists("connection.json"))
            {
                sledovaniTV.Connection = JSONObject.LoadFromFile<DeviceConnection>("connection.json");
            }

            Task.Run(
                async () =>
                {
                    await sledovaniTV.Login();

                    if (!JSONObject.FileExists("connection.json"))
                    {
                        sledovaniTV.Connection.SaveToFile("connection.json");
                    };

                    var channels = await sledovaniTV.GetChanels();
                    var epg = await sledovaniTV.GetEPG();

                    foreach (var ch in channels)
                    {
                        Console.WriteLine(ch.Name);
                    }
                });

            Console.WriteLine("Press any key");
            Console.ReadKey();
        }
    }
}
