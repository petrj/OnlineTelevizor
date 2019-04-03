using LoggerService;
using SledovaniTVAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SledovaniTVAPI
{
    public class SledovaniTV
    {
        private ILoggingService _log;
        private const string ServiceUrl = "http://sledovanitv.cz/api/";

        private Credentials _credentials;
        private DeviceConnection _deviceConnection;
        private Session _session;
        private StatusEnum _status = StatusEnum.NotInitialized;

        public List<TVChannel> Channels { get; set; }

        public SledovaniTV(ILoggingService loggingService)
        {
            _log = loggingService;
            Channels = new List<TVChannel>();
        }

        public void SetCredentials(string username, string password)
        {
            _credentials = new Credentials()
            {
                Username = username,
                Password = password
            };
        }

        public StatusEnum Status
        {
            get
            {
                return _status;
            }
        }

        public DeviceConnection Connection
        {
            get
            {
                return _deviceConnection;
            }
            set
            {
                _deviceConnection = value;
            }
        }

        private async Task<T> SendJSONRequest<T>(string functionName, Dictionary<string, string> pars)
        {
            var result = await SendRequest(functionName, pars);
            return JsonConvert.DeserializeObject<T>(result);
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
                request.Timeout = 100 * 60 * 1000; // 100 min timeout per one request

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

                _deviceConnection = await SendJSONRequest<DeviceConnection>("create-pairing", ps);

                _log.Debug("Received User Connection:");
                _log.Debug(_deviceConnection.ToString());

                if (String.IsNullOrEmpty(_deviceConnection.deviceId))
                {
                    _status = StatusEnum.PairingFailed;
                }
                else
                {
                    _status = StatusEnum.Paired;
                }

            } catch (Exception ex)
            {
                _log.Error(ex, "Error while pairing device");
                _status = StatusEnum.PairingFailed;
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

                // Using SendJSONRequest<Session> in real Android leads to Newtonsoft.Json.JsonSerializationException
                //_session = await SendJSONRequest<Session>("device-login", ps);
                var _sessionString = await SendRequest("device-login", ps);
                _session = new Session();

                if (_sessionString.StartsWith("{\"status\":1,\"PHPSESSID\":\""))
                {
                    _session.PHPSESSID = _sessionString.Substring(25, _sessionString.Length - 27);
                }

                if (_session.status == "0" ||
                    _session.error == "bad login" ||
                    String.IsNullOrEmpty(_session.PHPSESSID))
                {
                    _status = StatusEnum.LoginFailed;
                }
                else
                {
                    _status = StatusEnum.Logged;
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex, "Login failed");
                _status = StatusEnum.LoginFailed;
            }
        }

        public void ResetConnection()
        {
            _log.Debug("Resetting connection");

            _status = StatusEnum.NotInitialized;
            _deviceConnection.deviceId = null;
            _deviceConnection.password = null;
            _session.PHPSESSID = null;
        }

        public async Task Login()
        {
            if (_session != null && !String.IsNullOrEmpty(_session.PHPSESSID))
            {
                _status = StatusEnum.Logged;
            }

            _log.Info("Login");

            if (Status == StatusEnum.Logged)
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

           if (_deviceConnection != null && !String.IsNullOrEmpty(_deviceConnection.deviceId))
            {
                _status = StatusEnum.Paired;
            }

            if (Status != StatusEnum.Paired)
            {
                await CreatePairing();

                if (Status == StatusEnum.PairingFailed)
                {
                    _log.Debug("Pairing failed");
                    return; // bad credentials, no internet connection ?
                }
            }

            // login

            await DeviceLogin();

            if (Status == StatusEnum.LoginFailed)
            {
                // bad device connection ? Pairing again
                await CreatePairing();

                if (Status == StatusEnum.PairingFailed)
                {
                    _log.Debug("Pairing failed again");
                    return; // bad credentials, no internet connection ?
                }

                await DeviceLogin();
            }
        }

        public async Task ReloadChanels()
        {
            await Login();

            if (_status != StatusEnum.Logged)
                return;

            try
            {
                _log.Info($"Reloading channels");

                var ps = new Dictionary<string, string>()
                {
                    { "format", "androidtv" },
                    { "PHPSESSID", _session.PHPSESSID }
                };

                Channels.Clear();

                var channelsString = await SendRequest("playlist", ps);
                var channelsJson = JObject.Parse(channelsString);

                var number = 1;
                foreach (JObject channelJson in channelsJson["channels"])
                {
                    var ch = new TVChannel()
                    {
                        ChannelNumber = number.ToString(),

                        Id = channelJson["id"].ToString(),
                        Name = channelJson["name"].ToString(),
                        Url = channelJson["url"].ToString(),

                        Type = channelJson["type"].ToString(),
                        LogoUrl = channelJson["logoUrl"].ToString(),
                        Locked = channelJson["locked"].ToString(),
                        ParentLocked = channelJson["parentLocked"].ToString(),
                        Group = channelJson["group"].ToString()
                    };

                    number++;
                    Channels.Add(ch);
                }

                _log.Info($"Received {Channels.Count} channels");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while refreshing channels");
            }
        }

        public async Task Unlock()
        {
            await Login();

            if (_status != StatusEnum.Logged)
                return;

            var ps = new Dictionary<string, string>()
            {
                { "pin", _credentials.ChildLockPIN },
                { "whitelogo", "1" },
                { "PHPSESSID", _session.PHPSESSID }
            };

            // TODO: Parse Channels from "pin-unlock" request
        }
    }
}
