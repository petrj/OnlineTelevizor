using LoggerService;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TVAPI;

namespace DemoTVAPI
{
    public class Demo : ITVAPI
    {
        private ILoggingService _log;

        private List<Channel> _channels = new List<Channel>();
        private List<EPGItem> _epg = new List<EPGItem>();

        public Demo(ILoggingService loggingService)
        {
            _log = loggingService;

            _channels = new List<Channel>()
                    {
                        new Channel()
                        {
                             Name = "Big Buck Bunny",
                             EPGId = "1",
                             Id = "1",
                             ChannelNumber = "1",
                             Group = "Anime",
                             error = null,
                             Locked = "none",
                             LogoUrl = null,
                             status = null,
                             SubTitles = null,
                             Type = "Video",
                             Url = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
                        },
                        new Channel()
                        {
                             Name = "Radio Beat",
                             EPGId = "2",
                             Id = "2",
                             ChannelNumber = "2",
                             Group = "Rock music",
                             error = null,
                             Locked = "none",
                             LogoUrl = null,
                             status = null,
                             SubTitles = null,
                             Type = "Radio",
                             Url = "https://stream.rcs.revma.com/3d47nqvb938uv"
                        },
                        new Channel()
                        {
                             Name = "Český rozhlas Plus",
                             EPGId = "3",
                             Id = "3",
                             ChannelNumber = "3",
                             Group = "Český rozhlas",
                             error = null,
                             Locked = "none",
                             LogoUrl = null,
                             status = null,
                             SubTitles = null,
                             Type = "Radio",
                             Url = "http://amp.cesnet.cz:8000/cro-plus.ogg"
                        }
                    };

            _epg = new List<EPGItem>()
                   {
                        new EPGItem()
                        {
                             ChannelId = "1",
                             Description = "Big Buck Bunny tells the story of a giant rabbit with a heart bigger than himself. When one sunny day three rodents rudely harass him, something snaps... and the rabbit ain't no bunny anymore! In the typical cartoon tradition he prepares the nasty rodents a comical revenge.\n\nLicensed under the Creative Commons Attribution license\nhttp://www.bigbuckbunny.org",
                             EPGId = "1",
                             error = null,
                             Start = DateTime.Now.Date,
                             Finish = DateTime.Now.Date.AddDays(1),
                             Title = "Rabbit story"
                        },
                        new EPGItem()
                        {
                             ChannelId = "2",
                             Description = "Live stream rádia Beat",
                             EPGId = "1",
                             error = null,
                             Start = DateTime.Now.Date,
                             Finish = DateTime.Now.Date.AddDays(1),
                             Title = "První bigbít u nás"
                        },
                        new EPGItem()
                        {
                             ChannelId = "3",
                             Description = "Live stream rádia Plus",
                             EPGId = "1",
                             error = null,
                             Start = DateTime.Now.Date,
                             Finish = DateTime.Now.Date.AddDays(1),
                             Title = "Analyticko-publicistická stanice"
                        }
                   };
        }

        public void AddCustomChannel(string name, string url, string tp = "Video")
        {
            var c = (_channels.Count + 1).ToString();

            if (string.IsNullOrEmpty(tp))
            {
                tp = "Video";
            }

            _channels.Add(
            new Channel()
            {
                Name = name,
                EPGId = c,
                Id = c,
                ChannelNumber = c,
                Group = "Custom",
                error = null,
                Locked = "none",
                LogoUrl = null,
                status = null,
                SubTitles = null,
                Type = tp,
                Url = url
            });
        }

        public DeviceConnection Connection
        {
            get
            {
                return new DeviceConnection();
            }
        }

        public bool EPGEnabled
        {
            get { return true; }
        }

        public bool SubtitlesEnabled
        {
            get { return false; }
        }

        public bool QualityFilterEnabled
        {
            get { return false; }
        }

        public bool AdultLockEnabled
        {
            get { return false; }
        }

        public StatusEnum Status
        {
            get { return StatusEnum.Logged; }
        }

        public string LastErrorDescription
        {
            get { return String.Empty; }
        }

        public Task<List<Channel>> GetChannels(string quality = null)
        {
            return Task.Run(
               () =>
               {
                   return _channels;
               });
        }

        public async Task<Dictionary<string, List<EPGItem>>> GetChannelsEPG()
        {
            var res = new Dictionary<string, List<EPGItem>>();

            var channels = await GetChannels();
            var epg = await GetEPG();

            foreach (var channel in channels)
            {
                res.Add(channel.Id, new List<EPGItem>());

                foreach (var epgItem in epg)
                {
                    if (epgItem.ChannelId == channel.Id)
                    {
                        res[channel.Id].Add(epgItem);
                    }
                }
            }

            return res;
        }

        public Task<List<EPGItem>> GetEPG()
        {
            return Task.Run(
              () =>
              {
                  return _epg;
              });
        }

        public Task<string> GetEPGItemDescription(EPGItem epgItem)
        {
            return Task.Run(
            () =>
            {
                return epgItem.Description;
            });
        }

        public Task<List<Quality>> GetStreamQualities()
        {
            return Task.Run(
                () =>
                {
                    return new List<Quality>() { new Quality() { Id = "1", Name = "Standard" } };
                });
        }

        public Task Lock()
        {
            return Task.Run(() => { });
        }

        public Task Login(bool force = false)
        {
            return Task.Run(() => { });
        }

        public void ResetConnection()
        {
        }

        public void SetConnection(string deviceId, string password)
        {
        }

        public void SetCredentials(string username, string password, string childLockPIN = null)
        {
        }

        public Task Stop()
        {
            return Task.Run(() => { });
        }

        public Task Unlock()
        {
            return Task.Run(() => { });
        }
    }
}