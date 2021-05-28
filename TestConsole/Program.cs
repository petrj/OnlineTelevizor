using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DVBStreamerAPI;
using KUKITVAPI;
using LoggerService;
using Newtonsoft.Json.Linq;
using O2TVAPI;
using SledovaniTVAPI;
using TVAPI;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var loggingService = new FileLoggingService();
            loggingService.LogFilename = "TestConsole.log";
            loggingService.WriteToOutput = true;

            Console.WriteLine("...");


            var tvService = new O2TV(loggingService);

            if (JSONObject.FileExists("O2TV.credentials.json"))
            {
                /*
                credentials.json:
                 {
                    "Username": "O2TV email",
                    "password": "some password"
                 }
                */

                var credentials = JSONObject.LoadFromFile<Credentials>("O2TV.credentials.json");
                tvService.SetConnection(credentials.Username, credentials.Password);
            }


            /*
            var tvService = new KUKITV(loggingService);

            if (JSONObject.FileExists("kuki.json"))
            {
                var conn = JSONObject.LoadFromFile<DeviceConnection>("kuki.json");
                tvService.SetConnection(conn.deviceId, null);
            }
            */

                /*
                var tvService = new SledovaniTV(loggingService);

                if (JSONObject.FileExists("credentials.json"))
                {
                    var credentials = JSONObject.LoadFromFile<Credentials>("credentials.json");
                    tvService.SetCredentials(credentials.Username, credentials.Password, credentials.ChildLockPIN);
                }

                */

                /*
                if (JSONObject.FileExists("connection.json"))
                {
                    var conn = JSONObject.LoadFromFile<DeviceConnection>("connection.json");
                    tvService.SetConnection(conn.deviceId, conn.password);
                }
                */


                /*
                  if (JSONObject.FileExists("dvbStreamer.json"))
                  {
                      var conn = JSONObject.LoadFromFile<DeviceConnection>("dvbStreamer.json");
                      tvService.SetConnection(conn.deviceId, null);
                  }
                */


                Task.Run(
                async () =>
                {

                    await tvService.Login();

                    /*
                    var qualities = await tvService.GetStreamQualities();
                    foreach (var q in qualities)
                    {
                        Console.WriteLine(q.Name.PadRight(20) + "  " + q.Id.PadLeft(10) + "  " + q.Allowed);
                    }
                    */

                    //await tvService.Unlock();
                    //await sledovaniTV.Lock();

                    var channels = await tvService.GetChanels();

                    foreach (var ch in channels)
                    {
                        Console.WriteLine(ch.Name);
                        Console.WriteLine("  ID     :" + ch.Id);
                        Console.WriteLine("  EPGID  :" + ch.EPGId);
                        Console.WriteLine("  Number :" + ch.ChannelNumber);
                        Console.WriteLine("  Locked :" + ch.Locked);
                        Console.WriteLine("  Url    :" + ch.Url);
                        Console.WriteLine("  Type   :" + ch.Type);
                        Console.WriteLine("  Group  :" + ch.Group);
                        Console.WriteLine("  LogoUrl:" + ch.LogoUrl);
                        Console.WriteLine("-----------------------");
                    }

                    var channelsEPG = await tvService.GetChannelsEPG();
                    foreach (var epg in channelsEPG)
                    {
                        Console.WriteLine($"  {epg.Key}");
                        foreach (var epgItem in epg.Value)
                        {
                            var time = epgItem.Start.ToString("dd.MM.yyyy HH:mm") + "-" + epgItem.Finish.ToString("HH:mm");
                            Console.WriteLine($"   {time}  : {epgItem.Title}   [{epgItem.EPGId}]");

                            // getting EPG detail:

                            var description = await tvService.GetEPGItemDescription(epgItem);

                            Console.WriteLine($"                                   {description}");
                        }
                    }


                    //Console.WriteLine();
                    //Console.WriteLine("Press any key");
                });


            Console.ReadKey();
        }
    }
}