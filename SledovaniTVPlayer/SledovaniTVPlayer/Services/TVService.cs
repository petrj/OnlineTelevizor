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

            _sledovaniTV = new SledovaniTV(loggingService);
            _sledovaniTV.SetCredentials(_config.Username, _config.Password);

            _sledovaniTV.Connection = new DeviceConnection()
            {
                deviceId = _config.DeviceId,
                password = _config.DevicePassword
            };
        }

        public async Task<ObservableCollection<EPGItem>> GetEPG()
        {
            var result = new ObservableCollection<EPGItem>();

            try
            {
                var epg = await _sledovaniTV.GetEPG();

                if (_sledovaniTV.Status == StatusEnum.Logged || _sledovaniTV.Status == StatusEnum.Paired)
                {
                    foreach (var ei in epg)
                    {
                        result.Add(ei);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting EPG");
            }

            return result;
        }

        public async Task<ObservableCollection<ChannelItem>> GetChannels()
        {
            var chs = new ObservableCollection<ChannelItem>();

            try
            {
                var channels = await _sledovaniTV.GetChanels();

                if (_sledovaniTV.Status == StatusEnum.Logged || _sledovaniTV.Status == StatusEnum.Paired)
                {
                    if (String.IsNullOrEmpty(_config.DeviceId))
                    {
                        // saving device connection to configuration
                        _config.DeviceId = _sledovaniTV.Connection.deviceId;
                        _config.DevicePassword = _sledovaniTV.Connection.password;
                    }

                    foreach (var ch in channels)
                    {
                        if (ch.Locked != "none")
                        {
                            if (!_config.ShowLocked)
                                continue;
                        }

                        chs.Add(ChannelItem.CreateFromChannel(ch));
                    }
                }
            } catch (Exception ex)
            {
                _log.Error(ex, "Error getting channels");
            }

            return chs;
        }

        public async Task ResetConnection()
        {
            _sledovaniTV.ResetConnection();
            _config.DeviceId = null;
            _config.DevicePassword = null;
            _sledovaniTV.SetCredentials(_config.Username, _config.Password);
            await _sledovaniTV.Login();
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
