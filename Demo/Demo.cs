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

        public Demo(ILoggingService loggingService)
        {
            _log = loggingService;
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
            get { return false; }
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
                   return new List<Channel>()
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
                        }
                    };
               });
        }

        public Task<Dictionary<string, List<EPGItem>>> GetChannelsEPG()
        {
            //throw new NotImplementedException();
            return null;
        }

        public Task<List<EPGItem>> GetEPG()
        {
            //throw new NotImplementedException();
            return null;
        }

        public Task<string> GetEPGItemDescription(EPGItem epgItem)
        {
            //throw new NotImplementedException();
            return null;
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