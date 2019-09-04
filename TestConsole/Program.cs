using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KUKITVAPI;
using LoggerService;
using SledovaniTVAPI;
using TVAPI;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var loggingService = new BasicLoggingService();

            Console.WriteLine("...");

         
            var tvService = new KUKITV(loggingService);

            if (JSONObject.FileExists("kuki.json"))
            {
                var conn = JSONObject.LoadFromFile<DeviceConnection>("kuki.json");
                tvService.SetConnection(conn.deviceId, null);
            }
          

           /*
            var tvService = new SledovaniTV(loggingService);

            if (JSONObject.FileExists("credentials.json"))
            {
                var credentials = JSONObject.LoadFromFile<Credentials>("credentials.json");
                tvService.SetCredentials(credentials.Username, credentials.Password, credentials.ChildLockPIN);
            }

            if (JSONObject.FileExists("connection.json"))
            {
                var conn = JSONObject.LoadFromFile<DeviceConnection>("connection.json");
                tvService.SetConnection(conn.deviceId, conn.password);
            }
           
            */

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
                        Console.WriteLine("  ID     :" + ch.Id);
                        Console.WriteLine("  Number :" + ch.ChannelNumber);
                        Console.WriteLine("  Locked :" + ch.Locked);
                        Console.WriteLine("  Url    :" + ch.Url);
                        Console.WriteLine("  Type   :" + ch.Type);
                        Console.WriteLine("  Group  :" + ch.Group);
                        Console.WriteLine("  LogoUrl:" + ch.LogoUrl);
                        Console.WriteLine("-----------------------");
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