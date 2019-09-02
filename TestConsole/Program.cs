using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoggerService;
using SledovaniTVAPI;
using TVAPI;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var credentials = JSONObject.LoadFromFile<Credentials>("credentials.json");
            var loggingService = new BasicLoggingService();

            Console.WriteLine("...");

            var tvService = new SledovaniTV(loggingService );
            tvService.SetCredentials(credentials.Username, credentials.Password, credentials.ChildLockPIN);

            if (JSONObject.FileExists("connection.json"))
            {
                var conn = JSONObject.LoadFromFile<DeviceConnection>("connection.json");
                tvService.SetConnection(conn.deviceId, conn.password);
            }

            Task.Run(
                async () =>
                {
                    await tvService.Login();

                    if (!JSONObject.FileExists("connection.json"))
                    {
                        tvService.Connection.SaveToFile("connection.json");
                    };

                    var qualities = await tvService.GetStreamQualities();
                    foreach (var q in qualities)
                    {
                        Console.WriteLine(q.Name.PadRight(20) + "  " + q.Id.PadLeft(10) + "  " + q.Allowed);
                    }

                    //await tvService.Unlock();
                    //await sledovaniTV.Lock();

                    var channels = await tvService.GetChanels();
                    //var epg = await tvService.GetEPG();

                    foreach (var ch in channels)
                    {
                        Console.WriteLine(ch.Name);
                        Console.WriteLine("  " + ch.Locked);
                        Console.WriteLine("  " + ch.Url);
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine($"Status: {tvService.Status.ToString()}");
                    Console.WriteLine();
                    Console.WriteLine("Press any key");                   
                });


            Console.ReadKey();
        }
    }
}