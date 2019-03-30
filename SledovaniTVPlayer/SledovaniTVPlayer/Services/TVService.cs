using LoggerService;
using Newtonsoft.Json;
using SledovaniTVAPI;
using SledovaniTVPlayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SledovaniTVPlayer.Services
{
    public class TVService
    {
        private ILoggingService _log;
        ISledovaniTVConfiguration _config;

        private SledovaniTV _sledovaniTV;

        public TVService(ILoggingService loggingService, ISledovaniTVConfiguration config)
        {
            _log = loggingService;
            _config = config;

            var credentials = new Credentials()
            {
                Username = _config.Username,
                Password = _config.Password
            };

            _sledovaniTV = new SledovaniTV(credentials, loggingService);

            _sledovaniTV.Connection = new DeviceConnection()
            {
                deviceId = _config.DeviceId,
                password = _config.DevicePassword
            };
        }

        public async Task<ObservableCollection<TVChannel>> GetChannels()
        {
            var chs = new ObservableCollection<TVChannel>();

            try
            {
                await _sledovaniTV.ReloadChanels();

                if (_sledovaniTV.Status == StatusEnum.Logged || _sledovaniTV.Status == StatusEnum.Paired)
                {
                    
                    if (String.IsNullOrEmpty(_config.DeviceId))
                    {
                        // saving device connection to configuration
                        _config.DeviceId = _sledovaniTV.Connection.deviceId;
                        _config.DevicePassword = _sledovaniTV.Connection.password;
                    }

                    int i = 1;
                    foreach (var ch in _sledovaniTV.Channels.channels)
                    {
                        chs.Add(new TVChannel()
                        {
                            Name = ch.name,
                            Url = ch.url
                        });

                        i++;
                    }
                }
            } catch (Exception ex)
            {
                _log.Error(ex, "Error getting channels");
            }

            return chs;
        }

        public void ResetStatus()
        {
            _sledovaniTV.ResetStatus();
        }

        public StatusEnum Status
        {
            get
            {
                return _sledovaniTV.Status;
            }
        }
    }
}
