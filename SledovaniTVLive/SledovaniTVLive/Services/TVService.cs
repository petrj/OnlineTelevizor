using LoggerService;
using Newtonsoft.Json;
using SledovaniTVAPI;
using SledovaniTVLive.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SledovaniTVLive.Services
{
    public class TVService
    {
        private ILoggingService _log;
        ISledovaniTVConfiguration _config;
        private bool _adultChannelsUnlocked = false;

        private SledovaniTV _sledovaniTV;

        public TVService(ILoggingService loggingService, ISledovaniTVConfiguration config)
        {
            _log = loggingService;
            _config = config;

            _sledovaniTV = new SledovaniTV(loggingService);
            _sledovaniTV.SetCredentials(_config.Username, _config.Password, _config.ChildLockPIN);

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

        public async Task<List<QualityItem>> GetStreamQualities()
        {
            var result = new List<QualityItem>();

            try
            {
                var qualities = await _sledovaniTV.GetStreamQualities();

                if (_sledovaniTV.Status != StatusEnum.Logged)
                    return result;
                
                foreach (var q in qualities)
                {
                    if (q.Allowed == "0")
                        continue;

                    result.Add(new QualityItem()
                    {
                        Name = q.Name,
                        Id = q.Id
                    });
                }
                
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting stream qualities");
            }

            return result;
        }

        public async Task<ObservableCollection<ChannelItem>> GetChannels()
        {
            var chs = new ObservableCollection<ChannelItem>();

            try
            {
                if (_config.ShowAdultChannels && 
                    !String.IsNullOrEmpty(_config.ChildLockPIN) &&
                    !_adultChannelsUnlocked)
                {
                    _adultChannelsUnlocked = true;
                    await _sledovaniTV.Unlock();
                }

                if (!_config.ShowAdultChannels &&
                    _adultChannelsUnlocked)
                {
                    _adultChannelsUnlocked = false;
                    await _sledovaniTV.Lock();
                }

                var channels = await _sledovaniTV.GetChanels();

                if (_sledovaniTV.Status == StatusEnum.Logged || _sledovaniTV.Status == StatusEnum.Paired)
                {
                    if (String.IsNullOrEmpty(_config.DeviceId))
                    {
                        // saving device connection to configuration
                        _config.DeviceId = _sledovaniTV.Connection.deviceId;
                        _config.DevicePassword = _sledovaniTV.Connection.password;
                    }

                    var channelIndex = 0; 
                    foreach (var ch in channels)
                    {
                        if (ch.Locked != "none")
                        {
                            // locked or unavailable channels

                            if (ch.Locked == "noAccess" && !_config.ShowLocked)
                                continue;

                            if (ch.Locked == "pin" && !_config.ShowAdultChannels)
                                continue; // adult channels 

                            // unknown Locked state
                        }

                        if ( !_config.Purchased && (!(
                                                        (ch.Id == "ct24") ||
                                                        (ch.Id == "ct2") ||
                                                        (ch.Id == "radio_country") ||
                                                        (ch.Id == "fireplace") ||
                                                        (ch.Id == "retro") ||
                                                        (ch.Id == "nasatv")
                                                      )))
                           continue;

                        channelIndex++;

                        var channelItem = ChannelItem.CreateFromChannel(ch);
                        channelItem.ChannelNumber = channelIndex.ToString();
                        chs.Add(channelItem);
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
            _adultChannelsUnlocked = false;
            _sledovaniTV.ResetConnection();
            _config.DeviceId = null;
            _config.DevicePassword = null;
            _sledovaniTV.SetCredentials(_config.Username, _config.Password, _config.ChildLockPIN);
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
