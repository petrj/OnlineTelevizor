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
            var loggingService = new TCPIPLoggingService("http://88.103.80.48:8100");    

            Console.WriteLine("...");

            var sledovaniTV = new SledovaniTV(loggingService );
            sledovaniTV.SetCredentials(credentials.Username, credentials.Password, credentials.ChildLockPIN);

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

                    var qualities = await sledovaniTV.GetStreamQualities();
                    foreach (var q in qualities)
                    {
                        Console.WriteLine(q.Name.PadRight(20) + "  " + q.Id.PadLeft(10) + "  " + q.Allowed);
                    }

                    await sledovaniTV.Unlock();
                    //await sledovaniTV.Lock();

                    var channels = await sledovaniTV.GetChanels();
                    var epg = await sledovaniTV.GetEPG();

                    foreach (var ch in channels)
                    {
                        Console.WriteLine(ch.Name);
                        Console.WriteLine("  " + ch.Locked);
                        Console.WriteLine("  " + ch.Url);
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine($"Status: {sledovaniTV.Status.ToString()}");
                    Console.WriteLine();
                    Console.WriteLine("Press any key");                   
                });


            Console.ReadKey();
        }
    }
}