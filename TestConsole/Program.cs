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
            var loggingService = new NLogLoggingService();

            var sledovaniTV = new SledovaniTV(credentials, loggingService );

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

                    await sledovaniTV.ReloadChanels();

                    foreach (var ch in sledovaniTV.Channels.channels)
                    {
                        Console.WriteLine($"{ch.name}, {ch.url}");
                    }
                });

            Console.ReadLine();
        }
    }
}
