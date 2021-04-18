using LoggerService;
using SledovaniTVAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Globalization;
using TVAPI;
using System.Web;
using System.Linq;

namespace SledovaniTVAPI
{
    public class SledovaniTV : ITVAPI
    {
        private ILoggingService _log;
        private const string ServiceUrl = "http://sledovanitv.cz/api/";

        private Credentials _credentials;
        private DeviceConnection _deviceConnection;
        private Session _session;
        private StatusEnum _status = StatusEnum.NotInitialized;

        public SledovaniTV(ILoggingService loggingService)
        {
            _log = loggingService;
            _deviceConnection = new DeviceConnection();
            _session = new Session();
        }

        public void SetCredentials(string username, string password, string childLockPIN = null)
        {
            _credentials = new Credentials()
            {
                Username = username,
                Password = password,
                ChildLockPIN = childLockPIN
            };
        }

        public void SetConnection(string deviceId, string password)
        {
            _deviceConnection.deviceId = deviceId;
            _deviceConnection.password = password;
        }

        public bool EPGEnabled
        {
            get
            {
                return true;
            }
        }

        public DeviceConnection Connection
        {
            get
            {
                return _deviceConnection;
            }
        }

        public string PHPSESSID
        {
            get
            {
                if (_session == null || String.IsNullOrEmpty(_session.PHPSESSID))
                {
                    return null;
                }

                return _session.PHPSESSID;
            }
        }
        
        public StatusEnum Status
        {
            get
            {
                return _status;
            }
        }

        private async Task<string> SendRequest(string functionName, Dictionary<string, string> parameters)
        {
            _log.Debug($"Calling function {functionName}");

            var url = ServiceUrl + functionName;

            var first = true;
            foreach (var kvp in parameters)
            {
                if (first)
                {
                    first = false;
                    url += "?";
                }
                else
                {
                    url += "&";
                }
                url += $"{kvp.Key}={kvp.Value}";
            }

            var result = await SendRequest(url);

            return result;
        }

        private async Task<string> SendRequest(string url, string method = "GET")
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);

                var contentType = "application/x-www-form-urlencoded";

                request.Method = method;
                request.ContentType = contentType;
                request.Accept = "application/json";
                request.Timeout = 10 * 1000; // 10 sec timeout per one request

                _log.Debug($"Sending {method} request to url: {request.RequestUri}");
                _log.Debug($"ContentType: {request.ContentType}");
                _log.Debug($"Method: {request.Method}");
                _log.Debug($"RequestUri: {request.RequestUri}");
                _log.Debug($"Timeout: {request.Timeout}");
                _log.Debug($"ContentType: {request.ContentType}");
                _log.Debug($"ContentLength: {request.ContentLength}");

