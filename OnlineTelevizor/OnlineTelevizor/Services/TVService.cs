using LoggerService;
using Newtonsoft.Json;
using SledovaniTVAPI;
using OnlineTelevizor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TVAPI;
using KUKITVAPI;

namespace OnlineTelevizor.Services
{
    public class TVService
    {
        private ILoggingService _log;
        IOnlineTelevizorConfiguration _config;
        private bool _adultChannelsUnlocked = false;

        private ITVAPI _service;

        public TVService(ILoggingService loggingService, IOnlineTelevizorConfiguration config)
        {
            _log = loggingService;
            _config = config;

            InitTVService();
        }

        private void InitTVService()
        {
            if (_config.TVApi == TVAPIEnum.SledovaniTV)
            {
                _service = new SledovaniTV(_log);
                _service.SetCredentials(_config.Username, _config.Password, _config.ChildLockPIN);
                _service.SetConnection(_config.DeviceId, _config.DevicePassword);
            }
            else
            {
                _service = new KUKITV(_log);
                _service.SetConnection(_config.KUKIsn, null);
            }
        }

        public async Task<ObservableCollection<EPGItem>> GetEPG()
        {
            var result = new ObservableCollection<EPGItem>();

            try
            {
                var epg = await _service.GetEPG();

                if (_service.Status == StatusEnum.ConnectionNotAvailable)
                {
                    // repeat again after 1500 ms
                    await Task.Delay(1500);
                    epg = await _service.GetEPG();
                }

                if (_service.Status == StatusEnum.Logged)
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
                var qualities = await _service.GetStreamQualities();

                if (_service.Status != StatusEnum.Logged)
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
                    await _service.Unlock();
                }

                if (!_config.ShowAdultChannels &&
                    _adultChannelsUnlocked)
                {
                    _adultChannelsUnlocked = false;
                    await _service.Lock();
                }

                var channels = await _service.GetChanels();

                if (_service.Status == StatusEnum.ConnectionNotAvailable)
                {
                    // repeat again after 1500 ms
                    await Task.Delay(1500);
                    channels = await _service.GetChanels();
                }

                if (_service.Status == StatusEnum.Logged)
                {
                    if (String.IsNullOrEmpty(_config.DeviceId))
                    {
                        // saving device connection to configuration
                        _config.DeviceId = _service.Connection.deviceId;
                        _config.DevicePassword = _service.Connection.password;
                    }

                    var channelIndex = 0; 
                    foreach (var ch in channels)
                    {
                        if (ch.Locked != "none")
                        {
                            // locked or unavailable channels

                            if (ch.Locked == "noAccess" && !_config.ShowLocked)
                                continue;

                            if (ch.Locked == "pin" &&
                                (
                                    !_config.ShowAdultChannels || String.IsNullOrEmpty(_config.ChildLockPIN)
                                ))
                                continue; // adult channels 

                            // unknown Locked state
                        }

                        if ( !_config.Purchased && (!(
                                                        // sledovanitv
                                                        (ch.Id == "ct24") ||
                                                        (ch.Id == "ct2") ||
                                                        (ch.Id == "radio_country") ||
                                                        (ch.Id == "fireplace") ||
                                                        (ch.Id == "retro") ||
                                                        (ch.Id == "nasatv") ||
                                                        // kuki
                                                        (ch.Id == "ct2hd") ||
                                                        (ch.Id == "seznam") ||
                                                        (ch.Id == "kinosvet") ||
                                                        (ch.Id == "croradiozurnal") ||
                                                        (ch.Id == "radiojunior") 
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
            _service.ResetConnection();           

            _adultChannelsUnlocked = false;

            _config.DeviceId = null;
            _config.DevicePassword = null;

            InitTVService();

            await _service.Login();
        }

        public StatusEnum Status
        {
            get
            {
                return _service.Status;
            }
        }

        public bool EPGEnabled
        {
            get
            {
                return _service.EPGEnabled;
            }
        }
    }
}
