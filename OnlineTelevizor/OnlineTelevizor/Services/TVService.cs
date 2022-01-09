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
using DVBStreamerAPI;
using O2TVAPI;

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
            switch (_config.TVApi)
            {
                case TVAPIEnum.SledovaniTV:
                    _service = new SledovaniTV(_log);
                    _service.SetCredentials(_config.Username, _config.Password, _config.ChildLockPIN);
                    _service.SetConnection(_config.DeviceId, _config.DevicePassword);
                    break;
                case TVAPIEnum.KUKI:
                    _service = new KUKITV(_log);
                    _service.SetConnection(_config.KUKIsn, null);
                    break;
                case TVAPIEnum.DVBStreamer:
                    _service = new DVBStreamerClient(_log);
                    _service.SetConnection(_config.DVBStreamerUrl, null);
                    break;
                case TVAPIEnum.O2TV:
                    _service = new O2TV(_log);
                    _service.SetConnection(_config.O2TVUsername, _config.O2TVPassword);
                    break;
            }
        }


        public async Task<Dictionary<string, List<EPGItem>>>  GetEPG()
        {
            var epg = new Dictionary<string, List<EPGItem>>();

            try
            {
                epg = await _service.GetChannelsEPG();

                if (_service.Status == StatusEnum.ConnectionNotAvailable)
                {
                    // repeat again after 1500 ms
                    await Task.Delay(1500);
                    epg = await _service.GetChannelsEPG();
                }
            }
            catch (Exception ex)
            {
                epg = new Dictionary<string, List<EPGItem>>();
                _log.Error(ex, "Error getting EPG");
            }

            return epg;
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

        public async Task<string> GetEPGItemDescription(EPGItem epgItem)
        {
            return await _service.GetEPGItemDescription(epgItem);
        }

        public async Task StopStream()
        {
            await _service.Stop();
        }

        public async Task<ObservableCollection<ChannelItem>> GetChannels()
        {
            var chs = new ObservableCollection<ChannelItem>();

            if (_service.Status != StatusEnum.Logged)
            {
                if (_service.Status == StatusEnum.LoginFailed ||
                    _service.Status == StatusEnum.PairingFailed ||
                    _service.Status == StatusEnum.NotInitialized ||
                    _service.Status == StatusEnum.EmptyCredentials
                    )
                return chs;
            }

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
                                    !_config.ShowAdultChannels
                                ))
                                continue; // adult channels

                            // unknown Locked state
                        }

                        if (!_config.Purchased)
                        {
                            // channel filter

                            if (_config.TVApi == TVAPIEnum.SledovaniTV)
                            {
                                if (!(
                                        (ch.Id == "ct24") ||
                                        (ch.Id == "ct2") ||
                                        (ch.Id == "radio_country") ||
                                        (ch.Id == "fireplace") ||
                                        (ch.Id == "retro") ||
                                        (ch.Id == "nasatv")
                                        ))
                                    continue;
                            }

                            if (_config.TVApi == TVAPIEnum.KUKI)
                            {
                                if (!(
                                        (ch.Id == "ct2hd") ||
                                        (ch.Id == "seznam") ||
                                        (ch.Id == "kinosvet") ||
                                        (ch.Id == "croradiozurnal") ||
                                        (ch.Id == "radiojunior")
                                        ))
                                    continue;
                            }

                            if (_config.TVApi == TVAPIEnum.O2TV)
                            {
                                if (channelIndex > 5)
                                    continue;
                            }
                        }

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
                if (_service == null)
                    return StatusEnum.NotInitialized;

                return _service.Status;
            }
        }

        public bool EPGEnabled
        {
            get
            {
                if (_service == null)
                    return false;

                return _service.EPGEnabled;
            }
        }

        public bool QualityFilterEnabled
        {
            get
            {
                if (_service == null)
                    return false;

                return _service.QualityFilterEnabled;
            }
        }
    }
}