                foreach (var header in request.Headers)
                {
                    _log.Debug($"Header: {header.ToString()}");
                }

                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    string responseString;
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        responseString = sr.ReadToEnd();
                    }

                    _log.Debug($"Response: {responseString}");
                    _log.Debug($"StatusCode: {response.StatusCode}");
                    _log.Debug($"StatusDescription: {response.StatusDescription}");

                    _log.Debug($"ContentLength: {response.ContentLength}");
                    _log.Debug($"ContentType: {response.ContentType}");
                    _log.Debug($"ContentEncoding: {response.ContentEncoding}");

                    return responseString;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Pairing device with user credentials (_credentials)
        /// </summary>
        private async Task CreatePairing()
        {
            _log.Debug($"Pairing device with user credentials");

            try
            {
                var ps = new Dictionary<string, string>()
                {
                    { "username", _credentials.Username },
                    { "password", _credentials.Password },
                    { "type", "samsungtv" }
                };

                var deviceConnectionString = await SendRequest("create-pairing", ps);
                var devConnJson = JObject.Parse(deviceConnectionString);

                if (
                    ((devConnJson.HasValue("status") && (devConnJson.GetStringValue("status") == "0"))) ||
                    ((devConnJson.HasValue("error")) && (devConnJson.GetStringValue("error") == "bad login")) ||
                    (!devConnJson.HasValue("deviceId"))
                   )
                {
                    _status = StatusEnum.PairingFailed;
                }
                else
                {
                    _status = StatusEnum.Paired;

                    _deviceConnection = new DeviceConnection()
                    {
                        deviceId = devConnJson.GetStringValue("deviceId").ToString(),
                        password = devConnJson.GetStringValue("password").ToString()
                    };

                    _log.Debug("Received User Connection:");
                    _log.Debug(_deviceConnection.ToString());
                }
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Error while pairing device");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while pairing device");
                _status = StatusEnum.GeneralError;
            }
        }

        /// <summary>
        /// Login device to service
        /// </summary>
        private async Task DeviceLogin()
        {
            _log.Debug("Login device to service");

            try
            {
               var ps = new Dictionary<string, string>()
                {
                    { "deviceId", _deviceConnection.deviceId },
                    { "password", _deviceConnection.password },
                    { "version", "3.2.004" },
                    { "lang", "cs" },
                    { "unit", "default" }
                };

                var sessionString = await SendRequest("device-login", ps);
                var sessionJson = JObject.Parse(sessionString);

                if (
                    ((sessionJson.HasValue("status") && (sessionJson.GetStringValue("status") == "0"))) ||
                    ((sessionJson.HasValue("error")) && (sessionJson.GetStringValue("error") == "bad login")) ||
                    (!sessionJson.HasValue("PHPSESSID"))
                   )
                {
                    _status = StatusEnum.LoginFailed;
                }
                else
                {
                    _session = new Session()
                    {
                        PHPSESSID = sessionJson.GetStringValue("PHPSESSID")
                    };

                    _status = StatusEnum.Logged;
                }

            }
            catch (WebException wex)
            {
                _log.Error(wex, "Login failed");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Login failed");
                _status = StatusEnum.ConnectionNotAvailable;
            }
        }

        public async Task Login(bool force = false)
        {
            if (force)
                _status = StatusEnum.NotInitialized;

            if (!force && _session != null && !String.IsNullOrEmpty(_session.PHPSESSID))
            {
                _status = StatusEnum.Logged;
            }

            _log.Debug("Login");

            if (!force && Status == StatusEnum.Logged)
            {
                _log.Debug("Device is already logged");
                return;
            }

            if (String.IsNullOrEmpty(_credentials.Username) ||
                String.IsNullOrEmpty(_credentials.Password))
            {
                _status = StatusEnum.EmptyCredentials;
                _log.Debug("Empty credentials");
                return;
            }

            if (!force && _deviceConnection != null && !String.IsNullOrEmpty(_deviceConnection.deviceId))
            {
                _status = StatusEnum.Paired;
            }

            if (Status != StatusEnum.Paired)
            {
                await CreatePairing();

                if (Status != StatusEnum.Paired)
                {
                    _log.Debug("Pairing failed");
                    return; // bad credentials, no internet connection ?
                }
            }

            // login

            await DeviceLogin();

            if (!force && Status == StatusEnum.LoginFailed)
            {
                // bad device connection ? Pairing again
                await CreatePairing();

                if (Status != StatusEnum.Paired)
                {
                    _log.Debug("Pairing failed again");
                    return; // bad credentials, no internet connection ?
                }

                await DeviceLogin();
            }
        }

        public async Task<string> GetEPGItemDescription(EPGItem epgItem)
        {
            return epgItem.Description;
        }

        public async Task<Dictionary<string, List<EPGItem>>> GetChannelsEPG()
        {
            var epg = await GetEPG();

            var resNotSorted = new Dictionary<string, List<EPGItem>>();
            var res = new Dictionary<string, List<EPGItem>>();

            foreach (var epgItem in epg)
            {
                if (!resNotSorted.ContainsKey(epgItem.ChannelId))
                {
                    resNotSorted.Add(epgItem.ChannelId, new List<EPGItem>());
                }

                resNotSorted[epgItem.ChannelId].Add(epgItem);
            }

            // sorting each channel
            foreach (var chId in resNotSorted.Keys)
            {
                res.Add(chId, resNotSorted[chId].OrderBy(e => e.Start).ToList());
            }

            return res;
        }

        /// <summary>
        /// Getting actual EPG
        /// </summary>
        public async Task<List<EPGItem>>GetEPG()
        {
            _log.Debug($"Refreshing EPG");

            var result = new List<EPGItem>();

            await Login();

            try
            {
                var ps = new Dictionary<string, string>()
                {
                    { "PHPSESSID", _session.PHPSESSID },
                    { "detail", "1" },
                    { "duration", "180" }
                };

                var epgString = await SendRequest("epg", ps);
                var epgJson = JObject.Parse(epgString);

                // has session expired?
                if (epgJson.HasValue("status") &&
                    epgJson.GetStringValue("status") == "0" &&
                    epgJson.HasValue("error") &&
                    epgJson.GetStringValue("error") == "not logged")
                {
                    _log.Info("Received status 0, login again");

                    _session.PHPSESSID = null;

                    await Login(true);

                    if (Status == StatusEnum.Logged)
                    {
                        ps["PHPSESSID"] = _session.PHPSESSID;
                        epgString = await SendRequest("epg", ps);
                        epgJson = JObject.Parse(epgString);
                    }
                    else
                    {
                        _log.Info($"Login again failed (status: {Status})");
                        return result;
                    }
                }

                if (epgJson.HasValue("status") &&
                    epgJson.GetStringValue("status")=="1" &&
                    epgJson.HasValue("channels"))
                {
                    foreach (var epgCh in epgJson.GetValue("channels"))
                    {
                        // id from path (channels.ct1")
                        var chId = epgCh.Path.Substring(9);

                        foreach (var epg in epgJson.GetValue("channels")[chId])
                        {
                            var title = epg["title"].ToString();
                            var times = epg["startTime"].ToString();
                            var timef = epg["endTime"].ToString();
                            var desc = epg["description"].ToString();
                            var epgEventId = epg["eventId"].ToString();

                            var item = new EPGItem()
                            {
                                ChannelId = chId,
                                EPGId = epgEventId,
                                Title = title,
                                Start = DateTime.ParseExact(times, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                                Finish = DateTime.ParseExact(timef, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                                Description = desc
                            };

                            result.Add(item);
                        };
                     }
                }
            }
            catch (WebException wex)
            {
                _log.Error(wex, "EPG loading failed");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "EPG loading failed");
                _status = StatusEnum.GeneralError;
            }

            return result;
        }

        /// <summary>
        /// Getting stream qualities
        /// </summary>
        public async Task<List<Quality>> GetStreamQualities()
        {
            _log.Debug($"Getting stream qualities");

            var result = new List<Quality>();

            await Login();

            if (_status != StatusEnum.Logged)
                return result;

            try
            {
                var ps = new Dictionary<string, string>()
                {
                    { "PHPSESSID", _session.PHPSESSID }
                };

                var streamQualityResponseString = await SendRequest("get-stream-qualities", ps);
                var streamQualityJson = JObject.Parse(streamQualityResponseString);

                // has session expired?
                if (streamQualityJson.HasValue("status") &&
                   streamQualityJson.GetStringValue("status") == "0")
                {
                    _log.Info("Received status 0, login again");

                    _session.PHPSESSID = null;

                    await Login(true);

                    if (Status == StatusEnum.Logged)
                    {
                        ps["PHPSESSID"] = _session.PHPSESSID;
                        streamQualityResponseString = await SendRequest("get-stream-qualities", ps);
                        streamQualityJson = JObject.Parse(streamQualityResponseString);
                    }
                    else
                    {
                        _log.Info($"Login again failed (status: {Status})");
                        return result;
                    }
                }

                if (streamQualityJson.HasValue("status") &&
                   streamQualityJson.GetStringValue("status") == "1" &&
                   streamQualityJson.HasValue("qualities"))
                {
                    foreach (var qToken in streamQualityJson.GetValue("qualities"))
                    {
                        var q = JObject.Parse(qToken.ToString());
                        var id = q["id"];

                        var quality = new Quality()
                        {
                            Id = q["id"].ToString(),
                            Name = q["name"].ToString(),
                            Allowed = q["allowed"].ToString()
                        };

                        result.Add(quality);
                    }
                }
            }
            catch (WebException wex)
            {
                _log.Error(wex, "EPG loading failed");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Getting stream qualities failed");
                _status = StatusEnum.GeneralError;
            }

            return result;
        }

        public void ResetConnection()
        {
            _log.Debug("Resetting connection");

            _status = StatusEnum.NotInitialized;
            _deviceConnection.deviceId = null;
            _deviceConnection.password = null;
            _session.PHPSESSID = null;
        }

        public async Task<List<Channel>> GetChanels()
        {
            var result = new List<Channel>();

            await Login();

            if (_status != StatusEnum.Logged)
                return result;

            try
            {
                _log.Debug($"Reloading channels");

                var ps = new Dictionary<string, string>()
                {
                    { "format", "androidtv" },
                    { "PHPSESSID", _session.PHPSESSID }
                };

                var channelsString = await SendRequest("playlist", ps);
                var channelsJson = JObject.Parse(channelsString);

                // has session expired?
                if (channelsJson.HasValue("status") &&
                    channelsJson.GetStringValue("status") == "0" &&
                    channelsJson.HasValue("error") &&
                    channelsJson.GetStringValue("error") == "not logged"
                   )
                {
                    _log.Info("Received status 0, login again");

                    _session.PHPSESSID = null;

                    await Login(true);

                    if (Status == StatusEnum.Logged)
                    {
                        ps["PHPSESSID"] = _session.PHPSESSID;
                        channelsString = await SendRequest("playlist", ps);
                        channelsJson = JObject.Parse(channelsString);
                    }
                    else
                    {
                        _log.Info($"Login again failed (status: {Status})");
                        return result;
                    }
                }

                var number = 1;

                if (channelsJson.HasValue("status") &&
                 channelsJson.GetStringValue("status") == "1" &&
                 channelsJson.HasValue("channels"))
                {
                    foreach (JObject channelJson in channelsJson["channels"])
                    {
                        var ch = new Channel()
                        {
                            ChannelNumber = number.ToString(),

                            Id = channelJson["id"].ToString(),
                            Name = channelJson["name"].ToString(),
                            Url = channelJson["url"].ToString(),

                            Type = channelJson["type"].ToString(),
                            LogoUrl = channelJson["logoUrl"].ToString(),
                            Locked = channelJson["locked"].ToString(),
                            Group = channelJson["group"].ToString()
                        };

                        number++;
                        result.Add(ch);
                    }

                    _log.Debug($"Received {result.Count} channels");
                } else
                {
                    _log.Debug($"No channel received (status <>1)");
                }
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Error while getting channels");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while getting channels");
                _status = StatusEnum.GeneralError;
            }

            return result;
        }

        public async Task Unlock()
        {
            await Login();

            if (_status != StatusEnum.Logged)
                return;

            _log.Debug("Unlocking adult channels");

            try
            {
                var ps = new Dictionary<string, string>()
                {
                    { "pin", _credentials.ChildLockPIN },
                    { "whitelogo", "0" },
                    { "PHPSESSID", _session.PHPSESSID }
                };

                var unlockResponseString = await SendRequest("pin-unlock", ps);
                var unlockResponseJson = JObject.Parse(unlockResponseString);

                // has session expired?
                if (unlockResponseJson.HasValue("status") &&
                   unlockResponseJson.GetStringValue("status") == "0")
                {
                    _log.Info("Received status 0, login again");

                    _session.PHPSESSID = null;

                    await Login(true);

                    if (Status == StatusEnum.Logged)
                    {
                        ps["PHPSESSID"] = _session.PHPSESSID;
                        unlockResponseString = await SendRequest("pin-unlock", ps);
                        unlockResponseJson = JObject.Parse(unlockResponseString);
                    }
                    else
                    {
                        _log.Info($"Login again failed (status: {Status})");
                        return;
                    }
                }

                if ((unlockResponseJson.HasValue("status")) &&
                    (unlockResponseJson.GetStringValue("status") == "1")
                   )
                {
                    // unlocked
                }
                else
                if ((unlockResponseJson.HasValue("error")) &&
                    (unlockResponseJson.GetStringValue("error") == "bad pin"))
                {
                    _status = StatusEnum.BadPin;
                }
                else
                {
                    _log.Info("Unknown result while unlocking adult channels");
                    return;
                }
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Error while unlocking adult channels");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while unlocking adult channels");
                _status = StatusEnum.GeneralError;
            }
        }

        public async Task Lock()
        {
            await Login();

            if (_status != StatusEnum.Logged)
                return;

            _log.Debug("Locking adult channels");

            try
            {
                var ps = new Dictionary<string, string>()
                {
                    { "PHPSESSID", _session.PHPSESSID }
                };

                await SendRequest("pin-lock", ps);

                var lockResponseString = await SendRequest("pin-lock", ps);
                var lockResponseJson = JObject.Parse(lockResponseString);

                // has session expired?
                if (lockResponseJson.HasValue("status") &&
                   lockResponseJson.GetStringValue("status") == "0")
                {
                    _log.Info("Received status 0, login again");

                    _session.PHPSESSID = null;

                    await Login(true);

                    if (Status == StatusEnum.Logged)
                    {
                        ps["PHPSESSID"] = _session.PHPSESSID;

                        await SendRequest("pin-lock", ps);
                    }
                    else
                    {
                        _log.Info($"Login again failed (status: {Status})");
                        return;
                    }
                }
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Error while locking adult channels");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while locking adult channels");
                _status = StatusEnum.GeneralError;
            }
        }

        public async Task Stop()
        {
            // nothing to stop
        }

        public string GetEPGEventUrl(EPGItem item)
        {
            var eventIdEncoded = HttpUtility.UrlEncode(item.EPGId);
            return $"http://sledovanitv.cz/vlc/api-timeshift/event.m3u8?PHPSESSID={_session.PHPSESSID}&eventId={eventIdEncoded}";
        }
    }
}
